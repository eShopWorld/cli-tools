using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// Azure Redis configuration manager scan
    /// </summary>
    [Command("redis", Description = "scans and projects redis level configuration into KV")]
    public class AzScanRedisCommand : AzScanKeyRotationCommandBase
    {
        /// <inheritdoc />
        public AzScanRedisCommand(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager, IBigBrother bigBrother) : base(authenticated, keyVaultManager, bigBrother, "Redis")
        {
        }

        /// <inheritdoc />
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
                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix, name,
                            "ConnectionString",
                            $"{redis.HostName},password={(UseSecondaryKey ? redis.Keys.SecondaryKey : redis.Keys.PrimaryKey)},ssl=True,abortConnect=False");
                    }
                }
            }

            return 0;
        }
    }
}
