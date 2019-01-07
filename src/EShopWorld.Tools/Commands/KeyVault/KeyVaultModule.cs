using Autofac;

namespace EShopWorld.Tools.Commands.KeyVault
{
    public class KeyVaultModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GeneratePocoClassInternalCommand>();
            builder.RegisterType<GeneratePocoProjectInternalCommand>();
        }
    }
}
