using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
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

        [Fact, IsLayer2]
        public async Task CosmosResourcesProjectedPerResourceGroup_ShortOptions()
        {
            GetStandardOutput("azscan", "cosmosDb", "-k", AzScanCLITestsL2Fixture.OutputKeyVaultName, "-s",
                AzScanCLITestsL2Fixture.SierraIntegrationSubscription, "-r", AzScanCLITestsL2Fixture.TargetRegionName, "-g", AzScanCLITestsL2Fixture.DomainAResourceGroupName);

            CheckSecrets(await _fixture.LoadAllKeyVaultSecretsAsync());
        }

        internal static void CheckSecrets(IList<SecretBundle> secrets)
        {          
            secrets.Should()
                .ContainSingle(s => s.SecretIdentifier.Name.StartsWith("CosmosDB--", StringComparison.Ordinal));
            secrets.Should().ContainSingle(s =>
                s.SecretIdentifier.Name.Equals("CosmosDB--clitestdomainaresourcegroup--PrimaryConnectionString", StringComparison.Ordinal) &&
                s.Value.StartsWith("AccountEndpoint=https://clitestdomainaresourcegroup-ci.documents.azure.com:443/;AccountKey=", StringComparison.Ordinal));
        }
    }
}
