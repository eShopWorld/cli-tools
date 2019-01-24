using System;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScansCLITestsL1Collection))]
    public class AzScanDNSCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL1Fixture _fixture;

        public AzScanDNSCLITests(AzScanCLITestsL1Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer1]
        public void CheckOptions()
        {
            // ReSharper disable once StringLiteralTypo
            var content = GetStandardOutput("azscan", "dns", "-h");

            content.Should().ContainAll("-s", "--subscription", "-r", "--region", "-g", "--resourceGroup", "-k",
                "--keyVault");
        }

        [Fact, IsLayer1]
        public async Task TestDNSProjected_ShortNames()
        {
            GetStandardOutput("azscan", "dns", "-k", AzScanCLITestsL1Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL1Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL1Fixture.TargetRegionName);

            var secrets = await _fixture.LoadAllKeyVaultSecretsAsync();
            secrets.Where(s => s.SecretIdentifier.Name.StartsWith("Platform", StringComparison.Ordinal)).Should()
                .HaveCount(4);
            //CNAME check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--clitestdomainaresourcegroupApi--Global",
                    StringComparison.Ordinal) &&
                s.Value.Equals("https://clitestdomainaresourcegroup-api.clitestdomainaresourcegroup.dns",
                    StringComparison.OrdinalIgnoreCase));

            //API 1 - AG check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi1We--HTTPS",
                    StringComparison.Ordinal) &&
                s.Value.Equals("https://3.3.3.3",
                    StringComparison.OrdinalIgnoreCase));

            //API 1 - LB check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi1We--HTTP",
                    StringComparison.Ordinal) &&
                s.Value.Equals("http://1.1.1.1",
                    StringComparison.OrdinalIgnoreCase));

            //API 2 - Internal - LB check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi2We--HTTP",
                    StringComparison.Ordinal) &&
                s.Value.Equals("http://5.5.5.5",
                    StringComparison.OrdinalIgnoreCase));
        }

        [Fact, IsLayer1]
        public async Task TestDNSProjected_LongNames()
        {
            GetStandardOutput("azscan", "dns", "--keyVault", AzScanCLITestsL1Fixture.OutputKeyVaultName, "--subscription",
                AzScanCLITestsL1Fixture.SierraIntegrationSubscription, "--region", AzScanCLITestsL1Fixture.TargetRegionName);

            var secrets = await _fixture.LoadAllKeyVaultSecretsAsync();
            secrets.Where(s => s.SecretIdentifier.Name.StartsWith("Platform", StringComparison.Ordinal)).Should()
                .HaveCount(4);
            //CNAME check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--clitestdomainaresourcegroupApi--Global",
                    StringComparison.Ordinal) &&
                s.Value.Equals("https://clitestdomainaresourcegroup-api.clitestdomainaresourcegroup.dns",
                    StringComparison.OrdinalIgnoreCase));

            //API 1 - AG check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi1We--HTTPS",
                    StringComparison.Ordinal) &&
                s.Value.Equals("https://3.3.3.3",
                    StringComparison.OrdinalIgnoreCase));

            //API 1 - LB check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi1We--HTTP",
                    StringComparison.Ordinal) &&
                s.Value.Equals("http://1.1.1.1",
                    StringComparison.OrdinalIgnoreCase));

            //API 2 - Internal - LB check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi2We--HTTP",
                    StringComparison.Ordinal) &&
                s.Value.Equals("http://5.5.5.5",
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
