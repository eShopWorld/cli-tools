using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Eshopworld.Core;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    public abstract class AzScanCommandBase
    {
        protected readonly Azure.IAuthenticated Authenticated;
        protected readonly KeyVaultClient KeyVaultClient;
        protected readonly IBigBrother BigBrother;

        public AzScanCommandBase(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother)
        {
            Authenticated = authenticated;
            KeyVaultClient = keyVaultClient;
            BigBrother = bigBrother;
        }

        [Option(
            Description = "name of the keyvault to insert configuration into",
            ShortName = "k",
            LongName = "keyVault",
            ShowInHelpText = true)]
        [Required]
        public string KeyVaultName { get; set; }


        [Option(
            Description = "optional resource group filter",
            ShortName = "g",
            LongName = "resourceGroup",
            ShowInHelpText = true)]
        public string ResourceGroup { get; set; }

        [Option(
            Description = "optional environment filter",
            ShortName = "e",
            LongName = "environment",
            ShowInHelpText = true)]
        public string Environment { get; set; }

        [Option(
            Description = "optional regex filter",
            ShortName = "r",
            LongName = "regex",
            ShowInHelpText = true)]
        public string Regex { get; set; }

        public virtual async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            //run internal scan implementation over all detected subscriptions
            var defaultSubClient = Authenticated.WithDefaultSubscription();

            var subs = await defaultSubClient.Subscriptions.ListAsync();
            foreach (var sub in subs)
            {
                var subClient = Authenticated.WithSubscription(sub.SubscriptionId);

                await RunScanAsync(subClient);
            }

            return 1;
        }

        protected virtual Task<int> RunScanAsync([NotNull] IAzure client)
        {
            return Task.FromResult(1);
        }

        private static bool StringMatchRegexp(string value, string regexpStr)
        {
            if (string.IsNullOrWhiteSpace(regexpStr))
                return true;

            var regexp = new Regex(regexpStr);
            return regexp.IsMatch(value);
        }

        protected async Task SetKeyVaultSecretAsync(string name, string value)
        {
            await KeyVaultClient.SetSecretWithHttpMessagesAsync($"https://{KeyVaultName}.vault.azure.net/", name, value);
        }

        protected bool CheckBasicFilters(string key)
        {
            return string.IsNullOrWhiteSpace(key) ||
                   ((string.IsNullOrWhiteSpace(Environment) || key.EndsWith(Environment)) &&
                    StringMatchRegexp(key, Regex));
        }
    }
}
