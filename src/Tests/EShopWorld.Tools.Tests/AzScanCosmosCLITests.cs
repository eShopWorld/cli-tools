using System;
using System.Collections.Generic;
using System.Linq;
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
            await Task.WhenAll(RegionHelper.DeploymentRegionsToList().Select(r => Task.Run(async () =>
            {
                await _fixture.SetSecret(r.ToRegionCode(), "CosmosDB--dummy--dummy", "dummy");
                    //following secrets are not to be touched by the CLI
                    await _fixture.SetSecret(r.ToRegionCode(), "CosmosDBBlah", "dummy");
                await _fixture.SetSecret(r.ToRegionCode(), "Prefix--blah", "dummy");
            })));

            InvokeCLI("azscan", "cosmosDb", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription,
                domainParam, AzScanCLITestsL2FixtureBase.TestDomain);

            await Task.WhenAll(RegionHelper.DeploymentRegionsToList().Select(r => Task.Run(async () =>
            {
                var secrets = await _fixture.LoadAllKeyVaultSecrets(r.ToRegionCode());
                var deletedSecrets = await _fixture.LoadAllDeletedSecrets(r.ToRegionCode());
                await CheckSecrets(secrets);
                CheckSideSecrets(secrets, deletedSecrets);
            })));

        }

        [Fact, IsLayer2]
        public async Task CheckCosmosKeyRotation()
        {

            await _fixture.DeleteAllSecretsAcrossRegions();
            //set up dummy secrets
            await Task.WhenAll(RegionHelper.DeploymentRegionsToList().Select(r => Task.Run(async () =>
            {
                await _fixture.SetSecret(r.ToRegionCode(), "CosmosDB--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(r.ToRegionCode(), "CosmosDBBlah", "dummy");
                await _fixture.SetSecret(r.ToRegionCode(), "Prefix--blah", "dummy");
            })));

            InvokeCLI("azscan", "cosmosDb", "-s", AzScanCLITestsL2Fixture.SierraIntegrationSubscription,
                "-d", AzScanCLITestsL2FixtureBase.TestDomain, "-2");

            await Task.WhenAll(RegionHelper.DeploymentRegionsToList().Select(r => Task.Run(async () =>
            {
                var secrets = await _fixture.LoadAllKeyVaultSecrets(r.ToRegionCode());
                var deletedSecrets = await _fixture.LoadAllDeletedSecrets(r.ToRegionCode());
                await CheckSecrets(secrets, true);
                CheckSideSecrets(secrets, deletedSecrets);
            })));
        }

        internal async Task CheckSecrets(IList<SecretBundle> secrets, bool useSecondary = false)
        {
            secrets.Should()
                .ContainSingle(s => s.SecretIdentifier.Name.StartsWith("CosmosDB--", StringComparison.Ordinal));

            var cosmosKeys = await _fixture.TestCosmosDbAccount.ListKeysAsync();

            secrets.Should().ContainSingle(s =>
                s.SecretIdentifier.Name.Equals("CosmosDB--A--ConnectionString",
                    StringComparison.Ordinal) &&
                s.Value.Equals(
                    $"AccountEndpoint=https://esw-a-integration.documents.azure.com:443/;AccountKey={(useSecondary ? cosmosKeys.SecondaryMasterKey : cosmosKeys.PrimaryMasterKey)}",
                    StringComparison.Ordinal));
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, IEnumerable<DeletedSecretItem> deletedSecrets)
        {
            secrets.Should().HaveSecret("CosmosDBBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            deletedSecrets.Should().HaveDeletedSecret("CosmosDB--dummy--dummy");
        }
    }
}
