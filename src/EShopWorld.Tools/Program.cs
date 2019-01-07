using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Eshopworld.Core;
using EShopWorld.Tools.Commands.AutoRest;
using McMaster.Extensions.CommandLineUtils;
using EShopWorld.Tools.Commands;
using EShopWorld.Tools.Commands.AzScan;
using EShopWorld.Tools.Commands.KeyVault;
using EShopWorld.Tools.Commands.Transform;
using EShopWorld.Tools.Telemetry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;

namespace EShopWorld.Tools
{
    /// <summary>
    /// Dotnet CLI extension entry point.
    /// </summary>
    [Command(Name = "esw", Description = "eShopWorld CLI tool set")]
    [Subcommand(typeof(TransformCommand))]
    [Subcommand(typeof(AutoRestCommand))]
    [Subcommand(typeof(KeyVaultCommand))]
    [Subcommand(typeof(AzScanCommand))]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public class Program 
    {
        private static  IBigBrother _bigBrother;

        /// <summary>
        /// Gets the version of the assembly of the class calling this
        /// </summary>
        /// <returns></returns>
        private string GetVersion() => GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        /// <summary>
        /// Dotnet CLI extension entry point.
        /// </summary>
        /// <param name="args">The list of arguments for this extension.</param>
        /// <returns>Executable exit code.</returns>
        public static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication<Program>();

            var commandParsed = string.Empty;

            app.OnParsingComplete(result => { commandParsed = result.SelectedCommand.GetType().ToString(); } );

            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(SetupAutofac());

            try
            {
                _bigBrother = app.GetService<IBigBrother>();
                app.Execute(args);
            }
            catch (Exception e)
            {
                //console?.Error.WriteLine($"Error detected - {e.Message}");

                var @event = e.ToExceptionEvent<CLIExceptionEvent>();
                @event.CommandType = commandParsed;
                @event.Arguments = string.Join(',', app.Options.Select(t => $"{t.LongName}-'{t.Value()}'"));

                _bigBrother?.Publish(@event);
                _bigBrother?.Flush();
            }
            
        }

        internal static IServiceProvider SetupAutofac()
        {           
            var f = new AutofacServiceProviderFactory();
            var builder = f.CreateBuilder(ASPNetContext);

            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
          
            return f.CreateServiceProvider(builder);
        }

        // ReSharper disable once InconsistentNaming
        private static IServiceCollection ASPNetContext
        {
            get
            {
                var sc = new ServiceCollection();

                var applicationEnvironment = PlatformServices.Default.Application;
                sc.AddSingleton(applicationEnvironment);

                sc.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

                var applicationName = Assembly.GetEntryAssembly().GetName().Name;
                IFileProvider fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                sc.AddSingleton<IHostingEnvironment>(new HostingEnvironment
                {
                    ApplicationName = applicationName,
                    WebRootFileProvider = fileProvider,
                });

                sc.Configure<RazorViewEngineOptions>(options =>
                {
                    options.FileProviders.Clear();
                    options.FileProviders.Add(fileProvider);
                });

                var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
                sc.AddSingleton<DiagnosticSource>(diagnosticSource);

                sc.AddLogging();
                sc.AddMvc();

                return sc;
            }
        }

        /// <summary>
        /// global command execution logic
        /// </summary>
        /// <param name="app"></param>
        /// <param name="console"></param>
        /// <returns></returns>
        protected internal Task<int> OnExecute(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command to execute.");
            app.ShowHelp();

            return Task.FromResult(1);
        }
    }
}
