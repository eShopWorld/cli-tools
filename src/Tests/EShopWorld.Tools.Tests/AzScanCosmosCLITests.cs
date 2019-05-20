using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Common;
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
        [Theory, IsLayer2]
        public async Task CheckCosmosExpectedSecretProcess(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();

            //set up dummy secrets
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _fixture.SetSecret(region.ToRegionCode(), "Cosmos--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "CosmosBlah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            GetStandardOutput("azscan", "cosmosDb", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription,
                domainParam, AzScanCLITestsL2Fixture.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecretsAsync(region.ToRegionCode());
                await CheckSecrets(secrets);
                await CheckSideSecrets(secrets, region.ToRegionCode());
            }
        }

        [Fact, IsLayer2]
        public async Task CheckCosmosKeyRotation()
        {
            await _fixture.DeleteAllSecretsAcrossRegions();
            //set up dummy secrets
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _fixture.SetSecret(region.ToRegionCode(), "Cosmos--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "CosmosBlah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            GetStandardOutput("azscan", "cosmosDb", "-s", AzScanCLITestsL2Fixture.SierraIntegrationSubscription,
                "-d", AzScanCLITestsL2Fixture.TestDomain, "-2");

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecretsAsync(region.ToRegionCode());
                await CheckSecrets(secrets, true);
                await CheckSideSecrets(secrets, region.ToRegionCode());
            }
        }

        internal async Task CheckSecrets(IList<SecretBundle> secrets, bool useSecondary=false)
        {
            secrets.Should()
                .ContainSingle(s => s.SecretIdentifier.Name.StartsWith("CosmosDB--", StringComparison.Ordinal));

            var cosmosKeys = await _fixture.TestCosmosDbAccount.ListKeysAsync();

            secrets.Should().ContainSingle(s =>
                s.SecretIdentifier.Name.Equals("CosmosDB--A--ConnectionString",
                    StringComparison.Ordinal) &&
                s.Value.Equals(
                    $"AccountEndpoint=https://esw-a-integration.documents.azure.com:443/;AccountKey={(useSecondary?  cosmosKeys.SecondaryMasterKey : cosmosKeys.PrimaryMasterKey)}",
                    StringComparison.Ordinal));
        }

        private async Task CheckSideSecrets(IList<SecretBundle> secrets, string regionCode)
        {
            secrets.Should().HaveSecret("CosmosBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            (await _fixture.CheckIsSoftDeleted(regionCode, "Cosmos--dummy--dummy")).Should().BeTrue();

        }
    }
}
