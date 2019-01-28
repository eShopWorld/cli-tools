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
    public class AzScanSBCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanSBCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer2]
        public void CheckOptions()
        {
            var content = GetStandardOutput("azscan", "serviceBus", "-h");
            content.Should().ContainAll("-s", "--subscription", "-r", "--region", "-g", "--resourceGroup", "-k",
                "--keyVault");
        }

        [Fact, IsLayer2]
        public async Task CheckSBResourcesProjected_ShortNames()
        {
            await _fixture.DeleteAllSecrets();
            GetStandardOutput("azscan", "serviceBus", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName, "-g", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
            //check the KV
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("SB--", StringComparison.Ordinal));
            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("SB--clitestdomainaresourcegroup--PrimaryConnectionString",
                    StringComparison.Ordinal) &&
                s.Value.StartsWith(
                    "Endpoint=sb://clitestdomainaresourcegroup-ci.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=",
                    StringComparison.Ordinal));
        }

        [Fact, IsLayer2]
        public async Task CheckSBResourcesProjected_LongNames()
        {
            await _fixture.DeleteAllSecrets();
            GetStandardOutput("azscan", "serviceBus", "--keyVault", AzScanCLITestsL2Fixture.OutputKeyVaultName, "--subscription",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "--region", AzScanCLITestsL2Fixture.TargetRegionName, "--resourceGroup", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }
    }
}
