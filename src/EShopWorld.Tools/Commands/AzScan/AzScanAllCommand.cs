using System;
using System.Threading.Tasks;
using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// scan all meta command, delegating to commands serving particular resources
    /// </summary>
    [Command("all", Description = "scans all supported resources and projects their configuration into KV")]
    public class AzScanAllCommand : AzScanCommandBase
    {
        public AzScanAllCommand(Azure.IAuthenticated authenticated, KeyVaultClient keyVaultClient, IBigBrother bigBrother) : base(authenticated, keyVaultClient, bigBrother)
        {
        }

        public override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await Task.WhenAll(GetCompositeCommand<AzScanSqlCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzCosmosDbScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzRedisScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzServiceBusScanCommand>().OnExecuteAsync(app, console));

            return 1;
        }

        private T GetCompositeCommand<T>() where T : AzScanCommandBase
        {
            var instance = (T) Activator.CreateInstance(typeof(T), Authenticated, KeyVaultClient, BigBrother);

            instance.KeyVaultName = KeyVaultName;
            instance.Environment = Environment;
            instance.Regex = Regex;
            instance.ResourceGroup = ResourceGroup;

            return instance;
        }
    }
}
