﻿using System;
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
            var t = await Task.WhenAll(
                GetCompositeCommand<AzScanSqlCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzCosmosDbScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzRedisScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzServiceBusScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzScanAppInsightsCommand>().OnExecuteAsync(app, console));

            return 1;
        }

        private T GetCompositeCommand<T>() where T : AzScanCommandBase
        {
            var instance = (T) _serviceProvider.GetService(typeof(T));

            instance.KeyVaultName = KeyVaultName;
            instance.Subscription = Subscription;
            instance.ResourceGroup = ResourceGroup;

            return instance;
        }
    }
}
