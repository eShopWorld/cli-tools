using System.Reflection;

namespace EShopWorld.Tools.Base
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
        protected string GetVersion() => GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
