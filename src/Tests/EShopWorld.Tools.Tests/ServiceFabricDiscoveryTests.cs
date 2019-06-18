using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AzScan;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class ServiceFabricDiscoveryTests
    {
        [Theory, IsLayer0]
        [InlineData("ci", DeploymentRegion.WestEurope, "fabric-ci.eshopworld.net:19000")]
        [InlineData("test", DeploymentRegion.WestEurope, "fabric-test-we.eshopworld.net:19000")]
        [InlineData("test", DeploymentRegion.EastUS, "fabric-test-eus.eshopworld.net:19000")]
        [InlineData("test", DeploymentRegion.SoutheastAsia, "fabric-test-sa.eshopworld.net:19000")]
        [InlineData("prep", DeploymentRegion.WestEurope, "fabric-preprod-we.eshopworld.net:19000")]
        [InlineData("prep", DeploymentRegion.EastUS, "fabric-preprod-eus.eshopworld.net:19000")]
        [InlineData("prep", DeploymentRegion.SoutheastAsia, "fabric-preprod-sa.eshopworld.net:19000")]
        [InlineData("sand", DeploymentRegion.WestEurope, "fabric-sand-we.eshopworld.com:19000")]
        [InlineData("sand", DeploymentRegion.EastUS, "fabric-sand-eus.eshopworld.com:19000")]
        [InlineData("sand", DeploymentRegion.SoutheastAsia, "fabric-sand-sa.eshopworld.com:19000")]
        [InlineData("prod", DeploymentRegion.WestEurope, "fabric-prod-we.eshopworld.com:19000")]
        [InlineData("prod", DeploymentRegion.EastUS, "fabric-prod-eus.eshopworld.com:19000")]
        [InlineData("prod", DeploymentRegion.SoutheastAsia, "fabric-prod-sa.eshopworld.com:19000")]
        public void TestGetClusterUrl(string env, DeploymentRegion region, string expected)
        {
            ServiceFabricDiscovery.GetClusterUrl(env, region).Should().Be(expected);
        }
    }
}
