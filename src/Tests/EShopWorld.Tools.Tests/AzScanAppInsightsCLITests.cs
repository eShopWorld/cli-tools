﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Common;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Azure.KeyVault.Models;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanAppInsightsCLITests :CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsL2Fixture _fixture;

        public AzScanAppInsightsCLITests(AzScanCLITestsL2Fixture fixture)
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
                await _fixture.SetSecret(region.ToRegionCode(), "ApplicationInsights--dummy--dummy", "dummy");
                //following secrets are not to be touched by the CLI
                await _fixture.SetSecret(region.ToRegionCode(), "ApplicationInsightsBLah", "dummy");
                await _fixture.SetSecret(region.ToRegionCode(), "Prefix--blah", "dummy");
            }

            // ReSharper disable once StringLiteralTypo
            GetStandardOutput("azscan", "ai", subParam, AzScanCLITestsL2Fixture.SierraIntegrationSubscription, domainParam, AzScanCLITestsL2Fixture.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = await _fixture.LoadAllKeyVaultSecretsAsync(region.ToRegionCode());
                var disabledSecrets = await _fixture.LoadAllDisabledKeyVaultSecretsAsync(region.ToRegionCode());
                CheckSecrets(secrets);
                CheckSideSecrets(secrets, disabledSecrets);
            }
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {
            //check the KV
            secrets.Should().ContainSingle(s => s.SecretIdentifier.Name.StartsWith("ApplicationInsights--", StringComparison.Ordinal));
            secrets.Should().ContainSingle(s =>
                // ReSharper disable once StringLiteralTypo
                s.SecretIdentifier.Name.Equals("ApplicationInsights--InstrumentationKey",
                    StringComparison.Ordinal) &&
                Guid.Parse(s.Value) != default); //check key existence and that it is guid (parse succeeds)
        }

        private void CheckSideSecrets(IList<SecretBundle> secrets, IList<SecretItem> disabledSecrets)
        {
            secrets.Should().HaveSecret("ApplicationInsightsBLah", "dummy");
            secrets.Should().HaveSecret("Prefix--blah", "dummy");
            disabledSecrets.Should().HaveDisabledSecret("ApplicationInsights--dummy--dummy");
        }
    }
}
