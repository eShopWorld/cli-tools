using EShopWorld.Tools.Commands.AutoRest.Views;
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
using System.Reflection;

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
            [Option("-s|--swagger <name>",
                Description = "url to the swagger JSON file",
                ShowInHelpText = true)]
            [Required]
            public string SwaggerFile { get; set; }

            [Option("-o|--output <outputFolder>",
                Description = "output folder path to generate files into",
                ShowInHelpText = true)]
            [Required]
            public string Output { get; set; }

            [Option("-t|--tfm <tfm>",
                Description = "Target framework moniker name (default: netstandard1.5)",
                ShowInHelpText = true)]
            public List<string> TFMs { get; set; }

            private int OnExecute(IConsole console)
            {
                //validation first for required params
                //if (!SwaggerFile.HasValue())
                //{
                //    ValidationSucceeded = false;
                //    ValidationErrors = new List<string>(new[] {"Required parameter 'swagger file' missing (-s). See help (-h) for details"});
                //    return -1;
                //}

                //if (!outputFolderOption.HasValue())
                //{
                //    ValidationSucceeded = false;
                //    ValidationErrors = new List<string>(new[]
                //        {"Required parameter 'output' missing (-o). See help (-h) for details"});
                //    return -1;
                //}

                ////pass back to execution
                //IsHelp = help.HasValue();
                //SwaggerJsonUrl = swaggerFileOption.Value();
                //OutputFolder = outputFolderOption.Value();
                //TFMs = tfmOption.Values == null || tfmOption.Values.Count == 0 ? new[] { "net462", "netstandard2.0" }.ToList() : tfmOption.Values;

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
