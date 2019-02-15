using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("redis", Description = "scans and projects redis level configuration into KV")]
    public class AzScanRedisCommand : AzScanCommandBase
    {
        public AzScanRedisCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            foreach (var rg in RegionalPlatformResourceGroups)
            {
                var redises = await client.RedisCaches.ListByResourceGroupAsync(rg.Name);

                foreach (var redis in redises)
                {
                    if (!redis.RegionName.RegionNameCheck(rg.Region.ToRegionCode()))
                        continue;

                    var name = redis.Name.Contains('-')
                        ? redis.Name.Remove(redis.Name.LastIndexOf('-'))
                        : redis.Name;

                    foreach (var keyVault in rg.TargetKeyVaults)
                    {
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVault, "Redis", name,
                            "PrimaryConnectionString",
                            $"{redis.HostName},password={redis.Keys.PrimaryKey},ssl=True,abortConnect=False");
                    }
                }
            }

            return 0;
        }
    }
}
