using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools
{
    // <summary>
    /// Wraps the <see cref="CommandLineApplication"/> args functionality into specifics for this console app.
    /// </summary>
    [Obsolete]
    public class CommandLineOptions
    {
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

            app.HelpOption(inherited: true);
            app.Command("transform", cmd =>
            {
                cmd.OnExecute(() =>
                {
                    cmd.ShowHelp();
                    return 1;
                });
            });

            //options.Configure(app);

            //app.Execute(args);
            //options.IsHelp = app.IsShowingInformation;

            //return options;
            return null;
        }

        //private void Configure([NotNull]CommandLineApplication app)
        //{
        //    app.Command("transform", doIt => {

        //        doIt. = new Resx2JsonCommand();
        //    });

        //    var transformOption = app.Option(
        //        "-t|--transform",
        //        "Transform resx files to JSON");

        //    var autoRestOption = app.Option(
        //        "-a|--autorest <URI>",
        //        "The path to the swagger documentation to create an auto rest client");

        //    var help = app.HelpOption("-?|-h|--help");
        //    var verbose = app.VerboseOption();

        //    app.OnExecute(() =>
        //    {
        //        Transform = transformOption.Value();
        //        AutoRest = autoRestOption.Value();
        //        //todo cleanup
        //        ////ResxFolder = resxProject.Value();
        //        //JsonFolder = jsonProject.Value();
        //        IsHelp = help.HasValue();
        //        IsVerbose = verbose.HasValue();
        //        RemainingArguments = app.RemainingArguments;
        //    });
        //}
    }
}