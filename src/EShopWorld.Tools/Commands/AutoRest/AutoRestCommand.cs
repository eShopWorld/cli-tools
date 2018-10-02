using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// 
    /// </summary>
    [Command("autorest", Description = "Does autorest stuff"), HelpOption] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [Subcommand("run", typeof(Run))]
    public class AutoRestCommand : CommandBase
    {
        private int OnExecute(IConsole console)
        {
            console.Error.WriteLine("You must specify an action. See --help for more details.");
            return 1;
        }

        [Command("run", Description = "Generates the AutoRest Client Code")]
        private class Run
        {
            [Option("--uri")]
            [Required]
            public string URI { get; set; }

            private int OnExecute(IConsole console)
            {
                console.WriteLine("Do auto resty stuff");
                return 0;
            }
        }
    }
}
