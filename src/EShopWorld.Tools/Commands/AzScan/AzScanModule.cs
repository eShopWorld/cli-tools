using Autofac;

namespace EShopWorld.Tools.Commands.AzScan
{
    public class AzScanModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzScanSqlCommand>();
            builder.RegisterType<AzScanCosmosDbCommand>();
            builder.RegisterType<AzScanRedisCommand>();
            builder.RegisterType<AzScanServiceBusCommand>();
            builder.RegisterType<AzScanAppInsightsCommand>();
            builder.RegisterType<AzScanDNSCommand>();
            builder.RegisterType<AzScanKustoCommand>();

        }
    }
}
