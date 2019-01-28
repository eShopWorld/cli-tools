using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// some useful extension methods for various <see cref="IAzure"/> and its sub-clients
    /// consider moving this to shared space (e.g. Sierra contains similar/same code)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal static class AzureFluentExtensions
    {
        // ReSharper disable once InconsistentNaming
        internal static async Task<IResourceGroup> CreateRGIfNotExistsAsync(this IResourceGroups client, string name, string region = "west europe")
        {
            if (!(await client.ContainAsync(name)))
            {
                return await client.Define(name).WithRegion(region).CreateAsync();
            }

            return await client.GetByNameAsync(name);
        }

        // ReSharper disable once InconsistentNaming
        internal static async Task DeleteRGIfExists(this IResourceGroups client, string name)
        {
            if (await client.ContainAsync(name))
            {
                await client.DeleteByNameAsync(name);
            }
        }
    }
}
