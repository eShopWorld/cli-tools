using System;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Common;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(AzScanCLITestsEvoEnvL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanDNSProxyL2CLITests : CLIInvokingTestsBase
    {
        private readonly AzScanCLITestsEvoEnvL2Fixture _fixture;

        public AzScanDNSProxyL2CLITests(AzScanCLITestsEvoEnvL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, IsLayer2]
        public async Task TestProxyRecords()
        {
            InvokeCLI("azscan", "dns", "-s", AzScanCLITestsEvoEnvL2Fixture.TestSubscription,
                "-d", AzScanCLITestsL2FixtureBase.TestDomain);

            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var secrets = (await _fixture.LoadAllKeyVaultSecrets(region.ToRegionCode()))
                    .Where(s=>s.SecretIdentifier.Name.EndsWith("Proxy", StringComparison.Ordinal))
                    .ToList();

                secrets.Should().NotBeEmpty();

                foreach (var secret in secrets)
                {
                    secret.Value.Should().BeValidReverseProxyUrl();
                }
            }
        }
    }
}
