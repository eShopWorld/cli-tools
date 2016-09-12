﻿// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.CommandLineUtils
{
    using System;

    public static class CommandLineApplicationExtensions
    {
        public static void OnExecute(this CommandLineApplication command, Action invoke)
            => command.OnExecute(
                () =>
                {
                    invoke();
                    return 0;
                });

        public static CommandOption Option(this CommandLineApplication command, string template, string description)
            => command.Option(
                template,
                description,
                template.IndexOf('<') != -1
                    ? CommandOptionType.SingleValue
                    : CommandOptionType.NoValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static CommandOption HelpOption(this CommandLineApplication command) => command.HelpOption("-h|--help");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static CommandOption VerboseOption(this CommandLineApplication command) => command.Option("-v|--verbose", "Enable verbose output");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="shortFormVersionGetter"></param>
        /// <returns></returns>
        public static CommandOption VersionOption(this CommandLineApplication command, Func<string> shortFormVersionGetter) => command.VersionOption("--version", shortFormVersionGetter);
    }
}
