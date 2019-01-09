using Eshopworld.Tests.Core;
using EShopWorld.Tools;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once InconsistentNaming
    public class VersionCLITest : CLIInvokingTestsBase
    {
        [Fact, IsLayer1]
        public void CheckVersionMatch()
        {
            var console = GetStandardOutput("--version");
            console.TrimEnd().Should().Be(typeof(Program).Assembly.GetName().Version.ToString(3)); //check we are getting the expected version of CLI when invoking the command
        }
    }
}
