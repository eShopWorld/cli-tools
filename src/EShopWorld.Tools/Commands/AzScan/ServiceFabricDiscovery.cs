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
using JetBrains.Annotations;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ServiceFabric.Fluent;
using Microsoft.Azure.Management.ServiceFabric.Fluent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// encapsulates connection setup/connection details lookup and service type/instance lookup flow against ServiceFabric
    /// </summary>
    public class ServiceFabricDiscovery
    {
        private IAzure _azClient;
        private readonly KeyVaultClient _kvClient;
        private readonly ServiceFabricManagementClient _sfClient;
        private FabricClient _fabricClient;
        private DeploymentRegion _connectedClusterRegion;
        private string _connectedClusterProxyScheme;
        private int _connectedClusterProxyPort;
        private readonly Dictionary<int, string> _connectedClusterPortServiceMap = new Dictionary<int, string>();

        private static readonly XmlSerializer AppManifestSerializer = new XmlSerializer(typeof(ApplicationManifestType));
        private static readonly XmlSerializer ServiceManifestSerializer = new XmlSerializer(typeof(ServiceManifestType));

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="kvClient">key vault client</param>
        /// <param name="sfClient">service fabric management client</param>
        public ServiceFabricDiscovery(KeyVaultClient kvClient, ServiceFabricManagementClient sfClient)
        {
            _kvClient = kvClient;
            _sfClient = sfClient;
        }

        /// <summary>
        /// lookup application gateway scheme and port from cluster endpoint
        ///
        /// use cache if possible, definitions consistent against regions
        /// </summary>
        /// <param name="azClient">az client</param>
        /// <param name="env">target environment</param>
        /// <param name="reg">target region</param>
        /// <returns>tuple - scheme and port, if not define scheme is empty and port is -1</returns>
        public async Task<(string scheme, int port)> GetReverseProxyDetails(IAzure azClient, string env, DeploymentRegion reg)
        {
            if (!string.IsNullOrWhiteSpace(_connectedClusterProxyScheme) && _connectedClusterProxyPort != (-1))
            {
                return (_connectedClusterProxyScheme, _connectedClusterProxyPort);
            }

            await CheckConnectionStatus(azClient, env, reg);

            return (_connectedClusterProxyScheme, _connectedClusterProxyPort);
        }

        /// <summary>
        /// looks up service uri by its port
        /// 
        /// use cache if possible, definitions consistent across regions
        /// 
        /// if not found, empty uri is returned
        /// </summary>
        /// <param name="azClient">azure fluent client</param>
        /// <param name="env">environment to scope to</param>
        /// <param name="region">region to scope to</param>
        /// <param name="servicePort">target port</param>
        /// <returns>service fabric uri or null</returns>
        public async Task<string> LookupServiceNameByPort(IAzure azClient, string env, DeploymentRegion region,
            int servicePort)
        {
            if (!_connectedClusterPortServiceMap.ContainsKey(servicePort))
            {
                //not in cache, have we switched region? (but it means apps are not consistent against regions)
                if (_connectedClusterRegion!=region)
                {
                    await CheckConnectionStatus(azClient, env, region);
                }
            }

            return _connectedClusterPortServiceMap.ContainsKey(servicePort) ? _connectedClusterPortServiceMap[servicePort] : null;            
        }

        private async Task CheckConnectionStatus(IAzure azClient, string env, DeploymentRegion region)
        {
            if (_fabricClient == null || region != _connectedClusterRegion)
            {
                await Connect(azClient, env, region);
            }
        }

        /// <summary>
        /// connects to fabric
        /// involves retrieving cert from platform KV and installing it into user cert store if needed
        /// </summary>
        /// <param name="azClient">azure client</param>
        /// <param name="env">environment to scope to</param>
        /// <param name="region">region to scope to</param>
        /// <param name="clientEndpointPort">cluster client connection endpoint - defaulted to 19000</param>
        private async Task Connect(IAzure azClient, string env, DeploymentRegion region, int clientEndpointPort = 19000)
        {
            _azClient = azClient;
            //check that platform KV exists
            var regionalPlatformKvName = NameGenerator.GetRegionalKVName(env, region);
            if (!(await _azClient.Vaults.ListByResourceGroupAsync(NameGenerator.GetRegionalPlatformRGName(env, region)))
                .Any(v => v.Name.Equals(regionalPlatformKvName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
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
            //connect and scan the SF cluster
            var xc = new X509Credentials
            {
                StoreLocation = StoreLocation.CurrentUser,
                StoreName = StoreName.My.ToString(),
                FindType = X509FindType.FindByThumbprint,
                FindValue = cert.Thumbprint
            };
            xc.RemoteCertThumbprints.Add(cert.Thumbprint);
            xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;

            //lookup the cluster by RG
            _sfClient.SubscriptionId = _azClient.SubscriptionId;
            var clusterListResponse = await _sfClient.Clusters.ListByResourceGroupAsync(NameGenerator.GetV0PlatformRegionalRGName(env, region)); //NOTE that these are "v0" names
            ClusterInner cluster;
            while ((cluster = clusterListResponse.FirstOrDefault()) == null ||
                   !string.IsNullOrWhiteSpace(clusterListResponse.NextPageLink)) //single is expected in V1
            {
                clusterListResponse =
                    await _sfClient.Clusters.ListByResourceGroupNextAsync(clusterListResponse.NextPageLink);
            }

            if (cluster == null)
            {
                throw new ApplicationException($"Unable to look up Service Fabric cluster under {region}-platform-{env} Resource Group");
            }

            var uri = new UriBuilder(new Uri(cluster.ManagementEndpoint)) //client management endpoint does not seem to be exposed but can be derived
            {
                Port = clientEndpointPort,
                Scheme = string.Empty
            };

            _fabricClient = new FabricClient(xc, uri.ToString());
            _connectedClusterRegion = region;

            //pre-load app list, app and service manifests
            var appList = await _fabricClient.QueryManager.GetApplicationListAsync();

            foreach (var app in appList)
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

                        _connectedClusterPortServiceMap.TryAdd(endpoint.Port, serviceInstance.ServiceName.ToString());
                    }
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
                _connectedClusterProxyScheme = "http";//TODO:test only!
                _connectedClusterProxyPort = 9000;
            }
            else
            {
                _connectedClusterProxyPort = Convert.ToInt32(proxyEndpoint.Port, CultureInfo.InvariantCulture);
                _connectedClusterProxyScheme = proxyEndpoint.Protocol.ToString();
            }
        }

        private static void InstallCert([NotNull] X509Certificate2 cert)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                // ReSharper disable once AssignNullToNotNullAttribute
                var col = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, true);
                if (col.Count == 0)
                {
                    //install it
                    store.Add(cert);
                }
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

            var x509Cert = new X509Certificate2(Convert.FromBase64String(cert.Value<string>()), password.Value<string>(), X509KeyStorageFlags.UserKeySet);
            return string.IsNullOrWhiteSpace(x509Cert.Thumbprint) ? null : x509Cert;
        }
    }
}
