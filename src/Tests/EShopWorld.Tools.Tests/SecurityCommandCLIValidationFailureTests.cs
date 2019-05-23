using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class SecurityCommandCLIValidationFailureTests : CLIInvokingTestsBase
    {
        [InlineData("-a", "", "ERROR - Command McMaster.Extensions.CommandLineUtils.CommandLineApplication`1[EShopWorld.Tools.Program] - Missing value for option 'a'")]
        [InlineData("-a", "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", "Master Key Name cannot be empty or longer than 127 characters")]
        [InlineData("-b", "", "ERROR - Command McMaster.Extensions.CommandLineUtils.CommandLineApplication`1[EShopWorld.Tools.Program] - Missing value for option 'b'")]
        [InlineData("-b", "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", "Master Secret Name cannot be empty or longer than 127 characters")]
        [InlineData("-c", "", "ERROR - Command McMaster.Extensions.CommandLineUtils.CommandLineApplication`1[EShopWorld.Tools.Program] - Missing value for option 'c'")]
        [InlineData("-c", "1", "Master Key Strength must be either 2048, 3072 or 4096")]
        [InlineData("-e", "", "ERROR - Command McMaster.Extensions.CommandLineUtils.CommandLineApplication`1[EShopWorld.Tools.Program] - Missing value for option 'e'")]
        [InlineData("-e", "1", "Secret Encryption Algorithm must be either RSA-OAEP or RSA1_5 or RSA-OAEP-256")]
        [Theory, IsLayer2]        
        public void CallCLI(string param, string value, string expectedError)
        {
            GetErrorOutput("security", "rotateSDSKeys", "-k", "keyvault", param, value).Trim().Should().Be(expectedError);
        }
    }
}
