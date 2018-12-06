using JetBrains.Annotations;

namespace EShopWorld.Tools
{
    public interface IPathHelper
    {
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        string CreateRelativePath([NotNull]string fromPath, [NotNull]string toPath);

        /// <summary>
        /// Applies the relative path from the <paramref name="file"/> in relation to the <paramref name="sourceFolder"/>
        /// onto the <paramref name="outputFolder"/> and if the output folder structure doesn't exist, it gets created.
        /// </summary>
        /// <param name="sourceFolder">The source baseline folder.</param>
        /// <param name="file">The file we want to create the relation from the baseline folder.</param>
        /// <param name="outputFolder">The output target folder.</param>
        /// <returns>The final folder path, which is the combination of the <paramref name="outputFolder"/> with the relative path, without the file name+extension.</returns>
        string EnforceSameFolders([NotNull]string sourceFolder, [NotNull]string outputFolder, [NotNull]string file);

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path">The directory to create. </param>
        void CreateDirectory([NotNull]string path);
    }
}