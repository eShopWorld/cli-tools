using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// some useful extensions for AI client, used during testing
    ///
    /// TODO: consider moving to relevant package for reuse (note this is not fluent package)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal static class IComponentsOperationExtensions
    {
        // ReSharper disable once InconsistentNaming
        internal static async Task<ApplicationInsightsComponent> CreateAIInstanceIfNotExists(this IComponentsOperations client, string name,
            string resourceGroupName, string location="westEurope")
        {

            ApplicationInsightsComponent aiInstance=null;
            IPage<ApplicationInsightsComponent> page=null;

            //FYI - https://github.com/Azure/azure-sdk-for-net/issues/5123, we now have to deal with JSON exception
            try
            {
                do
                {
                    page = page==null ? await client.ListByResourceGroupAsync(resourceGroupName) : await client.ListByResourceGroupNextAsync(page.NextPageLink);
                    if ((aiInstance = page.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) !=
                        null)
                    {
                        break;
                    }
                } while (!string.IsNullOrWhiteSpace(page.NextPageLink));
            }
            catch (SerializationException)
            {
                //do nothing
            }

            if (aiInstance != null)
                return aiInstance;

            return await client.CreateOrUpdateAsync(resourceGroupName, name,
                new ApplicationInsightsComponent {Location = location, FlowType = "Bluefield", Kind = "web", ApplicationType = "web"});
        }
    }
}
