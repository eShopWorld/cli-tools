using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("cosmosDb", Description = "scan and project Cosmos Dbs configuration into KV")]
    public class AzScanCosmosDbCommand  : AzScanCommandBase
    {
        public AzScanCosmosDbCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

        protected override async Task<int> RunScanAsync(IAzure client)
        {
            var cosmoses = string.IsNullOrWhiteSpace(ResourceGroup)
                ? await client.CosmosDBAccounts.ListAsync()
                : await client.CosmosDBAccounts.ListByResourceGroupAsync(ResourceGroup);

            foreach (var cosmos in cosmoses)
            {
                //no region filter for cosmos

                var keys = await cosmos.ListKeysAsync();
                var name = cosmos.Name.Contains('-')
                    ? cosmos.Name.Remove(cosmos.Name.LastIndexOf('-')) : cosmos.Name;

                await KeyVaultClient.SetKeyVaultSecretAsync(KeyVaultName, "CosmosDB", name, "PrimaryConnectionString", $"AccountEndpoint={cosmos.DocumentEndpoint};AccountKey={keys.PrimaryMasterKey}");
            }

            return 1;
        }
    }
}
