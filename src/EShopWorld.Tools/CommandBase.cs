using McMaster.Extensions.CommandLineUtils;
using System.Reflection;

namespace EShopWorld.Tools
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CommandBase
    {
        /// <summary>
        /// Gets the version of the assembly of the class calling this
        /// </summary>
        /// <returns></returns>
        protected string GetVersion() => this.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
