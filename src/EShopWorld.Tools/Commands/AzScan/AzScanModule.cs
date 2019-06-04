using System.Security.Cryptography.X509Certificates;
using Autofac;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// AutoFac module fo AzScan family
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class AzScanModule : Module
    {
        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzScanSqlCommand>();
            builder.RegisterType<AzScanCosmosDbCommand>();
            builder.RegisterType<AzScanRedisCommand>();
            builder.RegisterType<AzScanServiceBusCommand>();
            builder.RegisterType<AzScanAppInsightsCommand>();
            builder.RegisterType<AzScanDNSCommand>();
            builder.RegisterType<AzScanKustoCommand>();
            builder.RegisterType<AzScanKeyVaultManager>();
            builder.RegisterType<AzScanEnvironmentInfoCommand>();
            builder.RegisterType<ServiceFabricDiscoveryFactory>();
            builder.Register(c =>
            {
                var _x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                _x509Store.Open(OpenFlags.ReadWrite);

                return _x509Store;
            }).SingleInstance();
        }
    }
}
