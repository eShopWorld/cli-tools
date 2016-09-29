namespace Esw.DotNetCli.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using JetBrains.Annotations;
    using Microsoft.DotNet.ProjectModel;
    using Newtonsoft.Json;

    public class Resx2JsonCommand // THIS IS NOT A COMMAND YET
    {
        private const string TranslationsFolder = "translations";
        private readonly string _resxProject;
        private readonly string _outputProject;

        private Dictionary<string, List<string>> _resourceDictionary;

        public static PathHelper PathHelper = new PathHelper(); // to be INJECTED in the near future! leave it as a prop

        public Resx2JsonCommand([NotNull]string resxProject, [NotNull]string outputProject)
        {
            _resxProject = resxProject;
            _outputProject = outputProject;
        }

        public void Run()
        {
            _resourceDictionary = new Dictionary<string, List<string>>();
            var sourceFolder = Path.GetDirectoryName(Path.GetFullPath(_resxProject));
            var outputFolder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_outputProject)), "wwwroot", TranslationsFolder);

            PathHelper.CreateDirectory(outputFolder);

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var resxProject = BuildWorkspace.Create().GetProject(sourceFolder);
            if(resxProject == null)
                throw new InvalidOperationException($"Couldn't find the source project '{_resxProject}'");

            var resxFiles = resxProject.Files.ResourceFiles.Select(f => f.Key);

            foreach (var resxFile in resxFiles.OrderBy(f => f?.Split('\\')?.Length))
            {
                var fileContent = GetMergedResource(resxFile);

                var json = ConvertResx2Json(fileContent);
                var finalFolder = PathHelper.EnforceSameFolders(sourceFolder, outputFolder, resxFile);
                var jsonFilePath = Path.Combine(finalFolder, Path.GetFileNameWithoutExtension(resxFile) + ".json");

                File.WriteAllText(jsonFilePath, json);
            }
        }

        internal virtual string ConvertResx2Json(string resx)
        {
            var resxDictionary = XElement.Parse(resx)
                                         .Elements("data")
                                         .ToDictionary(
                                             x => x.Attribute("name").Value,
                                             x => x.Element("value").Value);

            return JsonConvert.SerializeObject(resxDictionary);
        }

        internal virtual string GetMergedResource(string resxFile)
        {
            var fileContent = File.ReadAllText(resxFile);
            var fileName = Path.GetFileName(resxFile);

            if (_resourceDictionary.ContainsKey(fileName))
            {
                var baseResource = _resourceDictionary[fileName].Where(f => resxFile.Contains(Path.GetDirectoryName(f)))
                                                                .OrderByDescending(f => f?.Split('\\')?.Length)
                                                                .FirstOrDefault();

                if (baseResource != null)
                {
                    fileContent = MergeResx(File.ReadAllText(baseResource), fileContent);
                }

                _resourceDictionary[fileName].Add(resxFile);
            }
            else
            {
                _resourceDictionary.Add(Path.GetFileName(resxFile), new List<string> { resxFile });
            }

            return fileContent;
        }

        internal virtual string MergeResx(string source, string target)
        {
            return default(string);
        }
    }
}
