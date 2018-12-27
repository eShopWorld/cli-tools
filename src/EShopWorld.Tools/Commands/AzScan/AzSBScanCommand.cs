using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("servicebus", Description = "scan project service bus configuration into KV")]
    public class AzSBScanCommand : AzScanCommandBase
    {
        protected override async Task<int> RunScanAsync(IAzure client, ISubscription sub)
        {

            //list sb namespaces
            var namespaces = await (!string.IsNullOrWhiteSpace(ResourceGroup)
                ? client.ServiceBusNamespaces.ListByResourceGroupAsync(ResourceGroup)
                : client.ServiceBusNamespaces.ListAsync());

            foreach (var @namespace in namespaces)
            {
                if (!string.IsNullOrWhiteSpace(Environment) && !@namespace.Name.EndsWith(Environment))
                    continue;

                if (!StringMatchRegexp(@namespace.Name, Regex))
                    continue;

                var rule = await @namespace.AuthorizationRules.GetByNameAsync("RootManageSharedAccessKey");
                var keys = await rule.GetKeysAsync();

                var name = @namespace.Name.Contains('-')
                    ? @namespace.Name.Remove(@namespace.Name.LastIndexOf('-')) : @namespace.Name;

                await UpsertSecretToKV(name,keys.PrimaryConnectionString);
            }

            return 1;
        }
    }
}
