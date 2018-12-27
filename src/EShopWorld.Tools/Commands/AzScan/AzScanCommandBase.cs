using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest;

namespace EShopWorld.Tools.Commands.AzScan
{
    public abstract class AzScanCommandBase : CommandBase
    {
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

        protected override async Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console)
        {
            //run internal scan implementation over all detected subscriptions
            var client = ServiceProvider.GetService<RestClient>();
            var defaultSubClient = ServiceProvider.GetService<Azure.IAuthenticated>().WithDefaultSubscription();

            var subs = await defaultSubClient.Subscriptions.ListAsync();
            foreach (var sub in subs)
            {
                var subClient = ServiceProvider.GetService<Azure.IAuthenticated>().WithSubscription(sub.SubscriptionId);

                await RunScanAsync(subClient, sub);
            }

            return 1;
        }

        protected internal override void ConfigureDI(IConsole console)
        {
            base.ConfigureDI(console);
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync("https://management.core.windows.net/", string.Empty).Result;
            var tokenCredentials = new TokenCredentials(token);

            var client = RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty, AzureEnvironment.AzureGlobalCloud))
                .Build();

            ServiceCollection.AddSingleton(client);
            ServiceCollection.AddSingleton(Azure.Authenticate(client, null)); 
            ServiceCollection.AddSingleton(new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback))); //cannot use token from above - different resource
        }

        protected abstract Task<int> RunScanAsync([NotNull] IAzure client, [NotNull] ISubscription sub);

        protected static bool StringMatchRegexp(string value, string regexpStr)
        {
            if (string.IsNullOrWhiteSpace(regexpStr))
                return true;

            var regexp = new Regex(regexpStr);
            return regexp.IsMatch(value);
        }

        protected async Task UpsertSecretToKV(string name, string value)
        {
            var client = ServiceProvider.GetService<KeyVaultClient>();
            await client.SetSecretWithHttpMessagesAsync($"https://{KeyVaultName}.vault.azure.net/", name, value);
        }
    }
}
