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
    public class AzScanCosmosCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanCosmosCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }       

        [InlineData("-s", "-d")]
        [InlineData("--subscription", "--domain")]
        [Theory, IsLayer2]
        public async Task CheckCosmosResourcesProjectedPerResourceGroup(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();
            GetStandardOutput("azscan", "cosmosDb", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription,
                domainParam, AzScanCLITestsL2Fixture.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync(region.ToRegionCode()));
            }
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
            secrets.Should()
                .ContainSingle(s => s.SecretIdentifier.Name.StartsWith("CosmosDB--", StringComparison.Ordinal));
            secrets.Should().ContainSingle(s =>
                s.SecretIdentifier.Name.Equals("CosmosDB--a--PrimaryConnectionString",
                    StringComparison.Ordinal) &&
                s.Value.StartsWith(
                    "AccountEndpoint=https://esw-a-integration.documents.azure.com:443/;AccountKey=",
                    StringComparison.Ordinal));
        }
    }
}
