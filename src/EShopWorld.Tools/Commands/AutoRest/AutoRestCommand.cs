using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EShopWorld.Tools.Base;
using EShopWorld.Tools.Commands.AutoRest.Models;
using EShopWorld.Tools.Helpers;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// autorest command - top level
    /// </summary>
    [Command("autorest", Description = "Generates a rest client when targeted against a swagger version"), HelpOption] //todo when 2.3 McMaster.Extensions.CommandLineUtils use the new subcommand convention
    [Subcommand("run", typeof(Run))]
    public class AutoRestCommand : CommandBase
    {
        private int OnExecute(CommandLineApplication app, IConsole console)
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

        [Command("run", Description = "Generates the AutoRest Client Code")]
        internal class Run
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

            private int OnExecute(IConsole console)
            {             
                Directory.CreateDirectory(Output);
                // Initialize the necessary services
                var services = new ServiceCollection();
                AspNetRazorEngineServiceSetup.ConfigureDefaultServices<RenderProjectFileInternalCommand>(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                var provider = services.BuildServiceProvider();
                var serviceScope = provider.GetRequiredService<IServiceScopeFactory>();
                using (serviceScope.CreateScope())
                {
                    var swaggerInfo = SwaggerJsonParser.ParsetOut(SwaggerFile);
                    var projectFileName = swaggerInfo.Item1 + ".csproj";

                    //generate project file
                    var projectFileCommand = provider.GetRequiredService<RenderProjectFileInternalCommand>();
                    projectFileCommand.Render(new ProjectFileViewModel { TFMs = TFMs.ToArray(), ProjectName = swaggerInfo.Item1, Version = swaggerInfo.Item2 }, Path.Combine(Output, projectFileName));
                }

                return 0;
            }

        }
    }
}
