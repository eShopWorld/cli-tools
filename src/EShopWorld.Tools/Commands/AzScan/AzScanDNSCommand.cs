using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// DNS scanning command
    /// </summary>
    [Command("dns", Description = "scans DNS set up and projects it into KV")]
    // ReSharper disable once InconsistentNaming
    public class AzScanDNSCommand : AzScanCommandBase
    {
        //as these are used against each and every LB targeting A-record, let's cache
        private IPagedCollection<ILoadBalancer> _loadBalancersCache;
        private IPagedCollection<IPublicIPAddress> _pipCache;

        public AzScanDNSCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            //filter out non V1 DNS zones
            var zones = await client.DnsZones.ListByResourceGroupAsync(PlatformResourceGroup); 
            foreach (var zone in zones)
            {

                /**
                 * two main paths - API and UI
                 *
                 * API routing -> Traffic Manager -> App Gateway (optional for HTTP only) -> LB
                 * UI routing -> CDN -> App Services
                 *
                 * if no AppGateway - the DNS entry will not have LB suffix but this is detected and treated as LB
                 *
                 * this will change with FrontDoor/V2
                 */

                //scan CNAMEs - all global definitions
                foreach (var cName in await zone.CNameRecordSets.ListAsync())
                {
                    foreach (var keyVaultName in DomainResourceGroup.TargetKeyVaults)
                    {
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVaultName, "Platform", cName.Name, "Global",
                            $"https://{cName.Fqdn.TrimEnd('.')}");
                    }
                }

                var aNames = await zone.ARecordSets.ListAsync();

                //scan A(Name)s - regional entries
                foreach (var aName in aNames)
                {
                    var isLb = aName.Name.EndsWith("-lb") || !aNames.Any(a =>
                                   a.Name.Equals($"{aName.Name}-lb", StringComparison.OrdinalIgnoreCase));

                    if (!aName.IPv4Addresses.Any())
                    {
                        console.EmitWarning(BigBrother, GetType(), AppInstance.Options, $"DNS entry {aName.Name} does not have any target IP address");
                        continue;
                    }

                    foreach (var regionalDef in RegionalPlatformResourceGroups) //match regional KV to the A record region (by name)
                    {
                        var regionCode = regionalDef.Region.ToRegionCode();

                        if (!aName.Name.RegionCodeCheck(regionCode))
                            continue;

                        var ipAddress = aName.IPv4Addresses.First();
                        //lookup backend rule in IP matching LB instance - match the rule by name
                        var port = isLb ? await LookupLoadBalancerPort(client, console, aName.Name, ipAddress) : -1;

                        if (isLb && !port.HasValue)
                        {
                            //unable to process this record, warnings issued
                            continue;
                        }

                        foreach (var keyVault in regionalDef.TargetKeyVaults)
                        {
                            await KeyVaultClient.SetKeyVaultSecretAsync(keyVault,
                                "Platform", aName.Name,
                                isLb ? "HTTP" : "HTTPS",
                                $"{(isLb ? "http" : "https")}://{aName.IPv4Addresses.First()}{(isLb ? ":" + port.Value.ToString(CultureInfo.InvariantCulture) : "")}", "-lb");
                        }
                    }
                }
            }

            return 0;
        }

        private async Task<int?> LookupLoadBalancerPort(IAzure client, IConsole console, string aName, string ipAddress)
        {
            //lookup LB instance using cache 
            //note that "old" (v0/1) LBs are in "old" (v0/1) RGs so not filtering by RG
            if (_loadBalancersCache == null || _pipCache == null)
            {
                //hydrate
                _loadBalancersCache = await client.LoadBalancers.ListAsync(); //all pages
                _pipCache = await client.PublicIPAddresses.ListAsync(); //all pages
            }


            //public or private
            var publicIp =
                _pipCache.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.IPAddress) && i.IPAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase));

            //if public ip, locate by it's id, otherwise by private ip (internal load balancer)
            var targetLb = publicIp != null
                ? _loadBalancersCache.FirstOrDefault(lb =>
                    lb.PublicIPAddressIds.Any(s => s.Equals(publicIp.Id, StringComparison.OrdinalIgnoreCase)))
                : _loadBalancersCache.FirstOrDefault(lb =>
                    lb.Inner.FrontendIPConfigurations.Any(c => !string.IsNullOrEmpty(c.PrivateIPAddress) &&
                        c.PrivateIPAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase)));

            if (targetLb == null)
            {
                console.EmitWarning(BigBrother, GetType(), AppInstance.Options,
                    $"No Load balancer found for public ip of {ipAddress} for {aName}");
                return null;
            }

            //remove region and load balancer suffix from dns record name to derive rule name
            var ruleName = aName.EswTrim("-lb");

            //find the rule
            var lbRule =
                targetLb.LoadBalancingRules.FirstOrDefault(r =>
                    r.Key.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

            if (!default(KeyValuePair<string, ILoadBalancingRule>).Equals(lbRule))
            {
                return lbRule.Value.BackendPort;
            }

            console.EmitWarning(BigBrother, GetType(), AppInstance.Options,
                $"No Load balancer rule found for {aName} under {targetLb.Name} load balancer");

            return null;
        }
    }
}
