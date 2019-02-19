using System;
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
    /// Azure SQL scan command
    /// </summary>
    [Command("sql", Description = "scans and projects SQL configuration into KV")]
    public class AzScanSqlCommand : AzScanCommandBase
    {
        public AzScanSqlCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

        protected override async Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            foreach (var rg in RegionalPlatformResourceGroups)
            {
                var sqls = await client.SqlServers.ListByResourceGroupAsync(rg.Name);

                foreach (var sql in sqls)
                {
                    foreach (var db in sql.Databases.List()
                        .Where(db => !db.Name.Equals("master", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!db.RegionName.RegionNameCheck(rg.Region.ToRegionCode()))
                            continue;

                        foreach (var keyVault in rg.TargetKeyVaults)
                        {
                            await KeyVaultClient.SetKeyVaultSecretAsync(keyVault, "SQL",
                                $"{sql.Name}{db.Name}",
                                "ConnectionString",
                                $"Server=tcp:{sql.FullyQualifiedDomainName}; Database={db.Name};Trusted_Connection=False; Encrypt=True; MultipleActiveResultSets=True;");
                        }
                    }
                }
            }

            return 0;
        }
    }
}
