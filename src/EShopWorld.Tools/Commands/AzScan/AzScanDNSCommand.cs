using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json.Linq;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// DNS scanning command
    /// </summary>
    [Command("dns", Description = "scans DNS set up and projects it into KV")]
    // ReSharper disable once InconsistentNaming
    public class AzScanDNSCommand : AzScanCommandBase
    {
        private readonly ServiceFabricDiscoveryFactory _sfDiscoveryFactory;
        private readonly ResourceManagementClient _rmClient;

        //as these are used against each and every LB targeting A-record, let's cache
        private IList<ILoadBalancer> _loadBalancersCache;
        private IList<IPublicIPAddress> _pipCache;
        private IAzure _azClient;
        private IConsole _console;

        /// <inheritdoc />
        public AzScanDNSCommand(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager, IBigBrother bigBrother,
            ServiceFabricDiscoveryFactory sfDiscoveryFactory, ResourceManagementClient rmClient) : base(authenticated, keyVaultManager, bigBrother, "Platform")
        {
            _sfDiscoveryFactory = sfDiscoveryFactory;
            _rmClient = rmClient;
        }

        /// <inheritdoc />
        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            _azClient = client;
            _console = console;
            //scope to V2 (global for now) zone
            var zones = (await client.DnsZones.ListByResourceGroupAsync(NameGenerator.GetPlatformRGName(EnvironmentName)))
                .Where(z => z.Name.EndsWith(".private", StringComparison.Ordinal)); //private = V2

            foreach (var zone in zones)
            {

                /**
                 * two main paths - API and UI
                 *
                 * API routing -> FD (~"Global") -> ELB (~"Cluster") -> Node (~"Proxy" - note that reverse proxy may redirect back to cluster)
                 * UI routing -> FD (~"Global")
                 * 
                 */

                //hydrate LB, PIP cache
                _loadBalancersCache = (await _azClient.LoadBalancers.ListAsync()).ToList(); //all pages for now due to old RGs and unclear filtering strategy
                _pipCache = (await _azClient.PublicIPAddresses.ListAsync()).ToList(); //all pages, ditto

                //scan CNAMEs -all global definitions
                var tasks = (await zone.CNameRecordSets.ListAsync()).Select(ProcessCName).ToList();

                var aNames = await zone.ARecordSets.ListAsync();

                //append A name scan tasks
                tasks.AddRange(RegionalPlatformResourceGroups.Select(r =>
                         ScanRegionalANames(r, aNames)
                ));

                await Task.WhenAll(tasks);
            }

            return 0;
        }

        private async Task ProcessCName(ICNameRecordSet cname)
        {
            foreach (var keyVaultName in DomainResourceGroup.TargetKeyVaults)
            {
                await KeyVaultManager.SetKeyVaultSecretAsync(keyVaultName, "Platform", cname.Name, "Global",
                    $"https://{cname.Fqdn.TrimEnd('.')}", "-afd");
            }
        }
        private async Task ScanRegionalANames(ResourceGroupDescriptor r, IEnumerable<IARecordSet> aNames)
        {
            var sfDiscovery = _sfDiscoveryFactory.GetInstance(); //region = separate cluster
            await sfDiscovery.CheckConnectionStatus(_azClient, EnvironmentName, r.Region, _console);

            var regionCode = r.Region.ToRegionCode();

            //scan A(Name)s - regional entries
            var tasks = aNames.Where(a => a.Name.RegionCodeCheck(regionCode)).Select(aName => Task.Run(async () =>
            {
                if (!aName.IPv4Addresses.Any())
                {
                    _console.EmitWarning(GetType(), AppInstance.Options,
                        $"DNS entry {aName.Name} does not have any target IP address");
                    return;
                }

                var ipAddress = aName.IPv4Addresses.First();

                foreach (var keyVault in r.TargetKeyVaults)
                {
                    //lookup backend rule in IP matching LB instance - match the rule by name
                    var lbRule = await LookupLoadBalancerPort(aName.Name, ipAddress);

                    if (!lbRule.HasValue) continue;

                    var port = lbRule.Value.port;
                    var portScheme = lbRule.Value.scheme;

                    var clusterRoute =
                        $"{portScheme.ToLowerInvariant()}://{aName.Fqdn.TrimEnd('.')}:{port.ToString(CultureInfo.InvariantCulture)}";

                    await KeyVaultManager.SetKeyVaultSecretAsync(keyVault,
                        SecretPrefix, aName.Name, "Cluster",
                        clusterRoute,
                        "-lb", "-afd");

                    var reverseProxyDetails = sfDiscovery.GetReverseProxyDetails();

                    if (reverseProxyDetails == null)
                    {
                        _console.EmitWarning(typeof(AzScanDNSCommand), AppInstance.Options,
                            $"Unable to lookup reverse proxy details for Service Fabric cluster - Environment - {EnvironmentName} - Region - {r.Region.ToRegionCode().ToPascalCase()}");
                        continue;
                    }

                    var serviceInstanceName = sfDiscovery.LookupServiceNameByPort(port);

                    if (string.IsNullOrWhiteSpace(serviceInstanceName))
                    {
                        _console.EmitWarning(typeof(AzScanDNSCommand), AppInstance.Options,
                            $"Unable to lookup service instance name for {aName.Name}:{port} under {EnvironmentName} environment - Region - {r.Region.ToRegionCode().ToPascalCase()}");
                        continue;
                    }

                    var (proxyScheme, proxyPort) = reverseProxyDetails.Value;

                    var proxyUrl = new UriBuilder(proxyScheme.ToLowerInvariant(), "localhost", proxyPort,
                        serviceInstanceName.RemoveFabricScheme()).ToString();

                    await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix, aName.Name,
                        "Proxy", proxyUrl, "-lb");
                }
            }));

            await Task.WhenAll(tasks);
        }

        private async Task<(int port, string scheme)?> LookupLoadBalancerPort(string aName, string ipAddress)
        {
            //public or private
            var publicIp =
                _pipCache.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.IPAddress) && i.IPAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase));

            //if public ip, locate by it's id, otherwise by private ip (internal load balancer)
            var targetLb = publicIp != null
                ? _loadBalancersCache.FirstOrDefault(lb =>
                    lb.PublicIPAddressIds.Any(s => s.Equals(publicIp.Id, StringComparison.OrdinalIgnoreCase)))
                : _loadBalancersCache.FirstOrDefault(lb =>
                    lb.Inner.FrontendIPConfigurations.Any(c => !string.IsNullOrEmpty(c.PrivateIPAddress) &&
                        c.PrivateIPAddress == ipAddress));

            if (targetLb == null)
            {
                //treat as raw IP address for monolith purposes - known to target direct VMs etc. @Colin
                return null;
            }

            //remove region and load balancer suffix from dns record name to derive rule name
            var ruleName = aName.EswTrim("-lb", "-afd");

            //find the rule
            var lbRule =
                targetLb.LoadBalancingRules.FirstOrDefault(r =>
                    r.Key.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

            if (!default(KeyValuePair<string, ILoadBalancingRule>).Equals(lbRule))
            {
                var probe = await _rmClient.Resources.GetByIdAsync(lbRule.Value.Inner.Probe.Id, "2019-04-01" /*not supported in latest */);
                return (lbRule.Value.BackendPort, ((JObject) probe.Properties)["protocol"].Value<string>());
            }

            _console.EmitWarning(GetType(), AppInstance.Options,
                $"No Load balancer rule found for {aName} under {targetLb.Name} load balancer");

            return null;
        }
    }
}
