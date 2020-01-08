using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Eshopworld.Core;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Kusto;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// Azure Kusto configuration management scan process
    /// </summary>
    [Command("kusto", Description = "scans Kusto resources and projects their configuration to KV")]
    public class AzScanKustoCommand : AzScanCommandBase
    {
        private readonly KustoManagementClient _kustoClient;

        /// <inheritdoc />
        public AzScanKustoCommand(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager, IBigBrother bigBrother, KustoManagementClient kustoClient):base(authenticated, keyVaultManager, bigBrother, "Kusto")
        {
            _kustoClient = kustoClient;
        }

        /// <inheritdoc />
        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            //identify target subs to scan
            var subs = await GetTargetSubscriptions();
            //scan - if cluster found, attempt to identity the db by name
            foreach (var sub in subs)
            {
                _kustoClient.SubscriptionId = sub;
                var kustos = await _kustoClient.Clusters.ListAsync();
                //see https://github.com/Azure/azure-docs-sdk-dotnet/issues/1117 - this may need to be adjusted
                foreach (var kusto in kustos.Where(k=>k.State.Equals("running", StringComparison.OrdinalIgnoreCase))) 
                {
                    //forward looking (non catching) kusto name followed by the actual environmental db
                    var expectedDbName = $"(?<={kusto.Name}/){Domain}-{EnvironmentName}"; 
                    var rgName = GetResourceGroupName(kusto.Id);
                    var dbs = await _kustoClient.Databases.ListByClusterAsync(rgName, kusto.Name);

                    Match match=null;
                    var foundInstance = dbs.FirstOrDefault(db => (match = Regex.Match(db.Name, expectedDbName, RegexOptions.IgnoreCase)).Success);

                    if (foundInstance == null) continue;

                    //if appropriate db found, emit secrets - to all regional KVs
                    foreach (var keyVault in DomainResourceGroup.TargetKeyVaults)
                    {
                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix,
                            match.Value,
                            "ClusterUri",
                            kusto.Uri);
                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix,
                            match.Value,
                            "ClusterIngestionUri",
                            kusto.DataIngestionUri);
                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix,
                            match.Value,
                            "TenantId",
                            Authenticated.TenantId);
                        await KeyVaultManager.SetKeyVaultSecretAsync(keyVault, SecretPrefix,
                            match.Value,
                            "DBName",
                            match.Value);
                    }
                }
            }           

            return 0;
        }

        internal static string GetResourceGroupName([NotNull] string clusterId)
        {
            var match = Regex.Match(clusterId, "(?<=/resourceGroups/)([^/])+"); //take all between '/'s
            if (!match.Success)
            {
                throw new ArgumentException("unexpected kusto cluster metadata", nameof(clusterId));
            }

            return match.Value;
        }

        private async Task<IEnumerable<string>> GetTargetSubscriptions()
        {
            if (!EnvironmentName.Equals("prod", StringComparison.OrdinalIgnoreCase))
            {
                var defaultSubClient = Authenticated.WithDefaultSubscription();
                var subs = await defaultSubClient.Subscriptions.ListAsync();

                return subs.Where(s => !"evo-prod".Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.SubscriptionId);
            }

            return new[] {(await GetSubscriptionDetails(Subscription)).subscriptionId};
        }
    }
}
