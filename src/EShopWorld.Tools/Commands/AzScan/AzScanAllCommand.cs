using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// scan all meta command, delegating to commands serving particular resources
    /// </summary>
    [Command("all", Description = "scans all supported resources and projects their configuration into KV")]
    public class AzScanAllCommand : AzScanKeyRotationCommandBase
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> instance to locate inner commands</param>
        public AzScanAllCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        /// <inheritdoc />
        public override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await Task.WhenAll(
                GetCompositeCommand<AzScanSqlCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanCosmosDbCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanRedisCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanServiceBusCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanAppInsightsCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanDNSCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanKustoCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanEnvironmentInfoCommand>().OnExecuteAsync(app, console));

            return 0;
        }

        /// <inheritdoc />
        protected override Task<int> RunScanAsync(IAzure client, IConsole console)
        {
            throw new NotImplementedException();
        }

        private T GetCompositeCommand<T>() where T : AzScanCommandBase
        {
            var instance = (T) _serviceProvider.GetService(typeof(T));

            instance.Subscription = Subscription;
            instance.Domain = Domain;

            if (instance is AzScanKeyRotationCommandBase cosmosCommand)
            {
                cosmosCommand.UseSecondaryKey = UseSecondaryKey;
            }

            return instance;
        }
    }
}
