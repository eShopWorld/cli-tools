using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.KeyVault.Models;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanAppInsightsCLITests :CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanAppInsightsCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer2]
        public void CheckOptions()
        {
            var content = GetStandardOutput("azscan", "ai", "-h");
            content.Should().ContainAll("-s", "--subscription", "-r", "--region", "-g", "--resourceGroup", "-k",
                "--keyVault");
        }

        [Fact, IsLayer2]
        public async Task AIResourcesProjectedPerResourceGroup_ShortNames()
        {
            //run the CLI
            // ReSharper disable once StringLiteralTypo
            GetStandardOutput("azscan", "ai", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName, "-g", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
            //check the KV
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("AI--", StringComparison.Ordinal));
            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("AI--clitestdomainaresourcegroup--InstrumentationKey",
                    StringComparison.Ordinal) &&
                Guid.Parse(s.Value) != default(Guid)); //check key existence and that it is guid (parse succeeds)
        }

        [Fact, IsLayer2]
        public async Task AIResourcesProjectedPerResourceGroup_LongNames()
        {
            //run the CLI
            // ReSharper disable once StringLiteralTypo
            GetStandardOutput("azscan", "ai", "--keyVault", AzScanCLITestsL2Fixture.OutputKeyVaultName, "--subscription",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "--region", AzScanCLITestsL2Fixture.TargetRegionName, "--resourceGroup", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }
    }
}
