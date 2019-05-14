using System.IO;
using System.Reflection;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class KeyVaultExportCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer2]
        public void CheckOptions()
        {
            var content = GetStandardOutput("keyvault", "export", "-h");
            content.Should().ContainAll("--keyVault", "-k", "--output", "-o");
        }

        [Fact, IsLayer2]
        public void ExportTestKVAndBind()
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration();
            var output = Path.GetTempFileName();

            GetStandardOutput("keyvault", "export", "-k", config["POCOBindInputTestKeyVault"], "-o", output);


            File.Exists(output).Should().BeTrue();

            var testConfigBuilder = new ConfigurationBuilder();
            testConfigBuilder.AddJsonFile(output);
            var testConfig = testConfigBuilder.Build();
            var poco = new TestPOCO();

            testConfig.Bind(poco);
            poco.keyVaultItem.Should().NotBeNullOrWhiteSpace();
            poco.SomeDisabled.Should().NotBeNull();
            poco.SomeDisabled.EnabledSecretA.Should().NotBeNullOrWhiteSpace();
        }

        public class TestPOCO
        {
            public string keyVaultItem { get; set; }
            public SomeDisabledType SomeDisabled { get; set; }

            public class SomeDisabledType
            {
                public string EnabledSecretA { get; set; }
            }
        }
    }
}
