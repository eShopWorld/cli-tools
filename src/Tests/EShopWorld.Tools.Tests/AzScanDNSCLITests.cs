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
    public class AzScanDNSCLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanDNSCLITests(AzScanCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }


        [InlineData("-s", "-d")]
        [Theory, IsLayer2]
        // ReSharper disable once InconsistentNaming
        public async Task CheckDNSExpectedSecretProcess(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();

            //set up dummy secrets
            var initTasks = RegionHelper.DeploymentRegionsToList().Select(r => Task.Run(async () =>
            {
                await _fixture.SetSecret(r.ToRegionCode(), "Platform--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(r.ToRegionCode(), "PlatformBlah", "dummy");
                await _fixture.SetSecret(r.ToRegionCode(), "Prefix--blah", "dummy");
            }));

            await Task.WhenAll(initTasks);

            InvokeCLI("azscan", "dns", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam,
                AzScanCLITestsL2FixtureBase.TestDomain);

            var weCheckTask = Task.Run(async () =>
            {
                var weSecrets = await _fixture.LoadAllKeyVaultSecrets(DeploymentRegion.WestEurope.ToRegionCode());
                var deletedWeSecrets =
                    await _fixture.LoadAllDeletedSecrets(DeploymentRegion.WestEurope.ToRegionCode());

                CheckSecretsWE(weSecrets);
                CheckSideSecrets(weSecrets, deletedWeSecrets);
            });

            var eusCheckTask = Task.Run(async () =>
            {
                var eusSecrets = await _fixture.LoadAllKeyVaultSecrets(DeploymentRegion.EastUS.ToRegionCode());
                var deletedEusSecrets =
                    await _fixture.LoadAllDeletedSecrets(DeploymentRegion.EastUS.ToRegionCode());

                CheckSecretsEUS(eusSecrets);
                CheckSideSecrets(eusSecrets, deletedEusSecrets);
            });

            var saCheckTask = Task.Run(async () =>
            {
                var saSecrets = await _fixture.LoadAllKeyVaultSecrets(DeploymentRegion.SoutheastAsia.ToRegionCode());
                var deletedSaSecrets =
                    await _fixture.LoadAllDeletedSecrets(DeploymentRegion.SoutheastAsia.ToRegionCode());

                CheckSecretsSA(saSecrets);
                CheckSideSecrets(saSecrets, deletedSaSecrets);
            });

            await Task.WhenAll(weCheckTask, eusCheckTask, saCheckTask);
        }

        internal void CheckSecrets(IList<SecretBundle> secrets, DeploymentRegion region)
        {
            switch (region)
            {
                case DeploymentRegion.EastUS:
                    CheckSecretsEUS(secrets);
                    return;
                case DeploymentRegion.WestEurope:
                    CheckSecretsWE(secrets);
                    return;
                case DeploymentRegion.SoutheastAsia:
                    CheckSecretsSA(secrets);
                    break;
                default:
                    throw new ApplicationException($"Unsupported test region {region.ToRegionCode()}");
            }
        }

        // ReSharper disable once InconsistentNaming
        private void CheckSecretsWE(IList<SecretBundle> secrets)
        {
            secrets.Should().HaveSecretCountWithNameStarting("Platform--", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--Testapi1--Global", "https://testapi1.platformintegration.private");
            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--Testapi1--Cluster", $"https://testapi1-we.platformintegration.private:1111");
            //API 1 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi1--Proxy");
            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--Testapi2--Cluster", $"https://testapi2-we.platformintegration.private:1112");
            //API 2 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Proxy");
        }

        // ReSharper disable once InconsistentNaming
        private void CheckSecretsEUS(IList<SecretBundle> secrets)
        {
            secrets.Should().HaveSecretCountWithNameStarting("Platform--", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--Testapi1--Global", "https://testapi1.platformintegration.private");
            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--Testapi1--Cluster", $"https://testapi1-eus.platformintegration.private:2222");
            //API 1 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi1--Proxy");
            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--Testapi2--Cluster", $"https://testapi2-eus.platformintegration.private:2223");
            //API 2 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Proxy");
        }

        // ReSharper disable once InconsistentNaming
        private void CheckSecretsSA(IList<SecretBundle> secrets)
        {
            secrets.Should().HaveSecretCountWithNameStarting("Platform--", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--Testapi1--Global", "https://testapi1.platformintegration.private");
            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--Testapi1--Cluster", $"https://testapi1-sa.platformintegration.private:3333");
            //API 1 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi1--Proxy");
            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--Testapi2--Cluster", $"https://testapi2-sa.platformintegration.private:3334");
            //API 2 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Proxy");
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, IEnumerable<DeletedSecretItem> deletedSecrets)
        {
            secrets.Should().HaveSecret("PlatformBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            deletedSecrets.Should().HaveDeletedSecret("Platform--dummy--dummy");
        }
    }
}
