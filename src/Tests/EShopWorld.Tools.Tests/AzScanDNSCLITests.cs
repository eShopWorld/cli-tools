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
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _fixture.SetSecret(region.ToRegionCode(), "Platform--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "PlatformBlah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            InvokeCLI("azscan", "dns", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2Fixture.TestDomain);
            var weSecrets = await _fixture.LoadAllKeyVaultSecretsAsync(DeploymentRegion.WestEurope.ToRegionCode());
            var disabledWeSecrets =
                await _fixture.LoadAllDisabledKeyVaultSecretsAsync(DeploymentRegion.WestEurope.ToRegionCode());

            CheckSecretsWE(weSecrets);
            CheckSideSecrets(weSecrets, disabledWeSecrets);

            var eusSecrets = await _fixture.LoadAllKeyVaultSecretsAsync(DeploymentRegion.EastUS.ToRegionCode());
            var disabledEusSecrets =
                await _fixture.LoadAllDisabledKeyVaultSecretsAsync(DeploymentRegion.EastUS.ToRegionCode());

            CheckSecretsEUS(eusSecrets);
            CheckSideSecrets(eusSecrets, disabledEusSecrets);
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
                default:
                    throw new ApplicationException($"Unsupported test region {region.ToRegionCode()}");
            }
        }

        // ReSharper disable once InconsistentNaming
        private void CheckSecretsWE(IList<SecretBundle> secrets)
        {           
            secrets.Should().HaveSecretCountWithNameStarting("Platform--", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--Testapi1--Global", "https://testapi1.platformintegration.dns");
            //API 1 - AG check
            secrets.Should().HaveSecret("Platform--Testapi1--Gateway", "https://testapi1-we.platformintegration.dns");
            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--Testapi1--Cluster", $"http://{_fixture.WeIpAddress.IPAddress}:1111");
            //API 1 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi1--Proxy");
            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--Testapi2--Cluster", $"http://{_fixture.WeIpAddress.IPAddress}:1112");
            //API 2 - no gateway
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Gateway");
            //API 2 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Proxy");
        }

        // ReSharper disable once InconsistentNaming
        private void CheckSecretsEUS(IList<SecretBundle> secrets)
        {
            secrets.Should().HaveSecretCountWithNameStarting("Platform--", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--Testapi1--Global", "https://testapi1.platformintegration.dns");
            //API 1 - AG check
            secrets.Should().HaveSecret("Platform--Testapi1--Gateway", "https://testapi1-eus.platformintegration.dns");
            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--Testapi1--Cluster", $"http://{_fixture.EusIpAddress.IPAddress}:2222");
            //API 1 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi1--Proxy");
            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--Testapi2--Cluster", $"http://{_fixture.EusIpAddress.IPAddress}:2223");
            //API 2 - no gateway
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Gateway");
            //API 2 - no proxy
            secrets.Should().NotHaveSecretByName("Platform--Testapi2--Proxy");
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, IList<SecretItem> disabledSecrets)
        {
            secrets.Should().HaveSecret("PlatformBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            disabledSecrets.Should().HaveDisabledSecret("Platform--dummy--dummy");
        }
    }
}
