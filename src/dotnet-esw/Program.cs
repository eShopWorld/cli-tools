﻿namespace Esw.DotNetCli.Tools
{
    using System;
    using System.Diagnostics;
    using JetBrains.Annotations;
    using Microsoft.DotNet.Cli.Utils;

    public class Program
    {
        public static int Main([NotNull] string[] args)
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

                var notACommandYet = new Resx2JsonCommand(options.ResxProject, options.JsonProject);
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

        private static void HandleVerboseContext(CommandLineOptions options)
        {
            bool isVerbose;
            bool.TryParse(Environment.GetEnvironmentVariable(CommandContext.Variables.Verbose), out isVerbose);

            options.IsVerbose = options.IsVerbose || isVerbose;

            if (options.IsVerbose)
            {
                Environment.SetEnvironmentVariable(CommandContext.Variables.Verbose, bool.TrueString);
            }
        }
    }
}