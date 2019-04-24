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

            GetStandardOutput("azscan", "dns", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2Fixture.TestDomain);
            var weSecrets = await _fixture.LoadAllKeyVaultSecretsAsync(DeploymentRegion.WestEurope.ToRegionCode());
            CheckSecretsWE(weSecrets);
            CheckSideSecrets(weSecrets, DeploymentRegion.WestEurope.ToRegionCode());

            var eusSecrets = await _fixture.LoadAllKeyVaultSecretsAsync(DeploymentRegion.EastUS.ToRegionCode());
            CheckSecretsEUS(eusSecrets);
            CheckSideSecrets(eusSecrets, DeploymentRegion.EastUS.ToRegionCode());
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
            secrets.Should().HaveSecret("Platform--testapi1--Global", "https://testapi1.platformintegration.dns");

            //API 1 - AG check
            secrets.Should().HaveSecret("Platform--testapi1--HTTPS", "https://3.3.3.3");

            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--testapi1--HTTP", $"http://{_fixture.WeIpAddress.IPAddress}:1111");

            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--testapi2--HTTP", $"http://{_fixture.WeIpAddress.IPAddress}:1112");          
          }

        // ReSharper disable once InconsistentNaming
        private void CheckSecretsEUS(IList<SecretBundle> secrets)
        {
            secrets.Should().HaveSecretCountWithNameStarting("Platform--", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--testapi1--Global", "https://testapi1.platformintegration.dns");

            //API 1 - AG check
            secrets.Should().HaveSecret("Platform--testapi1--HTTPS", "https://4.4.4.4");

            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--testapi1--HTTP", $"http://{_fixture.EusIpAddress.IPAddress}:2222");

            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--testapi2--HTTP", $"http://{_fixture.EusIpAddress.IPAddress}:2223");         
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, string regionCode)
        {
            secrets.Should().HaveSecret("PlatformBlah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            _fixture.GetDisabledSecret(regionCode, "Platform--dummy--dummy").Should().NotBeNull();
        }
    }
}
