using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Common;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Kusto;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("kusto", Description = "scans Kusto resources and projects their configuration to KV")]
    public class AzScanKustoCommand : AzScanCommandBase
    {
        private readonly KustoManagementClient _kustoClient;

        public AzScanKustoCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother, KustoManagementClient kustoClient):base(authenticated, keyVaultClient, bigBrother)
        {
            _kustoClient = kustoClient;
        }

        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            //identify target subs to scan
            var subs = await GetTargetSubscriptions();
            //scan - if cluster found, attempt to identity the db by name
            foreach (var sub in subs)
            {
                _kustoClient.SubscriptionId = sub;
                var kustos = await _kustoClient.Clusters.ListAsync();
                
                foreach (var kusto in kustos)
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
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVault, "Kusto",
                            match.Value,
                            "ClusterUri",
                            kusto.Uri);
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVault, "Kusto",
                            match.Value,
                            "ClusterIngestionUri",
                            kusto.DataIngestionUri);
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVault, "Kusto",
                            match.Value,
                            "TenantId",
                            Authenticated.TenantId);
                        await KeyVaultClient.SetKeyVaultSecretAsync(keyVault, "Kusto",
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

            return new[] {await GetSubscriptionId(Subscription)};
        }
    }
}
