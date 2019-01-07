using Autofac;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// specific registrations for autorest command group
    /// </summary>
    public class AutorestModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RenderProjectFileInternalCommand>();
        }
    }
}
