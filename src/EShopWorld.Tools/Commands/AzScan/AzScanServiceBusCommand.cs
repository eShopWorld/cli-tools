using System.Threading.Tasks;
using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// Azure Service Bus configuration management scan process
    /// </summary>
    [Command("serviceBus", Description = "scan project service bus configuration into KV")]
    public class AzScanServiceBusCommand : AzScanKeyRotationCommandBase
    {
        /// <inheritdoc />
        public AzScanServiceBusCommand(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager, IBigBrother bigBrother) : base(authenticated, keyVaultManager, bigBrother, "SB")
        {
        }

        /// <inheritdoc />
        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            //list sb namespaces
            var namespaces = await client.ServiceBusNamespaces.ListByResourceGroupAsync(DomainResourceGroup.Name);
                
            foreach (var @namespace in namespaces)
            {            
                var rule = await @namespace.AuthorizationRules.GetByNameAsync("RootManageSharedAccessKey");
                var keys = await rule.GetKeysAsync();

                var name = @namespace.Name.Contains('-')
                    ? @namespace.Name.Remove(@namespace.Name.LastIndexOf('-')) : @namespace.Name;

                foreach (var keyVaultName in DomainResourceGroup.TargetKeyVaults)
                {
                    await KeyVaultManager.SetKeyVaultSecretAsync(keyVaultName, SecretPrefix, name, "ConnectionString",
                        UseSecondaryKey ? keys.SecondaryConnectionString : keys.PrimaryConnectionString);
                }
            }

            return 0;
        }
    }
}
