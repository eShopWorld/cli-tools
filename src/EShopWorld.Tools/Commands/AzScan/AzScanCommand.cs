using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// azscan prefix definition command
    /// </summary>
    [Command("azscan", Description = "azure resource configuration management scan"), HelpOption]
    [Subcommand(typeof(AzScanServiceBusCommand))]
    [Subcommand(typeof(AzScanCosmosDbCommand))]
    [Subcommand(typeof(AzScanRedisCommand))]
    [Subcommand(typeof(AzScanSqlCommand))]
    [Subcommand(typeof(AzScanAllCommand))]
    [Subcommand(typeof(AzScanAppInsightsCommand))]
    [Subcommand(typeof(AzScanDNSCommand))]
    public class AzScanCommand
    {
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(0);
        }
    }
}
