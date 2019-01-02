using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("redis", Description = "scans and projects redis level configuration into KV")]
    public class AzRedisScanCommand : AzScanCommandBase
    {
        protected override async Task<int> RunScanAsync(IAzure client)
        {
            var redises = !string.IsNullOrWhiteSpace(ResourceGroup)
                ? await client.RedisCaches.ListByResourceGroupAsync(ResourceGroup)
                : await client.RedisCaches.ListAsync();

            foreach (var redis in redises)
            {
                if (!CheckBasicFilters(redis.Name))
                    continue;

                var name = redis.Name.Contains('-')
                    ? redis.Name.Remove(redis.Name.LastIndexOf('-')) : redis.Name;

                await SetKeyVaultSecretAsync(name, redis.Keys.PrimaryKey);
            }
            return 1;
        }
    }
}
