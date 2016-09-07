namespace Esw.DotNetCli.Resx2Json
{
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Extensions.CommandLineUtils;

    public class CommandLineOptions
    {
        public string ResxProject { get; set; }
        public string JsonProject { get; set; }
        public IList<string> RemainingArguments { get; set; }
        public bool IsVerbose { get; set; }
        public bool IsHelp { get; set; }

        public static CommandLineOptions Parse(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet resx2json",
                FullName = "eShopWorld .NET Core CLI Commands"
            };

            var options = new CommandLineOptions();

            options.Configure(app);

            app.Execute(args);
            options.IsHelp = app.IsShowingInformation;

            return options;
        }

        private void Configure(CommandLineApplication app)
        {
            var resxProject = app.Option(
                "-s|--resx-project <project>",
                "The project to target (defaults to the project in the current directory). Can be a path to a project.json or a project directory.");

            var jsonProject = app.Option(
                "-o|--json-project <project>",
                "The path to the project containing Startup (defaults to the target project). Can be a path to a project.json or a project directory.");

            var verbose = app.Option("--verbose", "Show verbose output");

            app.VersionOption("--version", () => AssemblyVersion);
            app.HelpOption("-?|--help");

            app.OnExecute(() =>
            {
                ResxProject = resxProject.Value();
                JsonProject = jsonProject.Value();
                IsVerbose = verbose.HasValue();
                RemainingArguments = app.RemainingArguments;
            });
        }

        private static readonly Assembly ThisAssembly = typeof(CommandLineOptions).GetTypeInfo().Assembly;

        private static readonly string AssemblyVersion = ThisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                                         ?? ThisAssembly.GetName().Version.ToString();
    }
}