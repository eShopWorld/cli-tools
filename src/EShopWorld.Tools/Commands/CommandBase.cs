using System;
using System.Reflection;
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

        internal IServiceCollection ServiceCollection = new ServiceCollection();

        private IServiceProvider _serviceProvider;

        public IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                    _serviceProvider = ServiceCollection.BuildServiceProvider();

                return _serviceProvider;
            }
        }
        // ReSharper disable once InconsistentNaming
        protected internal virtual void ConfigureDI(IConsole console)
        {

        }

        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            ConfigureDI(console);
            return InternalExecute(app, console);
        }

        protected abstract int InternalExecute(CommandLineApplication app, IConsole console);
    }
}
