using System;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// DNS scanning command
    /// </summary>
    [Command("dns", Description = "scans DNS set up and projects it into KV")]
    // ReSharper disable once InconsistentNaming
    public class AzScanDNSCommand : AzScanCommandBase
    {
        public AzScanDNSCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {          
        }

        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {

                var zones = await client.DnsZones.ListAsync(); //this is at platform RG level but expectation of single per sub stands
                foreach (var zone in zones)
                {

                    /**
                     * two main paths - API and UI
                     *
                     * API routing -> Traffic Manager -> App Gateway -> LB
                     * UI routing -> CDN -> App Services
                     *
                     * this will change with FrontDoor
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
                            console.ForegroundColor = ConsoleColor.Yellow;
                            console.WriteLine($"DNS entry {aName.Name} does not have any target IP address");
                            console.ResetColor();
                            continue;
                        }

                        foreach (var regionalDef in RegionalPlatformResourceGroups
                        ) //match regional KV to the A record region (by name)
                        {
                            var regionCode = regionalDef.Region.ToRegionCode();

                            if (!aName.Name.RegionCodeCheck(regionCode))
                                continue;

                            foreach (var keyVault in regionalDef.TargetKeyVaults)
                            {
                                await KeyVaultClient.SetKeyVaultSecretAsync(keyVault,
                                    "Platform", aName.Name,
                                    isLb ? "HTTP" : "HTTPS",
                                    $"{(isLb ? "http" : "https")}://{aName.IPv4Addresses.First()}",  "-lb");
                            }
                        }
                    }
                }

            return 0;
        }
    }
}
