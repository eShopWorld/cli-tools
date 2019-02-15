using System;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Common;
using FluentAssertions;
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
        [InlineData("esw-payment-ci", "payment")]
        [InlineData("esw-payment-test", "payment")]
        [InlineData("esw-payment-sand", "payment")]
        [InlineData("esw-payment-prep", "payment")]
        [InlineData("esw-payment-prod", "payment")]
        [InlineData("esw-payment-Prod", "payment")]
        [InlineData("esw-payment-integration", "payment")]
        [InlineData("esw-payment-blah", "payment-blah")]
        [InlineData("blah-payment-blah", "blah-payment-blah")]
        [InlineData("blah-payment-we-lb", "blah-payment", "-we-lb")]
        public void EswTrim_Success(string input, string expectedOutput, params string[] additionalSuffixes)
        {
            input.EswTrim(additionalSuffixes).Should().Be(expectedOutput);
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
        [InlineData("", false)]
        [InlineData("1", true)]
        [InlineData("0", true)]
        [InlineData("-1", false)]
        [InlineData("a", false)]
        [InlineData("1.0", false)]

        public void IsUnsignedIntCheck(string input, bool expectedResult)
        {
            input.IsUnsignedInt().Should().Be(expectedResult);
        }

        [Theory, IsLayer0]
        [InlineData("", "")]
        [InlineData("1aaa", "_1aaa")]
        [InlineData("a", "a")]
        [InlineData("aaa11", "aaa11")]

        public void SanitizePropertyNameCheck(string input, string expectedResult)
        {
            input.SanitizePropertyName().Should().Be(expectedResult);
        }
    }
}
