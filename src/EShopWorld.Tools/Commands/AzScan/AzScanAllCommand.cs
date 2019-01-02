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
        protected internal override async Task<int> InternalExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await Task.WhenAll(GetCompositeCommand<AzScanSqlCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzCosmosDbScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzRedisScanCommand>().OnExecuteAsync(app, console),
                GetCompositeCommand<AzServiceBusScanCommand>().OnExecuteAsync(app, console));

            return 1;
        }

        private T GetCompositeCommand<T>() where T : AzScanCommandBase
        {
            var instance = Activator.CreateInstance<T>();

            instance.KeyVaultName = KeyVaultName;
            instance.Environment = Environment;
            instance.Regex = Regex;
            instance.ResourceGroup = ResourceGroup;

            return instance;
        }
    }
}
