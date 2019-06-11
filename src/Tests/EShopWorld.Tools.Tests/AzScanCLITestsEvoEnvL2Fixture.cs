using System.Threading.Tasks;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AzScanCLITestsEvoEnvL2Fixture : AzScanCLITestsL2FixtureBase
    {
        internal const string TestSubscription = "evo-test";

        public AzScanCLITestsEvoEnvL2Fixture():base("test")
        {
        }

        protected override async Task CreateResources()
        {
            //setup regional resources
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var regionalRg = await AzClient.ResourceGroups
                    .Define(GetRegionalResourceGroupName(region.ToRegionCode()))
                    .WithRegion(Region.EuropeWest).CreateAsync();

                await SetupOutputKV(regionalRg);
            }
        }

        protected override async Task DeleteResources()
        {
            if (AzClient != null)
            {
                foreach (var region in RegionHelper.DeploymentRegionsToList())
                {
                    await AzClient.ResourceGroups.DeleteRGIfExists(
                        GetRegionalResourceGroupName(region.ToRegionCode()));
                }
            }
        }
    }

    [CollectionDefinition(nameof(AzScanCLITestsEvoEnvL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanCLITestsEvoEnvL2Collection : ICollectionFixture<AzScanCLITestsEvoEnvL2Fixture>
    { }
}
