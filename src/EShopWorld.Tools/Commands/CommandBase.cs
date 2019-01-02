using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.DevOps;
using Eshopworld.Telemetry;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace EShopWorld.Tools.Commands
{
    /// <summary>
    /// base class for all commands
    /// </summary>
    public abstract class CommandBase
    {
        /// <summary>
        /// Gets the version of the assembly of the class calling this
        /// </summary>
        /// <returns></returns>
        protected string GetVersion() => GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        internal readonly IServiceCollection ServiceCollection = new ServiceCollection();

        private IServiceProvider _serviceProvider;

        protected internal IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                    _serviceProvider = ServiceCollection.BuildServiceProvider();


                return _serviceProvider;
            }
        }

        protected internal IBigBrother BigBrother => ServiceProvider.GetService<IBigBrother>();

        // ReSharper disable once InconsistentNaming
        protected internal virtual void ConfigureDI(IConsole console)
        {
            var config = EswDevOpsSdk.BuildConfiguration();
            ServiceCollection.AddSingleton(config);
            ServiceCollection.AddSingleton<IBigBrother>(new BigBrother(config["AIKey"], config["InternalAIKey"]));
        }

        /// <summary>
        /// cli framework entry-point
        ///
        /// handle instrumentation for exceptions
        /// </summary>
        /// <param name="app">app object instance</param>
        /// <param name="console">console wrapper</param>
        /// <returns>return code of the command</returns>
        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            ConfigureDI(console);
            try
            {
                return await InternalExecuteAsync(app, console);
            }
            catch (Exception e)
            {
                console?.Error.WriteLine($"Error detected - {e.Message}");
                var bb = ServiceProvider.GetService<IBigBrother>();

                var @event = e.ToExceptionEvent<CLIExceptionEvent>();
                @event.CommandType = GetType().ToString();
                @event.Arguments = string.Join(',', app.Options.Select(t => $"{t.LongName}-'{t.Value()}'"));

                bb?.Publish(@event);
                bb?.Flush();

                return 1;
            }
        }

        protected internal abstract Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console);
    }
}
