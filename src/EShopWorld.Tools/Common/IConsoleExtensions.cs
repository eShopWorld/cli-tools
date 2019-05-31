using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eshopworld.Core;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Common
{
    /// <summary>
    /// console extensions to support warnings and errors and associated telemetry
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IConsoleExtensions
    {
        /// <summary>
        /// emit warning to console and BigBrother
        /// </summary>
        /// <param name="console">extension instance</param>
        /// <param name="bb"><see cref="IBigBrother"/></param>
        /// <param name="command">command type</param>
        /// <param name="args">application options</param>
        /// <param name="warning">warning message</param>
        public static void EmitWarning(this IConsole console, IBigBrother bb, Type command, List<CommandOption> args, string warning)
        {
            var @event = new AzCLIWarningEvent()
            {
                CommandType = command.ToString(),
                Arguments = args!=null && args.Any() ? string.Join(',', args.Select(t => $"{t.LongName}-'{t.Value()}'")) : String.Empty,
                Warning = warning
            };

            bb?.Publish(@event);
            bb?.Flush();

            var argsMessage = string.IsNullOrWhiteSpace(@event.Arguments) ? "" : $",Arguments '{@event.Arguments}'";
            console.EmitMessage(console.Out, $"WARNING - Command {@event.CommandType}{argsMessage} - {warning}");
        }

        // ReSharper disable once UnusedParameter.Local
        private static void EmitMessage(this IConsole console, TextWriter tw, string text)
        {
            tw.WriteLine(text);
        }

        /// <summary>
        /// emit error to console and BigBrother
        /// </summary>
        /// <param name="console">console instance</param>
        /// <param name="bb"><see cref="IBigBrother"/></param>
        /// <param name="e">exception</param>
        /// <param name="command">command type</param>
        /// <param name="args">command arguments</param>
        public static void EmitException(this IConsole console, IBigBrother bb, Exception e, Type command, List<CommandOption> args)
        {
            var @event = e.ToExceptionEvent<AzCLIExceptionEvent>();
            @event.CommandType = command.ToString();
            @event.Arguments = args!=null ? string.Join(',', args.Select(t => $"{t.LongName}-'{t.Value()}'")) : string.Empty; 

            bb?.Publish(@event);
            bb?.Flush();

            var argsMessage = string.IsNullOrWhiteSpace(@event.Arguments) ? "" : $",Arguments '{@event.Arguments}'";
            console.EmitMessage(console.Error, $"ERROR - Command {@event.CommandType}{argsMessage} -{e.GetType().FullName} -  {e.Message}");
            console.WriteLine(e.StackTrace);
        }
    }
}
