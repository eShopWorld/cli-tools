using McMaster.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Common;
using EShopWorld.Tools.Telemetry;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// autorest command - top level
    /// </summary>
    [Command("autorest", Description = "AutoRest associated functionality"), HelpOption]
    [Subcommand(typeof(GenerateProjectFileCommand))]
    public class AutoRestCommand
    {
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }

        [Command("generateProjectFile", Description = "Generates project file for the Autorest generated code")]
        internal class GenerateProjectFileCommand 
        {
            private readonly IBigBrother _bigBrother;

            public GenerateProjectFileCommand(IBigBrother bigBrother)
            {
                _bigBrother = bigBrother;
            }

            [Option(
                Description = "url to the swagger JSON file",
                ShortName = "s",
                LongName = "swagger",
                ShowInHelpText = true)]
            [Required]
            public string SwaggerFile { get; set; }

            [Option(
                Description = "output folder path to generate files into",
                ShortName = "o",
                LongName = "output",
                ShowInHelpText = true)]
            [Required]
            public string Output { get; set; }

            [Option(
                Description = "Target framework moniker name (default: net462, netstandard2.0)",
                ShortName = "t",
                LongName = "tfm",
                ShowInHelpText = true)]
            public List<string> TFMs { get; set; } = new[] {"net462", "netstandard2.0"}.ToList();

            public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {             
                Directory.CreateDirectory(Output);
                
                var swaggerInfo = SwaggerJsonParser.ParsetOut(SwaggerFile);
                var projectFileName = swaggerInfo.Item1 + ".csproj";

                //generate project file
                var csproj = ProjectFileBuilder.CreateEswNetStandard20NuGet(swaggerInfo.Item1, swaggerInfo.Item2,
                        $"client side library for {swaggerInfo.Item1} API", "net462;netstandard2.0")
                    .WithItemGroup()
                    .WithReference("System.Net.Http")
                    .WithPackageReference("Microsoft.Rest.ClientRuntime", "2.3.19")
                    .WithPackageReference("Newtonsoft.Json", "12.0.1")
                    .Attach();

                File.WriteAllText(Path.Combine(Output, projectFileName), csproj.GetContent());

                _bigBrother?.Publish(new AutorestProjectFileGenerated{SwaggerFile = SwaggerFile});

                return Task.FromResult(0);
            }
        }
    }
}
