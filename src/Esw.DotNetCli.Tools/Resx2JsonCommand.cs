namespace Esw.DotNetCli.Tools
{
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

        public static PathHelper PathHelper = new PathHelper(); // to be INJECTED in the near future! leave it as a prop

        public Resx2JsonCommand([NotNull]string resxProject, [NotNull]string outputProject)
        {
            _resxProject = resxProject;
            _outputProject = outputProject;
        }

        public void Run()
        {
            var sourceFolder = Path.GetDirectoryName(Path.GetFullPath(_resxProject));
            var outputFolder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_outputProject)), "wwwroot", TranslationsFolder);

            PathHelper.CreateDirectory(outputFolder);

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var resxProject = BuildWorkspace.Create().GetProject(sourceFolder);

            var resxFiles = resxProject.Files.ResourceFiles.Select(f => f.Key);

            foreach (var resxFile in resxFiles)
            {
                var json = ConvertResx2Json(File.ReadAllText(resxFile));
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
    }
}
