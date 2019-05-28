using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// base class for all Az Scan family commands
    /// </summary>
    public abstract class AzScanCommandBase
    {
        protected readonly Azure.IAuthenticated Authenticated;
        protected readonly IBigBrother BigBrother;
        protected readonly AzScanKeyVaultManager KeyVaultManager;

        protected CommandLineApplication AppInstance;
        protected readonly string SecretPrefix;

        protected AzScanCommandBase()
        {

        }

        protected AzScanCommandBase(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager,
            IBigBrother bigBrother, string secretPrefix)
        {
            Authenticated = authenticated;
            KeyVaultManager = keyVaultManager;
            BigBrother = bigBrother;
            SecretPrefix = secretPrefix;
        }

        [Option(
            Description = "domain filter",
            ShortName = "d",
            LongName = "domain",
            ShowInHelpText = true)]
        [Required]
        public string Domain { get; set; }

        [Option(
            Description = "name of the subscription to scan ",
            ShortName = "s",
            LongName = "subscription",
            ShowInHelpText = true)]
        [Required]
        public string Subscription { get; set; }
        protected string SubscriptionId { get; set; }

        internal IEnumerable<ResourceGroupDescriptor> RegionalPlatformResourceGroups => RegionList.Select(r =>
                new ResourceGroupDescriptor
                {
                    Name = NameGenerator.GetRegionalPlatformRGName(EnvironmentName, r),
                    TargetKeyVaults = new[]
                    {
                       NameGenerator.GetDomainRegionalKVName(Domain, EnvironmentName, r)
                    },
                    Region = r
                });

        internal ResourceGroupDescriptor DomainResourceGroup => new ResourceGroupDescriptor
        {
            Name = NameGenerator.GetDomainRGName(Domain, EnvironmentName),
            TargetKeyVaults = RegionList.Select(r =>
                NameGenerator.GetDomainRegionalKVName(Domain, EnvironmentName, r))
        };


        private IEnumerable<DeploymentRegion> RegionList =>
            RegionHelper.DeploymentRegionsToList("ci".Equals(EnvironmentName, StringComparison.OrdinalIgnoreCase));

        protected string EnvironmentName
        {
            get
            {
                var lowerSub = Subscription.ToLowerInvariant();
                int dashIndex;
                if ((dashIndex = lowerSub.IndexOf('-')) == (-1))
                {
                    throw new ApplicationException($"Unrecognized subscription name : {Subscription}");
                }

                return lowerSub.Substring(dashIndex+1)
                    .Replace("sandbox", "sand", StringComparison.OrdinalIgnoreCase)
                    .Replace("preprod", "prep", StringComparison.OrdinalIgnoreCase); //return suffix and deal with know "anomalies"

            }
        }

        /// <summary>
        /// command execution logic
        /// </summary>
        /// <param name="app">application instance instance</param>
        /// <param name="console">console object</param>
        /// <returns><see cref="Task"/> with process result code</returns>
        public virtual async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            AppInstance = app;
            (SubscriptionId, Subscription) = await GetSubscriptionDetails(Subscription);
            var subClient = Authenticated.WithSubscription(SubscriptionId);

            await KeyVaultManager.AttachKeyVaults(DomainResourceGroup.TargetKeyVaults, SecretPrefix);

            var resultCode =  await RunScanAsync(subClient, console);

            if (resultCode == 0)
            {
                //only run this when command succeeded (and no exception)
                await KeyVaultManager.DetachKeyVaults();
            }

            return resultCode;
        }

        /// <summary>
        /// get subscription details for a given sub name - clean up (casing) of passed subscription name
        /// </summary>
        /// <param name="name">subscription name</param>
        /// <returns>subscription id, subscription name</returns>
        protected async Task<(string subscriptionName, string subscriptionId)> GetSubscriptionDetails(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("invalid value", nameof(name));
            }

            //look up subscription id for the given name
            var defaultSubClient = Authenticated.WithDefaultSubscription();

            var subs = await defaultSubClient.Subscriptions.ListAsync();
            var sub = subs.FirstOrDefault(s => name.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase));
            if (sub == null)
            {
                throw new ApplicationException($"Subscription {Subscription} not found. Check the account role setup.");
            }

            return (sub.SubscriptionId, sub.DisplayName);
        }

        protected abstract Task<int> RunScanAsync(IAzure client, IConsole console);
       

        internal class ResourceGroupDescriptor
        {
            internal string Name { get; set; }
            internal IEnumerable<string> TargetKeyVaults { get; set; }
            internal DeploymentRegion Region { get; set; }
        }
    }
}
