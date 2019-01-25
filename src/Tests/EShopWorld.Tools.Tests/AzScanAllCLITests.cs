using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScanCLITestsL2Collection))]
    public class AzScanAllCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanAllCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer2]
        public async Task CheckAllProjectedResourcesPerResourceGroup_ShortNames()
        {
            GetStandardOutput("azscan", "all", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName, "-g", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            var secrets = await _fixture.LoadAllKeyVaultSecretsAsync();

            AzScanAppInsightsCLITests.CheckSecrets(secrets);
            AzScanCosmosCLITests.CheckSecrets(secrets);
            AzScanDNSCLITests.CheckSecrets(secrets);
        }
    }
}
