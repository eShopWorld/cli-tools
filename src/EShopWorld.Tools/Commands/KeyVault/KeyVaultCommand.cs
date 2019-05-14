using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.KeyVault
{
    /// <summary>
    /// key vault top level command 
    /// </summary>
    
    [Command("keyvault", Description = "keyvault associated functionality"), HelpOption]
    [Subcommand(typeof(GeneratePOCOsCommand))]
    [Subcommand(typeof(ExportKeyVaultCommand))]
    public class KeyVaultCommand
    {
        /// <summary>
        /// output appropriate message to denote sub-command is missing
        /// </summary>
        /// <param name="app">app instance</param>
        /// <param name="console">console</param>
        /// <returns></returns>
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {            
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }

    }
}
