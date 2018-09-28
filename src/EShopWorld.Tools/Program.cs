using EShopWorld.Tools.Common;
using EShopWorld.Tools.Transforms;

namespace EShopWorld.Tools
{
    using System;
    using System.Diagnostics;
    using JetBrains.Annotations;
    using Microsoft.DotNet.Cli.Utils;

    /// <summary>
    /// Dotnet CLI extension entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Dotnet CLI extension entry point.
        /// </summary>
        /// <param name="args">The list of arguments for this extension.</param>
        /// <returns>Executable exit code.</returns>
        public static int Main([NotNull]string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                var options = CommandLineOptions.Parse(args);

                HandleVerboseContext(options);

                if (options.IsHelp)
                {
                    return 2;
                }

                //switch here for different commands ie: autorest/transforms


                //todo ioc PathHelper
                var notACommandYet = new Resx2JsonCommand(options.Resx2JsonCommandLineOptions.ResxFolder, options.Resx2JsonCommandLineOptions.JsonFolder, new PathHelper());
                notACommandYet.Run();
            }
            catch (Exception ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif
                Reporter.Error.WriteLine(ex.Message.Bold().Red());
                return 1;
            }

            return 0;
        }

        private static void HandleVerboseContext([NotNull]CommandLineOptions options)
        {
            bool.TryParse(Environment.GetEnvironmentVariable(CommandContext.Variables.Verbose), out bool isVerbose);

            options.IsVerbose = options.IsVerbose || isVerbose;

            if (options.IsVerbose)
            {
                Environment.SetEnvironmentVariable(CommandContext.Variables.Verbose, bool.TrueString);
            }
        }
    }
}
