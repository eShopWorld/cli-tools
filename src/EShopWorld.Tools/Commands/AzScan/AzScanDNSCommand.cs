using System.Linq;
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

        protected async override Task<int> RunScanAsync(IAzure client)
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
                foreach (var cname in await zone.CNameRecordSets.ListAsync())
                {
                    if (!cname.Name.ShortRegionCheck(Region))
                        continue;

                    await SetKeyVaultSecretAsync("Platform", cname.Name, "Global", $"https://{cname.Fqdn.TrimEnd('.')}");
                }
                //scan A(Name)s
                foreach (var aname in await zone.ARecordSets.ListAsync())
                {
                    var isLb = aname.Name.EndsWith("-lb");
                    if (!aname.IPv4Addresses.Any())
                    {
                        _console.WriteLine($"DNS entry {aname.Name} does not have any target IP address");
                        continue;
                    }

                    if (!aname.Name.ShortRegionCheck(Region))
                        continue;

                    await SetKeyVaultSecretAsync("Platform", aname.Name, isLb ? "LB" : "Gateway",
                        $"{(isLb? "http":"https")}://{aname.IPv4Addresses.First()}");
                }
            }

            return 1;
        }
    }
}
