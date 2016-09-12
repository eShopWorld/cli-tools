namespace Esw.DotNetCli.Resx2Json
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using JetBrains.Annotations;

    /// <summary>
    /// Constains extensions to the <see cref="Path"/> class in System.IO.
    /// </summary>
    public class PathHelper
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
        public string CreateRelativePath([NotNull]string fromPath, [NotNull]string toPath)
        {
            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath; // path can't be made relative.
            }

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
        public string EnforceSameFolders(string sourceFolder, string outputFolder, string file)
        {
            var relativePath = CreateRelativePath(sourceFolder, Path.GetDirectoryName(file));
            var absoluteFolder = outputFolder;

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                relativePath = Regex.Replace(relativePath, @"^[^\\]*", ".");

                foreach (var folder in relativePath.Split('\\').Skip(1))
                {
                    absoluteFolder = Path.Combine(absoluteFolder, folder);
                    CreateIfDoesntExist(absoluteFolder);
                }
            }

            return absoluteFolder;
        }

        /// <summary>
        /// Creates the specified folder if it doesn't exist, otherwise does nothing.
        /// </summary>
        /// <param name="folder">The path to the folder that we want to create.</param>
        /// <exception cref="ArgumentException">If the specified path isn't a directory.</exception>
        public void CreateIfDoesntExist([NotNull]string folder)
        {
            try
            {
                if (!File.GetAttributes(folder).HasFlag(FileAttributes.Directory))
                {
                    throw new ArgumentException($"The provided path: {folder} isn't a directory.", nameof(folder));
                }
            }
            catch (FileNotFoundException)
            {
                CreateDirectory(folder);
            }
        }

        /// <summary>Creates all directories and subdirectories in the specified path unless they already exist.</summary>
        /// <returns>An object that represents the directory at the specified path. This object is returned regardless of whether a directory at the specified path already exists.</returns>
        /// <param name="path">The directory to create. </param>
        /// <exception cref="T:System.IO.IOException">The directory specified by <paramref name="path" /> is a file.-or-The network name is not known.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="path" /> is a zero-length string, contains only white space, or contains one or more invalid characters. You can query for invalid characters by using the <see cref="M:System.IO.Path.GetInvalidPathChars" /> method.-or-<paramref name="path" /> is prefixed with, or contains, only a colon character (:).</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="path" /> is null. </exception>
        /// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters. </exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// <paramref name="path" /> contains a colon character (:) that is not part of a drive label ("C:\").</exception>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        /// <remarks>
        /// Inverts control over <see cref="Directory"/> for the <see cref="Directory.CreateDirectory(string)"/> method so that
        /// we can actually test the other blocks in this class.
        /// </remarks>>
        internal virtual void CreateDirectory([NotNull]string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}