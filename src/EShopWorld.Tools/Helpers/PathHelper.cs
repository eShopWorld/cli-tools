using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EShopWorld.Tools.Helpers
{
    /// <summary>
    /// Contains extensions to the <see cref="Path"/> class in System.IO as a helper class to improve mocking.
    /// </summary>
    public class PathHelper : IPathHelper
    {
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        public string CreateRelativePath(string fromPath, string toPath)
        {
            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return null; // path can't be made relative.
            }

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            var firstPart = relativePath.Split('\\').FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstPart)) return string.Empty; // bad path so don't continue

            if (firstPart.Contains(":")) return null; // Different drive letters

            return string.IsNullOrEmpty(relativePath) || firstPart.Equals(".") || firstPart.Equals("..")
                ? relativePath
                : Regex.Replace(relativePath, @"^[^\\]*", ".");
        }

        /// <summary>
        /// Applies the relative path from the <paramref name="file"/> in relation to the <paramref name="sourceFolder"/>
        /// onto the <paramref name="outputFolder"/> and if the output folder structure doesn't exist, it gets created.
        /// </summary>
        /// <param name="sourceFolder">The source baseline folder.</param>
        /// <param name="file">The file we want to create the relation from the baseline folder.</param>
        /// <param name="outputFolder">The output target folder.</param>
        /// <returns>The final folder path, which is the combination of the <paramref name="outputFolder"/> with the relative path, without the file name+extension.</returns>
        public string EnforceSameFolders(string sourceFolder, string outputFolder, string file)
        {
            var relativePath = CreateRelativePath(sourceFolder, Path.GetDirectoryName(file));
            var absoluteFolder = outputFolder;

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                absoluteFolder = Path.Combine(absoluteFolder, relativePath);
                CreateDirectory(absoluteFolder);
            }

            return absoluteFolder;
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path">The directory to create. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}