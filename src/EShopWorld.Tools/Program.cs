using EShopWorld.Tools.Commands.AutoRest;
using McMaster.Extensions.CommandLineUtils;
using System.Diagnostics;
using EShopWorld.Tools.Base;
using EShopWorld.Tools.Commands.KeyVault;
using EShopWorld.Tools.Commands.Transform;

namespace EShopWorld.Tools
{
    /// <summary>
    /// Dotnet CLI extension entry point.
    /// </summary>
    [Command(Name = "esw", Description = "eShopWorld CLI tool set")]
    [Subcommand("transform", typeof(Resx2JsonCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [Subcommand("autorest", typeof(AutoRestCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [Subcommand("keyvault", typeof(KeyVaultCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public class Program : CommandBase
    {
        /// <summary>
        /// Dotnet CLI extension entry point.
        /// </summary>
        /// <param name="args">The list of arguments for this extension.</param>
        /// <returns>Executable exit code.</returns>
        public static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="console"></param>
        /// <returns></returns>
        private int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command to execute.");
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
