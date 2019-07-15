using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AzScan;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class AzScanCommandBaseTests
    {
        [Theory, IsLayer0]
        [InlineData("evo-sandbox", DeploymentEnvironment.Sand)]
        [InlineData("evo-ci", DeploymentEnvironment.CI)]
        [InlineData("evo-prod", DeploymentEnvironment.Prod)]
        [InlineData("evo-preprod", DeploymentEnvironment.Prep)]
        [InlineData("evo-test", DeploymentEnvironment.Test)]
        public void TestEnvironmentMapping(string sub, DeploymentEnvironment expected)
        {
            new TestCommandBaseImpl() {Subscription = sub}.Environment.Should().Be(expected);
        }

        private class TestCommandBaseImpl : AzScanCommandBase
        {
            internal TestCommandBaseImpl() : base(null)
            {

            }
            protected override Task<int> RunScanAsync(IAzure client, IConsole console)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
