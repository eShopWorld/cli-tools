using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Management.ServiceModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Fluent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// encapsulates connection setup/connection details lookup and service type/instance lookup flow against ServiceFabric
    /// </summary>
    public class ServiceFabricDiscovery
    {
        private readonly KeyVaultClient _kvClient;
        private readonly X509Store _x509Store;
        private FabricClient _fabricClient;
        private (string _connectedClusterProxyScheme, int _connectedClusterProxyPort)? _reverseProxySettings;
        private readonly Dictionary<int, string> _connectedClusterPortServiceMap = new Dictionary<int, string>();

        private static readonly XmlSerializer AppManifestSerializer = new XmlSerializer(typeof(ApplicationManifestType));
        private static readonly XmlSerializer ServiceManifestSerializer = new XmlSerializer(typeof(ServiceManifestType));

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="kvClient">key vault client</param>
        /// <param name="x509Store">x509 store</param>
        public ServiceFabricDiscovery(KeyVaultClient kvClient, X509Store x509Store)
        {
            _kvClient = kvClient;
            _x509Store = x509Store;
        }

        /// <summary>
        /// get application gateway scheme and port from cluster endpoint
        /// </summary>
        /// <returns>tuple - scheme and port, if unavailable, null</returns>
        public(string scheme, int port)? GetReverseProxyDetails()
        {
            return _reverseProxySettings;
        }

        /// <summary>
        /// looks up service uri by its port
        /// 
        /// use cache if possible, definitions consistent across regions
        /// 
        /// if not found, null uri is returned
        /// </summary>   
        /// <param name="servicePort">target port</param>
        /// <returns>service fabric uri or null</returns>
        public string LookupServiceNameByPort(int servicePort)
        {
            return _connectedClusterPortServiceMap.ContainsKey(servicePort) ? _connectedClusterPortServiceMap[servicePort] : null;
        }

        /// <summary>
        /// check whether we have scanner and if not, hydrate the dictionaries
        /// </summary>
        /// <param name="azClient">azure client</param>
        /// <param name="env">environment</param>
        /// <param name="region">region</param>
        /// <returns></returns>
        public async Task CheckConnectionStatus(IAzure azClient, string env, DeploymentRegion region, IConsole console)
        {
            if (_fabricClient == null)
            {
                var cert = await InstallCert(azClient, env, region);
                if (string.IsNullOrWhiteSpace(cert))
                {
                    return;
                }

                await Connect(env, region, cert, console);
            }
        }

        /// <summary>
        /// install cert by extracting it and checking its thumbprint against the store
        /// </summary>
        /// <param name="azClient">az client</param>
        /// <param name="env">target environment</param>
        /// <param name="region">target region</param>
        /// <returns>cert thumbprint</returns>
        private async Task<string> InstallCert(IAzure azClient, string env, DeploymentRegion region)
        {
            //check that platform KV exists
            var regionalPlatformKvName = NameGenerator.GetRegionalPlatformKVName(env, region);
            if (!(await azClient.Vaults.ListByResourceGroupAsync(NameGenerator.GetRegionalPlatformRGName(env, region)))
                .Any(v => v.Name.Equals(regionalPlatformKvName, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }
            //hook up to platform KV
            var certSecret = await _kvClient.GetSecret(regionalPlatformKvName, NameGenerator.ServiceFabricPlatformKVCertSecretName);
            //extract cert
            var cert = ExtractCert(certSecret);
            if (cert == null)
            {
                throw new ApplicationException($"Unable to obtain expected Service Fabric Cluster certificate for {env} environment and {region} region");
            }
            //install cert - if necessary
            InstallCert(cert);

            return cert.Thumbprint;
        }

        /// <summary>
        /// connects to fabric
        /// involves retrieving cert from platform KV and installing it into user cert store if needed
        /// </summary>
        /// <param name="env">environment to scope to</param>
        /// <param name="region">region to scope to</param>
        /// <param name="certThumbprint">thumbprint of the cluster certificate</param>
        /// <param name="clientEndpointPort">cluster client connection endpoint - defaulted to 19000</param>
        private async Task Connect(string env, DeploymentRegion region, string certThumbprint, IConsole console, int clientEndpointPort = 19000)
        {
            //connect and scan the SF cluster
            var xc = new X509Credentials
            {
                StoreLocation = StoreLocation.CurrentUser,
                StoreName = StoreName.My.ToString(),
                FindType = X509FindType.FindByThumbprint,
                FindValue = certThumbprint
            };
            xc.RemoteCertThumbprints.Add(certThumbprint);
            xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;

            //use custom dns entries instead of having to lookup the actual instance (and also to meet SSL chain of trust)
            _fabricClient = new FabricClient(xc, GetClusterUrl(env, region, clientEndpointPort));

            //pre-load app list, app and service manifests
            var appList = await _fabricClient.QueryManager.GetApplicationListAsync();
            foreach (var app in appList)
            {
                try
                {

                    //get the app manifest to iterate over services
                    var appManifestStr =
                        await _fabricClient.ApplicationManager.GetApplicationManifestAsync(app.ApplicationTypeName,
                            app.ApplicationTypeVersion);
                    var appManifest =
                        (ApplicationManifestType)AppManifestSerializer.Deserialize(new StringReader(appManifestStr));

                    var serviceList = await _fabricClient.QueryManager.GetServiceListAsync(app.ApplicationName);

                    foreach (var serviceManifestImport in appManifest.ServiceManifestImport)
                    {
                        //get the service manifest to check endpoints
                        var serviceManifestString = await _fabricClient.ServiceManager.GetServiceManifestAsync(
                            app.ApplicationTypeName, app.ApplicationTypeVersion,
                            serviceManifestImport.ServiceManifestRef.ServiceManifestName);

                        var serviceManifest = (ServiceManifestType)
                            ServiceManifestSerializer.Deserialize(new StringReader(serviceManifestString));

                        foreach (var endpoint in serviceManifest.Resources.Endpoints.Where(e =>
                            e.Protocol == EndpointTypeProtocol.http || e.Protocol == EndpointTypeProtocol.https))
                        {
                            //some expectations here for API- stateless only
                            var serviceInstance = serviceList.FirstOrDefault(s =>
                                serviceManifest.ServiceTypes.FirstOrDefault(t =>
                                    t is StatelessServiceTypeType type &&
                                    type.ServiceTypeName == s.ServiceTypeName) != null);

                            if (serviceInstance == null) continue;

                            _connectedClusterPortServiceMap.TryAdd(endpoint.Port,
                                serviceInstance.ServiceName.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    console.EmitMessage(console.Out, $"Application discovery failed for {app.ApplicationName.ToString()} - reason {e.Message}");
                }
            }

            //pre-load reverse proxy details
            var clusterManifestString = await _fabricClient.ClusterManager.GetClusterManifestAsync();
            var clusterManifestSerializer = new XmlSerializer(typeof(ClusterManifestType));
            var clusterManifest = (ClusterManifestType)clusterManifestSerializer.Deserialize(new StringReader(clusterManifestString));

            //expectation that all node types are configured for the same port
            var proxyEndpoint = clusterManifest.NodeTypes.First().Endpoints.HttpApplicationGatewayEndpoint;
            if (proxyEndpoint == null)
            {
                _reverseProxySettings = null;
            }
            else
            {
                _reverseProxySettings = (proxyEndpoint.Protocol.ToString(),
                    Convert.ToInt32(proxyEndpoint.Port, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// historical baggage - some naming deviations  etc. for certs for clusters container here
        /// </summary>
        /// <param name="env">environment</param>
        /// <param name="region">region</param>
        /// <param name="port">client endpoint port number</param>
        /// <returns>cluster management hostname</returns>
        internal static string GetClusterUrl(string env, DeploymentRegion region, int port = 19000)
        {
            var sb = new StringBuilder($"fabric");
            sb.Append($"-{(env.Equals(DeploymentEnvironment.Prep.ToString(), StringComparison.OrdinalIgnoreCase) ? "preprod" : env)}");

            if (!env.Equals(DeploymentEnvironment.CI.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($"-{region.ToRegionCode()}");
            }

            sb.Append(env.Equals(DeploymentEnvironment.Prod.ToString(), StringComparison.OrdinalIgnoreCase) ||
                       env.Equals(DeploymentEnvironment.Sand.ToString(), StringComparison.OrdinalIgnoreCase)
                ? ".eshopworld.com" : ".eshopworld.net");

            sb.Append($":{port}");

            return sb.ToString().ToLowerInvariant();
        }

        private void InstallCert(X509Certificate2 cert)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            var col = _x509Store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, true);
            if (col.Count == 0)
            {
                //install it
                _x509Store.Add(cert);
            }
        }

        private static X509Certificate2 ExtractCert(SecretBundle certSecret)
        {
            if (certSecret == null || string.IsNullOrWhiteSpace(certSecret.Value))
            {
                return null;
            }

            //cert stored as base64 encoded json with base64 encoded cert stream as one of the pairs
            var decodedSecret = Convert.FromBase64String(certSecret.Value);
            var json = JObject.Load(new JsonTextReader(new StringReader(Encoding.UTF8.GetString(decodedSecret))));
            if (json == null || !json.TryGetValue("data", out var cert) || string.IsNullOrWhiteSpace(cert.Value<string>()))
            {
                return null;
            }

            if (!json.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password.Value<string>()))
            {
                return null;
            }

            var x509Cert = new X509Certificate2(Convert.FromBase64String(cert.Value<string>()), password.Value<string>(), X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            return string.IsNullOrWhiteSpace(x509Cert.Thumbprint) ? null : x509Cert;
        }
    }

    /// <summary>
    /// factory class for <see cref="ServiceFabricDiscovery"/>
    /// the idea being that that the factory is resolved via DI and instance requested per region (as this wraps cluster connection) as this is not a fixed list
    /// </summary>
    public sealed class ServiceFabricDiscoveryFactory
    {
        private readonly KeyVaultClient _kvClient;
        private readonly X509Store _x509Store;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="kvClient">key vault client passed to <see cref="ServiceFabricDiscovery"/> instance</param>
        /// <param name="x509Store">x509 store nodes</param>
        public ServiceFabricDiscoveryFactory(KeyVaultClient kvClient,  X509Store x509Store)
        {
            _kvClient = kvClient;
            _x509Store = x509Store;
        }

        /// <summary>
        /// get instance 
        /// </summary>
        /// <returns>new instance of <see cref="ServiceFabricDiscovery"/></returns>
        public ServiceFabricDiscovery GetInstance()
        {
            return new ServiceFabricDiscovery(_kvClient, _x509Store);
        }
    }
}
