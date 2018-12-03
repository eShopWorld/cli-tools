using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EShopWorld.Tools.Commands.AutoRest.Models;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// 
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
        private class Run
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
                Description = "Target framework moniker name (default: netstandard1.5)",
                ShortName = "t",
                LongName = "tfm",
                ShowInHelpText = true)]
            public List<string> TFMs { get; set; }

            private int OnExecute(IConsole console)
            {
                if (TFMs == null)
                {
                    TFMs = new[] { "net462", "netstandard2.0" }.ToList();
                }

                // Initialize the necessary services
                var services = new ServiceCollection();
                ConfigureDefaultServices(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                var provider = services.BuildServiceProvider();
                var serviceScope = provider.GetRequiredService<IServiceScopeFactory>();
                using (serviceScope.CreateScope())
                {
                    var swaggerInfo = SwaggerJsonParser.ParsetOut(SwaggerFile);
                    var projectFileName = swaggerInfo.Item1 + ".csproj";

                    //generate project file
                    var projectFileCommand = provider.GetRequiredService<RenderProjectFileCommand>();
                    projectFileCommand.Render(new ProjectFileViewModel { TFMs = TFMs.ToArray(), ProjectName = swaggerInfo.Item1, Version = swaggerInfo.Item2 }, Path.Combine(Output, projectFileName));
                }

                return 0;
            }

            public static void ConfigureDefaultServices(IServiceCollection services, string customApplicationBasePath)
            {
                var applicationEnvironment = PlatformServices.Default.Application;

                services.AddSingleton(applicationEnvironment);
                services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

                IFileProvider fileProvider;
                string applicationName;
                if (!string.IsNullOrEmpty(customApplicationBasePath))
                {
                    applicationName = Assembly.GetEntryAssembly().GetName().Name;
                    fileProvider = new PhysicalFileProvider(customApplicationBasePath);
                }
                else
                {
                    applicationName = Assembly.GetEntryAssembly().GetName().Name;
                    fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
                }

                services.AddSingleton<IHostingEnvironment>(new HostingEnvironment
                {
                    ApplicationName = applicationName,
                    WebRootFileProvider = fileProvider,
                });

                services.Configure<RazorViewEngineOptions>(options =>
                {
                    options.FileProviders.Clear();
                    options.FileProviders.Add(fileProvider);
                });

                var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
                services.AddSingleton<DiagnosticSource>(diagnosticSource);
                services.AddLogging();
                services.AddMvc();
                services.AddTransient<RenderProjectFileCommand>();
            }
        }
    }
}
