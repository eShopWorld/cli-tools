using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// Azure SQL scan command
    /// </summary>
    [Command("sql", Description = "scans and projects SQL configuration into KV")]
    public class AzScanSqlCommand : AzScanCommandBase
    {
        protected override async Task<int> RunScanAsync(IAzure client)
        {
            var sqls = string.IsNullOrWhiteSpace(ResourceGroup)
                ? await client.SqlServers.ListAsync()
                : await client.SqlServers.ListByResourceGroupAsync(ResourceGroup);

            foreach (var sql in sqls)
            {
                foreach (var db in sql.Databases.List()
                    .Where(db => !db.Name.Equals("master", StringComparison.OrdinalIgnoreCase)))
                {
                    var connStr =
                        $"Server=tcp:{sql.FullyQualifiedDomainName}; Database={db.Name}; User ID=TBA; Password=TBA; Trusted_Connection=False; Encrypt=True; MultipleActiveResultSets=True;";
                    //TODO: decide key name here since most characters are not allowed (e.g. .)
                    //await UpsertSecretToKVAsync($"{sql.FullyQualifiedDomainName}.{db.Name}", connStr); //TODO: naming here not consistent with devopsflex script ('SQLConnectionString' there)
                }
            }

            return 1;
        }
    }
}
