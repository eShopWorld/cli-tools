using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("serviceBus", Description = "scan project service bus configuration into KV")]
    public class AzServiceBusScanCommand : AzScanCommandBase
    {
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

                await SetKeyVaultSecretAsync(name,keys.PrimaryConnectionString);
            }

            return 1;
        }
    }
}
