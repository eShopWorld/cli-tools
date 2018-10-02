using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Transforms;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools
{
    /// <summary>
    /// Dotnet CLI extension entry point.
    /// </summary>
    [Command(Name = "esw", Description = "eshopWorld CLI tool set")]
    [Subcommand("transform", typeof(Resx2JsonCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [Subcommand("autorest", typeof(AutoRestCommand))] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public class Program : CommandBase
    {
        /// <summary>
        /// Dotnet CLI extension entry point.
        /// </summary>
        /// <param name="args">The list of arguments for this extension.</param>
        /// <returns>Executable exit code.</returns>
        public static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify at a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}
