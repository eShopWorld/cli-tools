using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Helpers;
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

        protected override async Task<int> RunScanAsync(IAzure client)
        {
            var redises = !string.IsNullOrWhiteSpace(ResourceGroup)
                ? await client.RedisCaches.ListByResourceGroupAsync(ResourceGroup)
                : await client.RedisCaches.ListAsync();

            foreach (var redis in redises)
            {
                if (!CheckRegion(redis.RegionName))
                    continue;

                var name = redis.Name.Contains('-')
                    ? redis.Name.Remove(redis.Name.LastIndexOf('-')) : redis.Name;

                await KeyVaultClient.SetKeyVaultSecretAsync(KeyVaultName, "Redis", name, "PrimaryConnectionString", $"{redis.HostName},password={redis.Keys.PrimaryKey},ssl=True,abortConnect=False");
            }
            return 1;
        }
    }
}
