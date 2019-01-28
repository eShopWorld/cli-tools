﻿using System.IO;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// tests for keyvault CLI command
    /// </summary>
    public class KeyVaultCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer2]
        public void CheckOptions()
        {
            var content = GetStandardOutput("keyvault", "generatePOCOs", "-h");
            content.Should().ContainAll("--keyVault", "-k",
                "--appName", "-m", "--namespace", "-n", "--output", "-o", "--version", "-v");
        }

        [Fact, IsLayer2]
        public void GeneratePOCOsFlow_MSI_LongNames()
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration(true);
            var output = Path.GetTempPath();

            DeleteTestFiles(output, "ConfigurationSecrets.cs", "KeyVaultCLITest.csproj");
            GetStandardOutput("keyvault", "generatePOCOs", "--keyVault",
                config["keyvault"], "--appName", "KeyVaultCLITest", "--output", output, "--namespace", "n", "--version", "1.2");

            File.Exists(Path.Combine(output, "ConfigurationSecrets.cs")).Should().BeTrue();
            File.Exists(Path.Combine(output, "KeyVaultCLITest.csproj")).Should().BeTrue();
        }

        [Fact, IsLayer2]
        public void GeneratePOCOsFlow_MSI_ShortNames()
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration(true);
            var output = Path.GetTempPath();

            DeleteTestFiles(output, "ConfigurationSecrets.cs", "KeyVaultCLITest.csproj");
            GetStandardOutput("keyvault", "generatePOCOs","-k", config["keyvault"], "-m", "KeyVaultCLITest", "-o", output, "--namespace", "n", "-v", "1.2");

            File.Exists(Path.Combine(output, "ConfigurationSecrets.cs")).Should().BeTrue();
            File.Exists(Path.Combine(output, "KeyVaultCLITest.csproj")).Should().BeTrue();
        }
    }
}
