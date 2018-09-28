using System;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools
{
    /// <summary>
    /// Custom extensions to <see cref="CommandLineApplication"/> that setup argument options for this extension.
    /// </summary>
    public static class CommandLineApplicationExtensions
    {
        /// <summary>
        /// Generic invoke <see cref="Action"/> callback that simply invokes and returns 0.
        /// </summary>
        /// <param name="command">The <see cref="CommandLineApplication"/> that we are setting up.</param>
        /// <param name="invoke">The setup <see cref="Action"/> delegate we are invoking.</param>
        public static void OnExecute(this CommandLineApplication command, Action invoke)
            => command.OnExecute(
                () =>
                {
                    invoke();
                    return 0;
                });

        /// <summary>
        /// Generic option wrapper with template and help description.
        /// </summary>
        /// <param name="command">The <see cref="CommandLineApplication"/> that we are setting up.</param>
        /// <param name="template">The command line argument template.</param>
        /// <param name="description">The description of the argument for help purposes.</param>
        /// <returns></returns>
        public static CommandOption Option(this CommandLineApplication command, string template, string description)
            => command.Option(
                template,
                description,
                template.IndexOf('<') != -1
                    ? CommandOptionType.SingleValue
                    : CommandOptionType.NoValue);

        /// <summary>
        /// Sets up the --help option.
        /// </summary>
        /// <param name="command">The <see cref="CommandLineApplication"/> that we are setting up.</param>
        /// <returns>The <see cref="CommandOption"/> for the argument.</returns>
        public static CommandOption HelpOption(this CommandLineApplication command) => command.HelpOption("-h|--help");

        /// <summary>
        /// Sets up the --verbose option.
        /// </summary>
        /// <param name="command">The <see cref="CommandLineApplication"/> that we are setting up.</param>
        /// <returns>The <see cref="CommandOption"/> for the argument.</returns>
        public static CommandOption VerboseOption(this CommandLineApplication command) => command.Option("-v|--verbose", "Enable verbose output");

        /// <summary>
        /// Sets up the --version option.
        /// </summary>
        /// <param name="command">The <see cref="CommandLineApplication"/> that we are setting up.</param>
        /// <param name="shortFormVersionGetter"></param>
        /// <returns>The <see cref="CommandOption"/> for the argument.</returns>
        public static CommandOption VersionOption(this CommandLineApplication command, Func<string> shortFormVersionGetter) => command.VersionOption("--version", shortFormVersionGetter);
    }
}