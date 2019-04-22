using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AzScan;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class AzScanKustoCommandTests
    {
        [InlineData("/subscriptions/49c77085-e8c5-4ad2-8114-1d4e71a64aaa/resourceGroups/kusto-test/providers/Microsoft.Kusto/Clusters/eswtest", "kusto-test")]
        [InlineData("/subscriptions/49c77085-e8c5-4ad2-8114-1d4e71a64aaa/resourceGroups/kusto-test2/providers/Microsoft.Kusto/Clusters/eswtest", "kusto-test2")]
        [InlineData("/subscriptions/49c77085-e8c5-4ad2-8114-1d4e71a64aaa/resourceGroups/Kusto-test2/providers/Microsoft.Kusto/Clusters/eswtest", "Kusto-test2")]
        [InlineData("/subscriptions/49c77085-e8c5-4ad2-8114-1d4e71a64aaa/resourceGroups/Kusto.a_b(c)d-test2/providers/Microsoft.Kusto/Clusters/eswtest", "Kusto.a_b(c)d-test2")]
        [Theory, IsLayer0]
        public void GetResourceGroupName(string input, string rgNameExpectation)
        {
            AzScanKustoCommand.GetResourceGroupName(input).Should().Be(rgNameExpectation);
        }
    }
}
