using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// base class for all Az Scan family commands
    /// </summary>
    public abstract class AzScanCommandBase
    {
        protected readonly Azure.IAuthenticated Authenticated;
        protected readonly KeyVaultClient KeyVaultClient;
        protected readonly IBigBrother BigBrother;

        protected AzScanCommandBase()
        {

        }

        protected AzScanCommandBase(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient,
            IBigBrother bigBrother)
        {
            Authenticated = authenticated;
            KeyVaultClient = keyVaultClient;
            BigBrother = bigBrother;
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

        internal IEnumerable<ResourceGroupDescriptor> RegionalPlatformResourceGroups => RegionList.Select(r =>
                new ResourceGroupDescriptor
                {
                    Name =
                        $"platform-{EnvironmentName}-{r.ToRegionCode()}".ToLowerInvariant(),
                    TargetKeyVaults = new[]
                    {
                        $"esw-{Domain}-{EnvironmentName}-{r.ToRegionCode()}".ToLowerInvariant()
                    },
                    Region = r
                });

        internal ResourceGroupDescriptor DomainResourceGroup => new ResourceGroupDescriptor
        {
            Name = $"{Domain}-{EnvironmentName}".ToLowerInvariant(),
            TargetKeyVaults = RegionList.Select(r =>
                $"esw-{Domain}-{EnvironmentName}-{r.ToRegionCode()}".ToLowerInvariant())
        };


        internal IList<DeploymentRegion> RegionList =>
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

        public virtual async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            var sub = await GetSubscriptionId(Subscription);

            var subClient = Authenticated.WithSubscription(sub);

            return await RunScanAsync(subClient, console);
        }

        /// <summary>
        /// get subscription id for a given sub name
        /// </summary>
        /// <param name="name">subscription name</param>
        /// <returns>subscription id</returns>
        protected async Task<string> GetSubscriptionId(string name)
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

            return sub.SubscriptionId;
        }
        

        protected virtual Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            return Task.FromResult(0);
        }

        internal class ResourceGroupDescriptor
        {
            internal string Name { get; set; }
            internal IEnumerable<string> TargetKeyVaults { get; set; }
            internal DeploymentRegion Region { get; set; }
        }
    }
}
