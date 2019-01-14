using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// azscan prefix definition command
    /// </summary>
    [Command("azscan", Description = "azure resource configuration management scan"), HelpOption]
    [Subcommand(typeof(AzServiceBusScanCommand))]
    [Subcommand(typeof(AzCosmosDbScanCommand))]
    [Subcommand(typeof(AzRedisScanCommand))]
    [Subcommand(typeof(AzScanSqlCommand))]
    [Subcommand(typeof(AzScanAllCommand))]
    [Subcommand(typeof(AzScanAppInsightsCommand))]
    public class AzScanCommand
    {
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }
    }
}
