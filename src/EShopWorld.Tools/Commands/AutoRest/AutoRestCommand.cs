using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EShopWorld.Tools.Commands.AutoRest.Models;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// autorest command - top level
    /// </summary>
    [Command("autorest", Description = "AutoRest associated functionality"), HelpOption]
    [Subcommand(typeof(GenerateProjectFileCommand))]
    public class AutoRestCommand : CommandBase
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

        [Command("generateProjectFile", Description = "Generates project file for the Autorest generated code")]
        internal class GenerateProjectFileCommand : RazorCommandBase
        {
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

            protected internal override void ConfigureDI(IConsole console)
            {
                base.ConfigureDI(console);
                ServiceCollection.AddSingleton<RenderProjectFileInternalCommand>();
            }

            protected override async Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console)
            {             
                Directory.CreateDirectory(Output);
                
                var swaggerInfo = SwaggerJsonParser.ParsetOut(SwaggerFile);
                var projectFileName = swaggerInfo.Item1 + ".csproj";

                //generate project file
                var projectFileCommand = ServiceProvider.GetRequiredService<RenderProjectFileInternalCommand>();
                projectFileCommand.Render(new ProjectFileViewModel { TFMs = TFMs.ToArray(), ProjectName = swaggerInfo.Item1, Version = swaggerInfo.Item2 }, Path.Combine(Output, projectFileName));

                BigBrother?.Publish(new AutorestProjectFileGenerated{SwaggerFile = SwaggerFile});

                return 0;
            }
        }
    }
}
