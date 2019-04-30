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
    [Subcommand(typeof(AzScanKustoCommand))]
    [Subcommand(typeof(AzScanEnvironmentInfoCommand))]
    public class AzScanCommand
    {
        /// <summary>
        /// bare azscan command logic
        ///
        /// note that sub-command is required
        /// </summary>
        /// <param name="app">app instance</param>
        /// <param name="console">console instance</param>
        /// <returns></returns>
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(0);
        }
    }
}
