using System.Threading.Tasks;
using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// emits environment information
    /// </summary>
    [Command("environmentInfo", Description = "projects environmental info - subscription level")]
    internal class AzScanEnvironmentInfoCommand : AzScanCommandBase
    {
        private readonly Azure.IAuthenticated _auth;
        private readonly AzScanKeyVaultManager _kvManager;

        public AzScanEnvironmentInfoCommand(Azure.IAuthenticated auth, AzScanKeyVaultManager kvManager, IBigBrother bb):base(auth, kvManager, bb, "Environment" )
        {
            _auth = auth;
            _kvManager = kvManager;
        }
        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            foreach (var kv in DomainResourceGroup.TargetKeyVaults)
            {
                await _kvManager.SetKeyVaultSecretAsync(kv, SecretPrefix, string.Empty, "SubscriptionId", SubscriptionId);
                await _kvManager.SetKeyVaultSecretAsync(kv, SecretPrefix, string.Empty, "SubscriptionName", Subscription);
            }

            return 0;
        }
    }
}
