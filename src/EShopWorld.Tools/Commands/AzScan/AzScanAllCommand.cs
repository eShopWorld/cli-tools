using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// scan all meta command, delegating to commands serving particular resources
    /// </summary>
    [Command("all", Description = "scans all supported resources and projects their configuration into KV")]
    public class AzScanAllCommand : AzScanCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        public AzScanAllCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await Task.WhenAll(
                GetCompositeCommand<AzScanSqlCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanCosmosDbCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanRedisCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanServiceBusCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanAppInsightsCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanDNSCommand>().OnExecuteAsync(app, console));

            return 1;
        }

        private T GetCompositeCommand<T>() where T : AzScanCommandBase
        {
            var instance = (T) _serviceProvider.GetService(typeof(T));

            instance.KeyVaultName = KeyVaultName;
            instance.Subscription = Subscription;
            instance.ResourceGroup = ResourceGroup;
            instance.Region = Region;

            return instance;
        }
    }
}
