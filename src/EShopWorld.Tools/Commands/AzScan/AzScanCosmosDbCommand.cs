using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("cosmosDb", Description = "scan and project Cosmos Dbs configuration into KV")]
    public class AzScanCosmosDbCommand  : AzScanCommandBase
    {
        /// <summary>
        /// boolean flag to indicate secondary key should be used instead of primary
        /// </summary>
        [Option(
            Description = "flag indicating to use secondary key",
            ShortName = "2",
            LongName = "secondary",
            ShowInHelpText = true)]
        public bool UseSecondaryKey { get; set; }
        
        public AzScanCosmosDbCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

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
                    await KeyVaultClient.SetKeyVaultSecretAsync(keyVaultName, "CosmosDB", cosmos.Name,
                        "ConnectionString",
                        $"AccountEndpoint={cosmos.DocumentEndpoint};AccountKey={(UseSecondaryKey ? keys.SecondaryMasterKey : keys.PrimaryMasterKey)}");
                }
            }                

            return 0;
        }
    }
}
