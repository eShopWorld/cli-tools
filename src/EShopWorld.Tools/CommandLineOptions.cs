using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools
{
    /// <summary>
    /// 
    /// </summary>
    public class Resx2JsonCommandLineOptions
    {
        /// <summary>
        /// Gets and sets the folder location that contains the resource files we want to transform.
        /// </summary>
        public string ResxFolder { get; set; }

        /// <summary>
        /// Gets and sets the project location where the output json files are to be included.
        /// </summary>
        public string JsonFolder { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AutoRestCommandLineOptions
    {
        /// <summary>
        /// Gets and sets the folder location that contains the resource files we want to transform.
        /// </summary>
        public string ResxFolder { get; set; }

        /// <summary>
        /// Gets and sets the project location where the output json files are to be included.
        /// </summary>
        public string JsonFolder { get; set; }
    }

    /// <summary>
    /// Wraps the <see cref="CommandLineApplication"/> args functionality into specifics for this console app.
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// Gets and sets if the help argument was used or not when invoking this console app.
        /// </summary>
        public Resx2JsonCommandLineOptions Resx2JsonCommandLineOptions { get; set; }

        /// <summary>
        /// Gets and sets if the help argument was used or not when invoking this console app.
        /// </summary>
        public AutoRestCommandLineOptions AutoRestCommandLineOptions { get; set; }

        /// <summary>
        /// Gets and sets if the help argument was used or not when invoking this console app.
        /// </summary>
        public string Transform { get; set; }

        /// <summary>
        /// Gets and sets if the help argument was used or not when invoking this console app.
        /// </summary>
        public string AutoRest { get; set; }

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
                FullName = "eShopWorld .NET Core CLI Tools"
            };

            var options = new CommandLineOptions();

            options.Configure(app);

            app.Execute(args);
            options.IsHelp = app.IsShowingInformation;

            return options;
        }

        private void Configure([NotNull]CommandLineApplication app)
        {
            //todo move to transform help
            var resxProject = app.Option(
                "-s|--resx-project <project>",
                "The source folder containing the RESX files. Can be absolute or relative.");

            var jsonProject = app.Option(
                "-o|--json-project <project>",
                "The target folder containing the JSON files. Can be absolute or relative.");

            var transformOption = app.Option(
                "-t|--transform",
                "Transform resx files to JSON");

            var autoRestOption = app.Option(
                "-a|--autorest <URI>",
                "The path to the swagger documentation to create an auto rest client");

            var help = app.HelpOption("-?|-h|--help");
            var verbose = app.VerboseOption();
            app.VersionOption(() => AssemblyVersion);

            app.OnExecute(() =>
            {
                Transform = transformOption.Value();
                AutoRest = autoRestOption.Value();
                //todo cleanup
                ////ResxFolder = resxProject.Value();
                //JsonFolder = jsonProject.Value();
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