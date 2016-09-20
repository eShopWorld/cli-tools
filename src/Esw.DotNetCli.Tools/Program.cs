namespace Esw.DotNetCli.Tools
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using JetBrains.Annotations;
    using Microsoft.DotNet.Cli.Utils;
    using Microsoft.DotNet.ProjectModel;
    using Newtonsoft.Json;

    public class Program
    {
        private const string TranslationsFolder = "translations";

        public static PathHelper PathHelper = new PathHelper();

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

                var sourceFolder = Path.GetDirectoryName(Path.GetFullPath(options.ResxProject));
                var outputFolder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(options.JsonProject)), "wwwroot", TranslationsFolder);

                PathHelper.CreateDirectory(outputFolder);

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                var resxProject = BuildWorkspace.Create().GetProject(sourceFolder);

                var resxFiles = resxProject.Files.ResourceFiles.Select(f => f.Key);

                foreach (var resxFile in resxFiles)
                {
                    var resxDictionary = XElement.Parse(File.ReadAllText(resxFile))
                                                 .Elements("data")
                                                 .ToDictionary(
                                                     x => x.Attribute("name").Value,
                                                     x => x.Element("value").Value);

                    var json = JsonConvert.SerializeObject(resxDictionary);
                    var finalFolder = PathHelper.EnforceSameFolders(sourceFolder, outputFolder, resxFile);
                    var jsonFilePath = Path.Combine(finalFolder, Path.GetFileNameWithoutExtension(resxFile) + ".json");

                    File.WriteAllText(jsonFilePath, json);
                }
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
