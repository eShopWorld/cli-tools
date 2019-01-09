using System.Threading.Tasks;
using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("serviceBus", Description = "scan project service bus configuration into KV")]
    public class AzServiceBusScanCommand : AzScanCommandBase
    {
        public AzServiceBusScanCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

        protected override async Task<int> RunScanAsync(IAzure client)
        {

            //list sb namespaces
            var namespaces = await (!string.IsNullOrWhiteSpace(ResourceGroup)
                ? client.ServiceBusNamespaces.ListByResourceGroupAsync(ResourceGroup)
                : client.ServiceBusNamespaces.ListAsync());

            foreach (var @namespace in namespaces)
            {
                if (!CheckBasicFilters(@namespace.Name))
                    continue;

                var rule = await @namespace.AuthorizationRules.GetByNameAsync("RootManageSharedAccessKey");
                var keys = await rule.GetKeysAsync();

                var name = @namespace.Name.Contains('-')
                    ? @namespace.Name.Remove(@namespace.Name.LastIndexOf('-')) : @namespace.Name;

                await SetKeyVaultSecretAsync("SB", name, "PrimaryConnectionString", keys.PrimaryConnectionString);
            }

            return 1;
        }
    }
}
