using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Transforms;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools
{
    /// <summary>
    /// Dotnet CLI extension entry point.
    /// </summary>
    [Command(Name = "esw", Description = "eShopWorld CLI tool set")]
    [Subcommand("transform", typeof(Resx2JsonCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    //[Subcommand("autorest", typeof(AutoRestCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
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
            console.Error.WriteLine("You must specify a subcommand to execute.");
            app.ShowHelp();
            return 1;
        }
    }
}
