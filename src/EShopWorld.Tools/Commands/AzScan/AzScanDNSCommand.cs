﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
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
        private readonly ServiceFabricDiscovery _sfDiscovery;

        //as these are used against each and every LB targeting A-record, let's cache
        private IPagedCollection<ILoadBalancer> _loadBalancersCache;
        private IPagedCollection<IPublicIPAddress> _pipCache;

        /// <inheritdoc />
        public AzScanDNSCommand(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager, IBigBrother bigBrother, ServiceFabricDiscovery sfDiscovery) : base(authenticated, keyVaultManager, bigBrother, "Platform")
        {
            _sfDiscovery = sfDiscovery;
        }

        /// <inheritdoc />
        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            //filter out non V1 DNS zones
            var zones = (await client.DnsZones.ListByResourceGroupAsync(NameGenerator.GetPlatformRGName(EnvironmentName)))
                .Where(z => !z.Name.EndsWith(".private", StringComparison.Ordinal)); //private = V2

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
                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVaultName, "Platform", cName.Name, "Global",
                            $"https://{cName.Fqdn.TrimEnd('.')}");
                    }
                }

                var aNames = await zone.ARecordSets.ListAsync();

                foreach (var regionalDef in RegionalPlatformResourceGroups) //match regional KV to the A record region (by name)
                {
                    var regionCode = regionalDef.Region.ToRegionCode();

                    //scan A(Name)s - regional entries
                    foreach (var aName in aNames)
                    {
                        if (!aName.Name.RegionCodeCheck(regionCode))
                            continue;

                        var isLb = aName.Name.EndsWith("-lb") || !aNames.Any(a =>
                                       a.Name.Equals($"{aName.Name}-lb", StringComparison.OrdinalIgnoreCase));

                        if (!aName.IPv4Addresses.Any())
                        {
                            console.EmitWarning(BigBrother, GetType(), AppInstance.Options, $"DNS entry {aName.Name} does not have any target IP address");
                            continue;
                        }                       

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
                            if (isLb)
                            {
                                var (proxyScheme, proxyPort) = await _sfDiscovery.GetReverseProxyDetails(client, EnvironmentName, regionalDef.Region);

                                if (!string.IsNullOrWhiteSpace(proxyScheme))
                                {
                                    //attempt to construct reverse proxy url too
                                    string serviceInstanceName;
                                    if (!string.IsNullOrWhiteSpace(serviceInstanceName =
                                        await _sfDiscovery.LookupServiceNameByPort(client, EnvironmentName,
                                            regionalDef.Region, port.Value)))
                                    {
                                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix, aName.Name,
                                            "Proxy",
                                            new UriBuilder(proxyScheme, "localhost", proxyPort, serviceInstanceName.RemoveFabricScheme())
                                                .ToString(), "-lb");
                                    }
                                    else
                                    {
                                        console.EmitWarning(BigBrother, typeof(AzScanDNSCommand), AppInstance.Options, $"Unable to lookup service instance name for {aName.Name}:{port.Value} under {EnvironmentName} environment");
                                    }
                                }
                                else
                                {
                                    console.EmitWarning(BigBrother, typeof(AzScanDNSCommand), AppInstance.Options, $"Unable to lookup reverse proxy details for Service Fabric cluster - Environment - {EnvironmentName} - Region - {regionalDef.Region.ToRegionCode()}");
                                }

                                await KeyVaultManager.SetKeyVaultSecretAsync(keyVault,
                                    SecretPrefix, aName.Name, "Cluster",
                                    $"http://{aName.IPv4Addresses.First()}:{port.Value.ToString(CultureInfo.InvariantCulture)}",
                                    "-lb");
                            }
                            else
                            {
                                await KeyVaultManager.SetKeyVaultSecretAsync(keyVault,
                                    SecretPrefix, aName.Name, "Gateway", $"https://{aName.Fqdn.TrimEnd('.')}");
                            }
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
                        c.PrivateIPAddress == ipAddress));

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
