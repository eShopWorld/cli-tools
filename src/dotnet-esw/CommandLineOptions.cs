namespace Esw.DotNetCli.Tools
{
    using System.Collections.Generic;
    using System.Reflection;
    using JetBrains.Annotations;
    using Microsoft.Extensions.CommandLineUtils;

    /// <summary>
    /// Wraps the <see cref="CommandLineApplication"/> args functionallity into specifics for this console app.
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// Gets and sets the folder location that contains the resource files we want to transform.
        /// </summary>
        public string ResxFolder { get; set; }

        /// <summary>
        /// Gets and sets the project location where the output json files are to be included.
        /// </summary>
        public string JsonFolder { get; set; }

        /// <summary>
        /// Gets and sets if the help argument was used or not when invoking this console app.
        /// </summary>
        public bool IsHelp { get; set; }

        /// <summary>
        /// Gets and sets if the verbose argument was used or not when invoking this console app.
        /// </summary>
        public bool IsVerbose { get; set; }

        /// <summary>
        /// Gets and sets the remaining arguments used with this app that weren't setup on the <see cref="CommandLineApplication"/>.
        /// </summary>
        public IList<string> RemainingArguments { get; set; }

        /// <summary>
        /// Parses the input arguments from a console app main entry point into <see cref="CommandLineApplication"/> <see cref="CommandOption"/>.
        /// </summary>
        /// <param name="args">The entry point argument list.</param>
        /// <returns>The parse argument list as an instance of <see cref="CommandLineOptions"/>.</returns>
        public static CommandLineOptions Parse(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet esw",
                FullName = "eShopWorld .NET Core CLI Commands"
            };

            var options = new CommandLineOptions();

            options.Configure(app);

            app.Execute(args);
            options.IsHelp = app.IsShowingInformation;

            return options;
        }

        private void Configure([NotNull]CommandLineApplication app)
        {
            var resxProject = app.Option(
                "-s|--resx-project <project>",
                "The source folder containing the RESX files. Can be absolute or relative.");

            var jsonProject = app.Option(
                "-o|--json-project <project>",
                "The target folder containing the JSON files. Can be absolute or relative.");

            var help = app.HelpOption();
            var verbose = app.VerboseOption();
            app.VersionOption(() => AssemblyVersion);

            app.OnExecute(() =>
            {
                ResxFolder = resxProject.Value();
                JsonFolder = jsonProject.Value();
                IsHelp = help.HasValue();
                IsVerbose = verbose.HasValue();
                RemainingArguments = app.RemainingArguments;
            });
        }

        private static readonly Assembly ThisAssembly = typeof(CommandLineOptions).GetTypeInfo().Assembly;

        private static readonly string AssemblyVersion = ThisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                                         ?? ThisAssembly.GetName().Version.ToString();
    }
}