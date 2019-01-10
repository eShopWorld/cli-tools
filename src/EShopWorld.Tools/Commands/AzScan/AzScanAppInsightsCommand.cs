using System.Threading.Tasks;
using Eshopworld.Core;
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

        protected override async Task<int> RunScanAsync(IAzure client)
        {
            _azClient.SubscriptionId = client.SubscriptionId;

            string nextPageLink = string.Empty;
            do
            {
                var ais = string.IsNullOrEmpty(ResourceGroup)
                    ? string.IsNullOrWhiteSpace(nextPageLink)
                        ? await _azClient.Components.ListAsync()
                        : await _azClient.Components.ListNextAsync(nextPageLink)
                    : string.IsNullOrWhiteSpace(nextPageLink)
                        ? await _azClient.Components.ListByResourceGroupAsync(ResourceGroup)
                        : await _azClient.Components.ListByResourceGroupNextAsync(nextPageLink);

                nextPageLink = ais.NextPageLink;

                foreach (var ai in ais)
                {                    
                    if (!CheckRegion(ai.Location))
                        continue;

                    await SetKeyVaultSecretAsync("AI", ai.Name, "InstrumentationKey", ai.InstrumentationKey);
                }
            } while (nextPageLink != null);

            return 1;
        }
    }
}
