// ReSharper disable once CheckNamespace
namespace System.IO
{
    using Linq;
    using Text.RegularExpressions;

    /// <summary>
    /// Constains extensions to the <see cref="Path"/> class in System.IO.
    /// </summary>
    public static class PathExtensions
    {
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string CreateRelativePath(this string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException(nameof(fromPath));
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException(nameof(toPath));

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        /// <summary>
        /// Applies the relative path from the <paramref name="file"/> in relation to the <paramref name="sourceFolder"/>
        /// onto the <paramref name="outputFolder"/> and if the output folder structure doesn't exist, it gets created.
        /// </summary>
        /// <param name="sourceFolder">The source baseline folder.</param>
        /// <param name="file">The file we want to create the relation from the baseline folder.</param>
        /// <param name="outputFolder">The output target folder.</param>
        /// <returns>The final folder path, which is the combination of the <paramref name="outputFolder"/> with the relative path, without the file name+extension.</returns>
        public static string EnforceSameFolders(this string sourceFolder, string outputFolder, string file)
        {
            var relativePath = sourceFolder.CreateRelativePath(Path.GetDirectoryName(file));
            var absoluteFolder = outputFolder;

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                relativePath = Regex.Replace(relativePath, @"^[^\\]*", ".");

                foreach (var folder in relativePath.Split('\\').Skip(1))
                {
                    absoluteFolder = Path.Combine(absoluteFolder, folder);
                    if (!Directory.Exists(absoluteFolder))
                    {
                        Directory.CreateDirectory(absoluteFolder);
                    }
                }
            }

            return absoluteFolder;
        }
    }
}