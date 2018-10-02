using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// 
    /// </summary>
    [Command("autorest", Description = "Generates a rest client when targeted against a swagger version"), HelpOption] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
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
            [Option("-s|--swagger <name>", Description = "url to the swagger JSON file")]
            [Required]
            public string SwaggerFile { get; set; }

            [Option("-o|--output <outputFolder>", Description = "output folder path to generate files into")]
            [Required]
            public string Output { get; set; }

            [Option("-t|--tfm <tfm>", Description = "Target framework moniker name (default: netstandard1.5)")]
            public string TFMs { get; set; }

            /// <summary>
            /// flag to contain options validation
            /// </summary>
            public bool ValidationSucceeded { get; set; } = true;

            /// <summary>
            /// list of validation errors
            /// </summary>
            public IList<string> ValidationErrors { get; set; }

            private int OnExecute(IConsole console)
            {
                //validation first for required params
                if (!SwaggerFile.HasValue())
                {
                    ValidationSucceeded = false;
                    ValidationErrors = new List<string>(new[]
                        {"Required parameter 'swagger file' missing (-s). See help (-h) for details"});
                    return -1;
                }

                if (!outputFolderOption.HasValue())
                {
                    ValidationSucceeded = false;
                    ValidationErrors = new List<string>(new[]
                        {"Required parameter 'output' missing (-o). See help (-h) for details"});
                    return -1;
                }

                //pass back to execution
                IsHelp = help.HasValue();
                SwaggerJsonUrl = swaggerFileOption.Value();
                OutputFolder = outputFolderOption.Value();
                TFMs = tfmOption.Values == null || tfmOption.Values.Count == 0 ? new[] { "net462", "netstandard2.0" }.ToList() : tfmOption.Values;
                return 0;
            }
        }
    }
}
