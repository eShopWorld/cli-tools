using System.Threading.Tasks;
using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// cosmos db scan implementation
    /// </summary>
    [Command("cosmosDb", Description = "scan and project Cosmos Dbs configuration into KV")]
    public class AzScanCosmosDbCommand  : AzScanKeyRotationCommandBase
    {
        /// <inheritdoc />
        public AzScanCosmosDbCommand(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager, IBigBrother bigBrother) : base(authenticated, keyVaultManager, bigBrother, "CosmosDB")
        {
        }

        /// <inheritdoc />
        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {                
            var cosmoses =
                await client.CosmosDBAccounts.ListByResourceGroupAsync(DomainResourceGroup.Name);                    

            foreach (var cosmos in cosmoses)
            {
                //no region filter for cosmos
                var keys = await cosmos.ListKeysAsync();

                foreach (var keyVaultName in DomainResourceGroup.TargetKeyVaults)
                {
                    await KeyVaultManager.SetKeyVaultSecretAsync(keyVaultName, SecretPrefix, cosmos.Name,
                        "ConnectionString",
                        $"AccountEndpoint={cosmos.DocumentEndpoint};AccountKey={(UseSecondaryKey ? keys.SecondaryMasterKey : keys.PrimaryMasterKey)}");
                }
            }                

            return 0;
        }
    }
}
