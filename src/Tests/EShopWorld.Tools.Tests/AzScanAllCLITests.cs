﻿using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Helpers;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanAllCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanAllCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }      

        [InlineData("-s", "-d")]
        [InlineData("--subscription", "--domain")]
        [Theory, IsLayer2]
        public async Task CheckAllProjectedResourcesPerResourceGroup(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();
            GetStandardOutput("azscan", "all", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2Fixture.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecretsAsync(region.ToRegionCode());

                AzScanAppInsightsCLITests.CheckSecrets(secrets);
                AzScanCosmosCLITests.CheckSecrets(secrets);
                AzScanDNSCLITests.CheckSecrets(secrets, region);
                AzScanSBCLITests.CheckSecrets(secrets);
            }
        }
    }
}
