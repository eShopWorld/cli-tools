using Autofac;

namespace EShopWorld.Tools.Commands.AzScan
{
    public class AzScanModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzScanSqlCommand>();
            builder.RegisterType<AzCosmosDbScanCommand>();
            builder.RegisterType<AzRedisScanCommand>();
            builder.RegisterType<AzServiceBusScanCommand>();
            builder.RegisterType<AzScanAppInsightsCommand>();
        }
    }
}
