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

        // ReSharper disable once InconsistentNaming
        protected internal virtual void ConfigureDI()
        {

        }

        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            ConfigureDI();
            return InternalExecute(app, console);
        }

        protected abstract int InternalExecute(CommandLineApplication app, IConsole console);
    }
}
