using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <inheritdoc />
    /// <summary>
    /// azscan prefix definition command
    /// </summary>
    [Command("azscan", Description = "azure resource configuration management scan"), HelpOption]
    [Subcommand(typeof(AzServiceBusScanCommand))]
    [Subcommand(typeof(AzCosmosDbScanCommand))]
    [Subcommand(typeof(AzRedisScanCommand))]
    [Subcommand(typeof(AzScanSqlCommand))]
    [Subcommand(typeof(AzScanAllCommand))]
    public class AzScanCommand : CommandBase
    {
        protected internal override Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }
    }
}
