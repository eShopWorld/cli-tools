﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AzScan;
using EShopWorld.Tools.Common;
using FluentAssertions;
using Microsoft.Azure.KeyVault.Models;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// secrets tests for <see cref="AzScanEnvironmentInfoCommand"/>
    /// </summary>
    [Collection(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanEnvironmentInfoCommandCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanEnvironmentInfoCommandCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [InlineData("-s", "-d")]
        [Theory, IsLayer2]
        // ReSharper disable once InconsistentNaming
        public async Task CheckExpectedSecretProcess(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();
            //set up dummy secrets
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _fixture.SetSecret(region.ToRegionCode(), "Environment--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "EnvironmentBlah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            // ReSharper disable once StringLiteralTypo
            InvokeCLI("azscan", "environmentInfo", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2FixtureBase.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecrets(region.ToRegionCode());
                var deletedSecrets = await _fixture.LoadAllDeletedSecrets(region.ToRegionCode());

                CheckSecrets(secrets);
                CheckSideSecrets(secrets, deletedSecrets);
            }
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
            //check the KV        
            secrets.Should().HaveSecret("Environment--SubscriptionId", EswDevOpsSdk.SierraIntegrationSubscriptionId);
            secrets.Should().HaveSecret("Environment--SubscriptionName", "sierra-integration");
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, IList<DeletedSecretItem> deletedSecrets)
        {
            secrets.Should().HaveSecret("EnvironmentBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            deletedSecrets.Should().HaveDeletedSecret("Environment--dummy--dummy");
        }
    }
}
