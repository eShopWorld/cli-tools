﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
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
        [InlineData("--subscription", "--domain")]
        [Theory, IsLayer2]
        // ReSharper disable once InconsistentNaming
        public async Task CheckDNSResourcesProjected(string subParam, string domainParam)
        {
            await _fixture.DeleteAllSecretsAcrossRegions();
           
            GetStandardOutput("azscan", "dns", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2Fixture.TestDomain);

            CheckSecretsWE(await _fixture.LoadAllKeyVaultSecretsAsync(DeploymentRegion.WestEurope.ToRegionCode()));
            CheckSecretsEUS(await _fixture.LoadAllKeyVaultSecretsAsync(DeploymentRegion.EastUS.ToRegionCode()));
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
        private static void CheckSecretsWE(IList<SecretBundle> secrets)
        {           
            secrets.Should().HaveSecretCountWithNameStarting("Platform", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--testapi1--Global", "https://testapi1.aintegration.dns");

            //API 1 - AG check
            secrets.Should().HaveSecret("Platform--testapi1--HTTPS", "https://3.3.3.3");

            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--testapi1--HTTP", "http://1.1.1.1");

            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--testapi2--HTTP", "http://5.5.5.5");

        }

        // ReSharper disable once InconsistentNaming
        private static void CheckSecretsEUS(IList<SecretBundle> secrets)
        {
            secrets.Should().HaveSecretCountWithNameStarting("Platform", 4);

            //CNAME check
            secrets.Should().HaveSecret("Platform--testapi1--Global", "https://testapi1.aintegration.dns");

            //API 1 - AG check
            secrets.Should().HaveSecret("Platform--testapi1--HTTPS", "https://4.4.4.4");

            //API 1 - LB check
            secrets.Should().HaveSecret("Platform--testapi1--HTTP", "http://2.2.2.2");

            //API 2 - Internal - LB check
            secrets.Should().HaveSecret("Platform--testapi2--HTTP", "http://6.6.6.6");     
        }
    }
}
