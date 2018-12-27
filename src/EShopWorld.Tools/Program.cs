using System.Threading.Tasks;
using EShopWorld.Tools.Commands.AutoRest;
using McMaster.Extensions.CommandLineUtils;
using EShopWorld.Tools.Commands;
using EShopWorld.Tools.Commands.AzScan;
using EShopWorld.Tools.Commands.KeyVault;
using EShopWorld.Tools.Commands.Transform;

namespace EShopWorld.Tools
{
    /// <summary>
    /// Dotnet CLI extension entry point.
    /// </summary>
    [Command(Name = "esw", Description = "eShopWorld CLI tool set")]
    [Subcommand(typeof(TransformCommand))]
    [Subcommand(typeof(AutoRestCommand))]
    [Subcommand(typeof(KeyVaultCommand))]
    [Subcommand(typeof(AzScanCommand))]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public class Program : CommandBase
    {
        /// <summary>
        /// Dotnet CLI extension entry point.
        /// </summary>
        /// <param name="args">The list of arguments for this extension.</param>
        /// <returns>Executable exit code.</returns>
        public static void Main(string[] args)
        {
            CommandLineApplication.ExecuteAsync<Program>(args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="console"></param>
        /// <returns></returns>
        protected override async Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command to execute.");
            app.ShowHelp();
            return 1;
        }
    }
}
