using System;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Common;
using EShopWorld.Tools.Telemetry;
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
        /// <param name="bb"><see cref="IBigBrother"/> instance</param>
        public AzScanAllCommand(IServiceProvider serviceProvider, IBigBrother bb):base(bb)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await Task.WhenAll(
                ExecuteInnerCommand<AzScanSqlCommand>(app, console),
                ExecuteInnerCommand<AzScanCosmosDbCommand>(app, console),
                ExecuteInnerCommand<AzScanRedisCommand>(app, console),
                ExecuteInnerCommand<AzScanServiceBusCommand>(app, console),
                ExecuteInnerCommand<AzScanAppInsightsCommand>(app, console),
                ExecuteInnerCommand<AzScanDNSCommand>(app, console),
                ExecuteInnerCommand<AzScanKustoCommand>(app, console),
                ExecuteInnerCommand<AzScanEnvironmentInfoCommand>(app, console));

            return 0;
        }

        private async Task ExecuteInnerCommand<T>(CommandLineApplication app, IConsole console) where T : AzScanCommandBase
        {
            var command = GetCompositeCommand<T>();
            var timedInner = new EswCliToolCommandExecutionTimedEvent
            {
                Arguments = app.Options.ToConsoleString(), CommandType = command.GetType().FullName
            };
            try
            {
                await command.OnExecuteAsync(app, console);
            }
            finally
            {
                BigBrother.Publish(timedInner);
                BigBrother.Flush();
            }
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
