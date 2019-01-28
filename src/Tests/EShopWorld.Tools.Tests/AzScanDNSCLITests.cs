using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.KeyVault.Models;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanDNSCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanDNSCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer2]
        public void CheckOptions()
        {
            // ReSharper disable once StringLiteralTypo
            var content = GetStandardOutput("azscan", "dns", "-h");

            content.Should().ContainAll("-s", "--subscription", "-r", "--region", "-g", "--resourceGroup", "-k",
                "--keyVault");
        }

        [Fact, IsLayer2]
        public async Task CheckDNSResourcesProjected_ShortNames()
        {
            await _fixture.DeleteAllSecrets();
            GetStandardOutput("azscan", "dns", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
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
                s.SecretIdentifier.Name.Equals("Platform--testapi1--HTTPS",
                    StringComparison.Ordinal) &&
                s.Value.Equals("https://3.3.3.3",
                    StringComparison.OrdinalIgnoreCase));

            //API 1 - LB check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi1--HTTP",
                    StringComparison.Ordinal) &&
                s.Value.Equals("http://1.1.1.1",
                    StringComparison.OrdinalIgnoreCase));

            //API 2 - Internal - LB check
            secrets.Should().Contain(s =>
                s.SecretIdentifier.Name.Equals("Platform--testapi2--HTTP",
                    StringComparison.Ordinal) &&
                s.Value.Equals("http://5.5.5.5",
                    StringComparison.OrdinalIgnoreCase));
        }

        [Fact, IsLayer1]
        public async Task CheckDNSResourcesProjected_LongNames()
        {
            await _fixture.DeleteAllSecrets();
            GetStandardOutput("azscan", "dns", "--keyVault", AzScanCLITestsL2Fixture.OutputKeyVaultName, "--subscription",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "--region", AzScanCLITestsL2Fixture.TargetRegionName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }
    }
}
