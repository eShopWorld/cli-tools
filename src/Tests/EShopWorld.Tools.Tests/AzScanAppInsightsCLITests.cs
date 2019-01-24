using System;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScansCLITestsL1Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanAppInsightsCLITests :CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL1Fixture _fixture;

        public AzScanAppInsightsCLITests(AzScanCLITestsL1Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer1]
        public void CheckOptions()
        {
            var content = GetStandardOutput("azscan", "ai", "-h");
            content.Should().ContainAll("-s", "--subscription", "-r", "--region", "-g", "--resourceGroup", "-k",
                "--keyVault");
        }

        [Fact, IsLayer1]
        public async Task AIResourcesProjectedPerResourceGroup_ShortNames()
        {
            //run the CLI
            // ReSharper disable once StringLiteralTypo
            GetStandardOutput("azscan", "ai", "-k", AzScanCLITestsL1Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL1Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL1Fixture.TargetRegionName, "-g", AzScanCLITestsL1Fixture.DomainAResourceGroupName);
            //check the KV
            var secrets =  await _fixture.LoadAllKeyVaultSecretsAsync();
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("AI--"));
            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("AI--clitestdomainaresourcegroupAi--InstrumentationKey",
                    StringComparison.Ordinal) && Guid.Parse(s.Value)!=default(Guid)); //check key existence and that it is guid (parse succeeds)
        }

        [Fact, IsLayer1]
        public async Task AIResourcesProjectedPerResourceGroup_LongNames()
        {
            //run the CLI
            // ReSharper disable once StringLiteralTypo
            GetStandardOutput("azscan", "ai", "--keyVault", AzScanCLITestsL1Fixture.OutputKeyVaultName, "--subscription",
                AzScanCLITestsL1Fixture.SierraIntegrationSubscription, "--region", AzScanCLITestsL1Fixture.TargetRegionName, "--resourceGroup", AzScanCLITestsL1Fixture.DomainAResourceGroupName);
            //check the KV
            var secrets = await _fixture.LoadAllKeyVaultSecretsAsync();
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("AI--"));
            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("AI--clitestdomainaresourcegroupAi--InstrumentationKey",
                    StringComparison.Ordinal) && Guid.Parse(s.Value) != default(Guid)); //check key existence and that it is guid (parse succeeds)
        }
    }
}
