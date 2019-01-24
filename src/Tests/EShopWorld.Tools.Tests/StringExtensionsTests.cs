using System;
using Eshopworld.Tests.Core;
using FluentAssertions;
using EShopWorld.Tools.Helpers;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class StringExtensionTests
    {
        [Theory, IsLayer0]
        [InlineData("esw-payment-redis", "eswPaymentRedis")]
        [InlineData("esw-payment_redis", "eswPaymentRedis")]
        [InlineData("esw-payment-rEdis", "eswPaymentRedis")]
        [InlineData("esw-payment rEdis", "eswPaymentRedis")]
        [InlineData("esw-payment rEdis.com", "eswPaymentRedisCom")]

        public void ToCamelCase_Success(string input, string expectedOutput)
        {
            input.ToCamelCase().Equals(expectedOutput, StringComparison.Ordinal /*do not ignore case*/).Should().BeTrue();
        }

        [Theory, IsLayer0]
        [InlineData("esw-payment-ci", "esw-payment")]
        [InlineData("esw-payment-test", "esw-payment")]
        [InlineData("esw-payment-sand", "esw-payment")]
        [InlineData("esw-payment-preprod", "esw-payment")]
        [InlineData("esw-payment-prod", "esw-payment")]
        [InlineData("esw-payment-blah", "esw-payment-blah")]
        public void StripRecognizedSuffix_Success(string input, string expectedOutput)
        {
            input.StripRecognizedSuffix("-ci", "-test", "-sand", "-preprod", "-prod").Should().Be(expectedOutput);
        }

        [Theory, IsLayer0]
        [InlineData("aaa", "we", true)]
        [InlineData("aaa-we", "we", true)]
        [InlineData("aaa-we-lb", "we", true)]
        [InlineData("aaa-eus", "we", false)]
        // ReSharper disable once StringLiteralTypo
        [InlineData("aaawe", "we", true)]
        [InlineData("aaa-aa", "we", true)]
        public void RegionCodeCheck(string input, string region, bool expectedResult)
        {
            input.RegionCodeCheck(region).Should().Be(expectedResult);
        }

        [Theory, IsLayer0]
        [InlineData("aaa", "we", true)]
        [InlineData("West Europe", "we", true)]
        [InlineData("East US", "we", false)]
        public void RegionNameCheck(string input, string region, bool expectedResult)
        {
            input.RegionNameCheck(region).Should().Be(expectedResult);
        }

        [Theory, IsLayer0]
        [InlineData("aaa", "we", true)]
        [InlineData("West Europe", "we", true)] //this would indicate wrong check used in the code
        // ReSharper disable once StringLiteralTypo
        [InlineData("westeurope", "we", true)]
        // ReSharper disable once StringLiteralTypo
        [InlineData("eastus", "we", false)]
        public void RegionAbbreviatedNameCheck(string input, string region, bool expectedResult)
        {
            input.RegionAbbreviatedNameCheck(region).Should().Be(expectedResult);
        }
    }
}
