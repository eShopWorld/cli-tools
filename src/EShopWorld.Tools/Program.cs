using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Eshopworld.Core;
using EShopWorld.Tools.Commands.AutoRest;
using McMaster.Extensions.CommandLineUtils;
using EShopWorld.Tools.Commands.AzScan;
using EShopWorld.Tools.Commands.KeyVault;
using EShopWorld.Tools.Commands.Security;
using EShopWorld.Tools.Commands.Transform;
using EShopWorld.Tools.Telemetry;
using Microsoft.Extensions.DependencyInjection;

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
    [Subcommand(typeof(SecurityCommand))]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public class Program 
    {
        private static  IBigBrother _bigBrother;
        private static IConsole _console;

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
        public static int Main(string[] args)
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
                _console = app.GetService<IConsole>();
                return app.Execute(args);
            }
            catch (Exception e)
            {
                var @event = e.ToExceptionEvent<CLIExceptionEvent>();
                @event.CommandType = commandParsed;
                @event.Arguments = string.Join(',', app.Options.Select(t => $"{t.LongName}-'{t.Value()}'"));

                _console.ForegroundColor = ConsoleColor.Red;
                _console.Error.WriteLine($"Command {commandParsed} produced an error {e.Message}");
                _console.ResetColor();

                _bigBrother?.Publish(@event);
                _bigBrother?.Flush();

                return -1;
            }
        }

        private static IServiceProvider SetupAutofac()
        {           
            var f = new AutofacServiceProviderFactory();

            var builder = new ContainerBuilder();        
            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
          
            return f.CreateServiceProvider(builder);
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
