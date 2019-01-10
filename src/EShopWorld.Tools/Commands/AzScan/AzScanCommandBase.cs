using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Helpers;
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

        internal static string[] ApplicableSubscriptions =
            {"evo-ci", "evo-test", "evo-sandbox", "evo-preprod", "evo-prod"};

        internal static string[] SuffixesToRemove = {"-ci", "-test", "-sand", "-preprod", "-prod"};

        protected AzScanCommandBase()
        {
            
        }

        protected AzScanCommandBase(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother)
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
            Description = "name of the subscription to scan ",
            ShortName = "s",
            LongName = "subscription",
            ShowInHelpText = true)]
        [Required]
        public string Subscription { get; set; }

        [Option(
            Description = "optional region filter",
            ShortName = "r",
            LongName = "region",
            ShowInHelpText = true)]
        public string Region { get; set; }

        public virtual async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            //look up subscription id for the given name
            var defaultSubClient = Authenticated.WithDefaultSubscription();

            var subs = await defaultSubClient.Subscriptions.ListAsync();
            var sub = subs.FirstOrDefault(s => Subscription.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase));
            if (sub == null)
                throw new ApplicationException($"Subscription {Subscription} not found. Check the account role setup.");

            var subClient = Authenticated.WithSubscription(sub.SubscriptionId);

            await RunScanAsync(subClient);
         
            return 1;
        }

        protected virtual Task<int> RunScanAsync([NotNull] IAzure client)
        {
            return Task.FromResult(1);
        }

        protected async Task SetKeyVaultSecretAsync(string prefix, string name, string suffix, string value)
        {      
            
            await KeyVaultClient.SetSecretWithHttpMessagesAsync($"https://{KeyVaultName}.vault.azure.net/", $"{prefix}-{name.StripRecognizedSuffix(SuffixesToRemove).ToCamelCase()}-{suffix}", value);
        }

        protected bool CheckRegion(string region)
        {
            return string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(Region) || region.Equals(Region, StringComparison.OrdinalIgnoreCase);
        }
    }
}
