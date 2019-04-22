using System;
using System.Collections.Generic;
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
                Arguments = string.Join(',', args.Select(t => $"{t.LongName}-'{t.Value()}'")),
                Warning = warning
            };

            bb?.Publish(@event);
            bb?.Flush();

            EmitMessage(console, ConsoleColor.Yellow, $"Command {@event.CommandType}, Arguments {@event.Arguments} produced warning - {warning}");
        }

        private static void EmitMessage(this IConsole console, ConsoleColor color, string text)
        {
            console.ForegroundColor = color;
            console.WriteLine(text);
            console.ResetColor();
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
            @event.Arguments = string.Join(',', args.Select(t => $"{t.LongName}-'{t.Value()}'")); 

            bb?.Publish(@event);
            bb?.Flush();

            EmitMessage(console, ConsoleColor.Red, $"Command {@event.CommandType}, Arguments {@event.Arguments} produced an error - {e.Message}");
        }
    }
}
