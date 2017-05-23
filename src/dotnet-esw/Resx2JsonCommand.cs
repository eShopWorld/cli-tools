namespace Esw.DotNetCli.Tools
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;
    using JetBrains.Annotations;
    using Newtonsoft.Json;

    /// <summary>
    /// A command (not yet, but soon) to transform and merge RESX files into their angular JSON equivalents.
    /// </summary>
    public class Resx2JsonCommand // THIS IS NOT A COMMAND YET
    {
        private const string TranslationsFolder = "translations";
        private readonly string _resxProject;
        private readonly string _outputProject;

        internal const string JsonDefaultCulture = "en";
        internal Dictionary<string, List<string>> ResourceDictionary;

        public static PathHelper PathHelper = new PathHelper(); // to be INJECTED in the near future! leave it as a prop


        /// <summary>
        /// Initializes a new instance of <see cref="Resx2JsonCommand"/>.
        /// </summary>
        /// <remarks>
        /// Here for testability purposes only.
        /// </remarks>
        internal Resx2JsonCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resx2JsonCommand"/>.
        /// </summary>
        /// <param name="resxProject">The path to the VS project that contains the RESX files. Can be absolute or relative.</param>
        /// <param name="outputProject">The path to the VS project that will contain the JSON files. Can be absolute or relative.</param>
        public Resx2JsonCommand([NotNull]string resxProject, [NotNull]string outputProject)
        {
            _resxProject = resxProject;
            _outputProject = outputProject;
        }

        /// <summary>
        /// Runs this command.
        /// </summary>
        public void Run()
        {
            ResourceDictionary = new Dictionary<string, List<string>>();
            var sourceFolder = Path.GetDirectoryName(Path.GetFullPath(_resxProject));
            var outputFolder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_outputProject)), "wwwroot", TranslationsFolder);

            PathHelper.CreateDirectory(outputFolder);

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var resxFiles = Directory.GetFiles(sourceFolder, "*.resx", SearchOption.AllDirectories)
                                     .Select(Path.GetFullPath);

            foreach (var resxFile in resxFiles.OrderBy(f => f?.Split('\\')?.Length))
            {
                var fileContent = GetMergedResource(resxFile);

                var json = ConvertResx2Json(fileContent);

                // always insert culture on JSON file names using the default culture constant
                var jsonFilePath = GetJsonPath(PathHelper.EnforceSameFolders(sourceFolder, outputFolder, resxFile), resxFile);
                File.WriteAllText(jsonFilePath, json);
            }
        }

        /// <summary>
        /// Makes sure that localization JSON file names always have culture against them.
        /// So the default LocalResource.resx file in the C# world would get transformed to LocalResource.en.json if
        /// the default culture is "en".
        /// </summary>
        /// <param name="outputFolder">The target output folder of the JSON file.</param>
        /// <param name="resxFile">The full path of the RESX file.</param>
        /// <returns>The full path, including the file name, for the JSON file.</returns>
        [NotNull]internal virtual string GetJsonPath([NotNull]string outputFolder, [NotNull]string resxFile)
        {
            return Path.GetFileNameWithoutExtension(resxFile).Split('.').Length == 1
                ? Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(resxFile) + "." + JsonDefaultCulture + ".json")
                : Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(resxFile) + ".json");
        }

        /// <summary>
        /// Converts a RESX XML <see cref="string"/> into a JSON string.
        /// </summary>
        /// <param name="resx">The RESX XML <see cref="string"/>.</param>
        /// <returns>The transformed JSON <see cref="string"/>.</returns>
        [NotNull]internal virtual string ConvertResx2Json([NotNull]string resx)
        {
            var resxDictionary = XElement.Parse(resx)
                                         .Elements("data")
                                         .ToDictionary(
                                             x => x.Attribute("name")?.Value,
                                             x => x.Element("value")?.Value);

            return JsonConvert.SerializeObject(resxDictionary);
        }

        /// <summary>
        /// Applies the folder hierarchy merge logic to a specific file on a given path.
        /// </summary>
        /// <param name="resxFile">The path for the resx file.</param>
        /// <returns>The RESX XML file content after being merged (if merging applies).</returns>
        [NotNull]internal virtual string GetMergedResource([NotNull]string resxFile)
        {
            var fileContent = ReadText(resxFile);
            var fileName = Path.GetFileName(resxFile);

            if (ResourceDictionary.ContainsKey(fileName))
            {
                var baseResource = ResourceDictionary[fileName].Where(f => resxFile.Contains(Path.GetDirectoryName(f)))
                                                                .OrderByDescending(f => f?.Split('\\')?.Length)
                                                                .FirstOrDefault();

                if (baseResource != null)
                {
                    fileContent = MergeResx(ReadText(baseResource), fileContent);
                    WriteText(resxFile, fileContent);
                }

                ResourceDictionary[fileName].Add(resxFile);
            }
            else
            {
                ResourceDictionary.Add(Path.GetFileName(resxFile), new List<string> { resxFile });
            }

            return fileContent;
        }

        /// <summary>
        /// Merges two blocks of XML RESX as <see cref="string"/>.
        /// </summary>
        /// <param name="source">The RESX XML to act as source.</param>
        /// <param name="target">The RESX XML to act as target.</param>
        /// <returns>The merged RESX XML.</returns>
        [NotNull]internal virtual string MergeResx([NotNull]string source, [NotNull]string target)
        {
            var sourceXml = XElement.Parse(source);
            var targetXml = XElement.Parse(target);

            foreach (var element in sourceXml.Elements("data").Except(targetXml.Elements("data"), new ResxDataComparer()))
            {
                targetXml.Add(element);
            }

            return targetXml.ToString();
        }

        /// <summary>
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading. </param>
        /// <returns>A string containing all lines of the file.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]internal virtual string ReadText([NotNull]string path)
        {
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to. </param>
        /// <param name="contents">The string to write to the file. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void WriteText([NotNull]string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        /// <summary>
        /// Defines methods to support the comparison of RESX data <see cref="XElement"/> objects for equality.
        /// </summary>
        internal class ResxDataComparer : IEqualityComparer<XElement>
        {
            /// <summary>
            /// Determines whether the specified objects are equal.
            /// </summary>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            /// <param name="x">The first object of type <see cref="XElement"/> to compare.</param>
            /// <param name="y">The second object of type <see cref="XElement"/> to compare.</param>
            public bool Equals(XElement x, XElement y)
            {
                // nullability checks
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                // equality check (with null guards)
                var nameAttComp = x.Attribute("name")?.Value.Equals(y.Attribute("name")?.Value);
                return nameAttComp.HasValue && nameAttComp.Value;
            }

            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <returns>A hash code for the specified object.</returns>
            /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
            public int GetHashCode(XElement obj)
            {
                var hash = obj?.Attribute("name")?.Value?.GetHashCode();
                return hash ?? 0;
            }
        }
    }
}
