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

        /// <summary>
        /// boolean flag to indicate secondary key should be used instead of primary
        /// </summary>
        [Option(
            Description = "flag indicating to use secondary key  - applicable for Cosmos Db resources",
            ShortName = "2",
            LongName = "secondary",
            ShowInHelpText = true)]
        public bool UseSecondaryKey { get; set; }

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
                GetCompositeCommand<AzScanDNSCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanKustoCommand>().OnExecuteAsync(app, console));

            return 0;
        }

        private T GetCompositeCommand<T>() where T : AzScanCommandBase
        {
            var instance = (T) _serviceProvider.GetService(typeof(T));

            instance.Subscription = Subscription;
            instance.Domain = Domain;

            if (instance is AzScanCosmosDbCommand cosmosCommand)
            {
                cosmosCommand.UseSecondaryKey = UseSecondaryKey;
            }

            return instance;
        }
    }
}
