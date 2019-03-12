﻿using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// scan all meta command, delegating to commands serving particular resources
    /// </summary>
    [Command("all", Description = "scans all supported resources and projects their configuration into KV")]
    public class AzScanAllCommand : AzScanKeyRotationCommandBase
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
                GetCompositeCommand<AzScanDNSCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanKustoCommand>().OnExecuteAsync(app, console));

            return 0;
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
