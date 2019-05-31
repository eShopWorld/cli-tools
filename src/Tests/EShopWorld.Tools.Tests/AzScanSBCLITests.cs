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
    public class AzScanSBCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanSBCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }    

        [InlineData("-s", "-d")]
        [Theory, IsLayer2]
        // ReSharper disable once InconsistentNaming
        public async Task CheckSBExpectedSecretProcess(string subParam, string domainParam)
        {
            try
            {

            
            await _fixture.DeleteAllSecretsAcrossRegions();

            //set up dummy secrets
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _fixture.SetSecret(region.ToRegionCode(), "SB--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "SBBlah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            InvokeCLI("azscan", "serviceBus", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2FixtureBase.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecrets(region.ToRegionCode());
                var deletedSecrets = await _fixture.LoadAllDeletedSecrets(region.ToRegionCode());
                await CheckSecrets(secrets);
                CheckSideSecrets(secrets, deletedSecrets);
            }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Fact, IsLayer2]
        // ReSharper disable once InconsistentNaming
        public async Task CheckSBKeyRotation()
        {
            try
            { 
            await _fixture.DeleteAllSecretsAcrossRegions();
            //set up dummy secrets
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _fixture.SetSecret(region.ToRegionCode(), "SB--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "SBBlah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            InvokeCLI("azscan", "serviceBus", "-s", AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-d", AzScanCLITestsL2FixtureBase.TestDomain, "-2");

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecrets(region.ToRegionCode());
                var deletedSecrets = await _fixture.LoadAllDeletedSecrets(region.ToRegionCode());
                await CheckSecrets(secrets, true);
                CheckSideSecrets(secrets, deletedSecrets);
            }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        internal async Task CheckSecrets(IList<SecretBundle> secrets, bool useSecondary = false)
        {
            //check the KV
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("SB--", StringComparison.Ordinal));

            var rule = await _fixture.TestServiceBusNamespace.AuthorizationRules.GetByNameAsync("RootManageSharedAccessKey");
            var keys = await rule.GetKeysAsync();

            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("SB--A--ConnectionString",
                    StringComparison.Ordinal) &&
                s.Value.Equals(
                    useSecondary ? keys.SecondaryConnectionString : keys.PrimaryConnectionString,
                    StringComparison.Ordinal));         
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, IEnumerable<DeletedSecretItem> deletedSecrets)
        {
            secrets.Should().HaveSecret("SBBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            deletedSecrets.Should().HaveDeletedSecret("SB--dummy--dummy");
        }
    }
}
