using System;
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
using EShopWorld.Tools.Common;
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

            CommandLineApplication commandParsed=null;

            EswCliToolCommandExecutionTimedEvent timedEvent = null;
            app.OnParsingComplete(result =>
                {
                    commandParsed = result.SelectedCommand;
                    timedEvent = new EswCliToolCommandExecutionTimedEvent
                    {
                        CommandType = commandParsed?.GetType().FullName ?? app.GetType().FullName,
                        Arguments = commandParsed?.Options.ToConsoleString()
                    };
                }
            );

            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(SetupAutofac());

            try
            {
                _bigBrother = app.GetService<IBigBrother>();
                _console = app.GetService<IConsole>();
                int retCode;
                if ((retCode = app.Execute(args)) != 0)
                {
                    _console.EmitWarning(_bigBrother, commandParsed?.GetType() ?? app.GetType(), commandParsed?.Options,
                        $"Command returned non zero code - code returned : {retCode}");
                }

                return retCode;
            }
            catch (Exception e)
            {
                _console.EmitException(_bigBrother, e, commandParsed?.GetType() ?? app.GetType(),
                    commandParsed?.Options);

                return -1;
            }
            finally
            {
                if (timedEvent != null)
                {
                    _bigBrother?.Publish(timedEvent);
                    _bigBrother?.Flush();
                }
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
