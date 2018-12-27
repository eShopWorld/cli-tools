using System.Diagnostics;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.AzScan
{
    [Command("azscan", Description = "azure resource configuration management scan"), HelpOption]
    [Subcommand(typeof(AzSBScanCommand))]
    public class AzScanCommand : CommandBase
    {
        protected override async Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a subcommand");
            app.ShowHelp();

#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif
            return 1;
        }
    }
}
