using System;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AzScan;
using FluentAssertions;
using EShopWorld.Tools.Helpers;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class StringExtensionTests
    {
        [Theory, IsUnit]
        [InlineData("esw-payment-redis", "eswPaymentRedis")]
        [InlineData("esw-payment_redis", "eswPaymentRedis")]
        [InlineData("esw-payment-rEdis", "eswPaymentRedis")]
        public void ToCamelCase_Success(string input, string expectedOutput)
        {
            input.ToCamelCase().Equals(expectedOutput, StringComparison.Ordinal /*do not ignore case*/).Should().BeTrue();
        }

        [Theory, IsUnit]
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
    }
}
