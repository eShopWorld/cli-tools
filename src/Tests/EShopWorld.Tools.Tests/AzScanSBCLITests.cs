using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Helpers;
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

        [InlineData("-s", "-d")]
        [InlineData("--subscription", "--domain")]
        [Theory, IsLayer2]
        // ReSharper disable once InconsistentNaming
        public async Task CheckSBResourcesProjected(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();
            GetStandardOutput("azscan", "serviceBus", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2Fixture.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync(region.ToRegionCode()));
            }
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
            //check the KV
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("SB--", StringComparison.Ordinal));
            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("SB--a--PrimaryConnectionString",
                    StringComparison.Ordinal) &&
                s.Value.StartsWith(
                    "Endpoint=sb://esw-a-integration.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=",
                    StringComparison.Ordinal));
        }
    }
}
