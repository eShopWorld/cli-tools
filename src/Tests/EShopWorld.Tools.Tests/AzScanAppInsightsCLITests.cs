using System.Threading.Tasks;
using Eshopworld.Tests.Core;using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(CLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanAppInsightsCLITests :CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanAppInsightsCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer2]
        public async Task ResourcesProjectedPerResourceGroupTests()
        {
            //run the CLI
            // ReSharper disable once StringLiteralTypo
            GetStandardOutput("azscan", "ai", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName);
            //check the KV
            var secrets = await _fixture.LoadAllKeyVaultSecrets();
            //secrets.Should().ContainSingle(s=> s.Id.Equals("AI--"))
        }
    }
}
