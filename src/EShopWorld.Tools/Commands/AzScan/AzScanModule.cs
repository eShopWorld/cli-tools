using Autofac;

namespace EShopWorld.Tools.Commands.AzScan
{
    public class AzScanModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzScanSqlCommand>();
            builder.RegisterType<AzCosmosDbScanCommand>();
            builder.RegisterType<AzScanRedisCommand>();
            builder.RegisterType<AzServiceBusScanCommand>();
            builder.RegisterType<AzScanAppInsightsCommand>();
            builder.RegisterType<AzScanDNSCommand>();
        }
    }
}
