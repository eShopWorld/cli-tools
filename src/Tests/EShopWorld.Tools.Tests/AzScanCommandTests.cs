using System;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;
using Xunit.Sdk;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// these tests do not require L2 fixture
    /// </summary>
    public class AzScanCommandTests : CLIInvokingTestsBase
    {
        [InlineData("all")]
        [InlineData("ai")]
        [InlineData("cosmosDb")]
        [InlineData("dns")]
        [InlineData("serviceBus")]
        [Theory, IsLayer2]
        public void CheckOptions(string subCommand)
        {
            var content = GetStandardOutput("azscan", subCommand, "-h");
            content.Should().ContainAll("-s", "--subscription", "-d", "--domain");
        }

        [Fact, IsLayer2]
        public void UnsupportedSubCommand()
        {
            Assert.Throws<XunitException>(() => GetStandardOutput("azscan", "blah"));
        }
    }
}
