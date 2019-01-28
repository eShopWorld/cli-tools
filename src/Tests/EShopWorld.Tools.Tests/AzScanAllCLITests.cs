using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using FluentAssertions;
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
        public void CheckOptions()
        {
            var content = GetStandardOutput("azscan", "all", "-h");
            content.Should().ContainAll("-s", "--subscription", "-r", "--region", "-g", "--resourceGroup", "-k",
                "--keyVault");
        }

        [Fact, IsLayer2]
        public async Task CheckAllProjectedResourcesPerResourceGroup_ShortNames()
        {
            await _fixture.DeleteAllSecrets();
            GetStandardOutput("azscan", "all", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName, "-g", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            var secrets = await _fixture.LoadAllKeyVaultSecretsAsync();

            AzScanAppInsightsCLITests.CheckSecrets(secrets);
            AzScanCosmosCLITests.CheckSecrets(secrets);
            AzScanDNSCLITests.CheckSecrets(secrets);
            AzScanSBCLITests.CheckSecrets(secrets);
        }

        [Fact, IsLayer2]
        public async Task CheckAllProjectedResourcesPerResourceGroup_LongNames()
        {
            await _fixture.DeleteAllSecrets();
            GetStandardOutput("azscan", "all", "--keyVault", AzScanCLITestsL2Fixture.OutputKeyVaultName, "--subscription",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "--region", AzScanCLITestsL2Fixture.TargetRegionName, "--resourceGroup", AzScanCLITestsL2Fixture.DomainAResourceGroupName);            

            var secrets = await _fixture.LoadAllKeyVaultSecretsAsync();

            AzScanAppInsightsCLITests.CheckSecrets(secrets);
            AzScanCosmosCLITests.CheckSecrets(secrets);
            AzScanDNSCLITests.CheckSecrets(secrets);
            AzScanSBCLITests.CheckSecrets(secrets);
        }
    }
}
