﻿namespace Esw.DotNetCli.Resx2Json
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using JetBrains.Annotations;
    using Microsoft.DotNet.Cli.Utils;
    using Microsoft.DotNet.ProjectModel;
    using Newtonsoft.Json;

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

                var sourceFolder = Path.GetDirectoryName(options.ResxProject);
                var outputFolder = Path.GetDirectoryName(options.JsonProject);

                var resxProject = BuildWorkspace.Create().GetProject(sourceFolder);
                var jsonProject = BuildWorkspace.Create().GetProject(outputFolder);

                var resxFiles = resxProject.Files.ResourceFiles.Select(f => f.Key);

                foreach (var resxFile in resxFiles)
                {
                    var resxDictionary = XElement.Parse(File.ReadAllText(resxFile))
                                                 .Elements("data")
                                                 .ToDictionary(
                                                     x => x.Attribute("name").Value,
                                                     x => x.Element("value").Value);

                    var json = JsonConvert.SerializeObject(resxDictionary);
                }
            }
            catch (Exception ex)
            {
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
