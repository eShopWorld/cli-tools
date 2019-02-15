using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// implements scanning logic for AI and projects its config into KV
    /// </summary>
    [Command("ai", Description = "scans AI resources and projects their configuration to KV")]
    public class AzScanAppInsightsCommand : AzScanCommandBase
    {
        private readonly ApplicationInsightsManagementClient _azClient;

        public AzScanAppInsightsCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother, ApplicationInsightsManagementClient azClient) : base(authenticated, keyVaultClient, bigBrother)
        {
            _azClient = azClient;
        }

        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            _azClient.SubscriptionId = client.SubscriptionId;

            //FYI - https://github.com/Azure/azure-sdk-for-net/issues/5123
       
            string nextPageLink = null;
            do
            {
                var ais = string.IsNullOrWhiteSpace(nextPageLink)
                    ? await _azClient.Components.ListByResourceGroupAsync(DomainResourceGroup.Name)
                    : await _azClient.Components.ListByResourceGroupNextAsync(nextPageLink);

                nextPageLink = ais.NextPageLink;

                foreach (var ai in ais)
                {
                    foreach (var keyVaultName in DomainResourceGroup.TargetKeyVaults)
                    {
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVaultName, "AI", ai.Name,
                            "InstrumentationKey",
                            ai.InstrumentationKey);
                    }
                }
            } while (!string.IsNullOrWhiteSpace(nextPageLink));

            return 0;
        }
    }
}
