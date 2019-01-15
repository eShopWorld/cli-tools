﻿using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
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
        private readonly IConsole _console;

        public AzScanDNSCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother, IConsole console) : base(authenticated, keyVaultClient, bigBrother)
        {
            _console = console;
        }

        protected override async Task<int> RunScanAsync(IAzure client)
        {
            var zones = await client.DnsZones.ListAsync();
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

                //scan CNAMEs
                foreach (var cName in await zone.CNameRecordSets.ListAsync())
                {
                    if (!cName.Name.RegionCodeCheck(Region))
                        continue;

                    await KeyVaultClient.SetKeyVaultSecretAsync(KeyVaultName, "Platform", cName.Name, "Global", $"https://{cName.Fqdn.TrimEnd('.')}");
                }
                //scan A(Name)s
                foreach (var aName in await zone.ARecordSets.ListAsync())
                {
                    var isLb = aName.Name.EndsWith("-lb");
                    if (!aName.IPv4Addresses.Any())
                    {
                        _console.WriteLine($"DNS entry {aName.Name} does not have any target IP address");
                        continue;
                    }

                    if (!aName.Name.RegionCodeCheck(Region))
                        continue;

                    await KeyVaultClient.SetKeyVaultSecretAsync(KeyVaultName, "Platform", aName.Name, isLb ? "LB" : "Gateway",
                        $"{(isLb? "http":"https")}://{aName.IPv4Addresses.First()}");
                }
            }

            return 1;
        }
    }
}
