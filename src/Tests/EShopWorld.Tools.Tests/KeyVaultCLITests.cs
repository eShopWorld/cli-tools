﻿using System.IO;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Unit.Tests
{
    public class KeyVaultCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer1]
        public void CheckOptions()
        {
            var content = GetStandardOutput("keyvault", "generatePOCOs", "-h");
            content.Should().ContainAll("--appId", "-a", "--appSecret", "-s", "--tenantId", "-t", "--keyVault", "-k",
                "--appName", "-m", "--namespace", "-n", "--obsoleteTag", "-b", "--typeTag", "-g", "--nameTag", "-f",
                "--output", "-o", "--version", "-v");
        }

        [Fact, IsLayer1]
        public void GeneratePOCOsFlow_LongNames()
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration(true);
            var output = Path.GetTempPath();

            DeleteTestFiles(output, "ConfigurationSecrets.cs", "KeyVaultCLITest.csproj");
            GetStandardOutput("keyvault","generatePOCOs", "--appSecret", $"\"{config["appSecret"]}\"", "--appId",
                $"\"{config["appId"]}\"", "--tenantId", $"\"{config["tenantId"]}\"", "--keyVault",
                config["keyvault"], "--appName", "KeyVaultCLITest", "--output", output, "--namespace", "n", "--version", "1.2");

            File.Exists(Path.Combine(output, "ConfigurationSecrets.cs")).Should().BeTrue();
            File.Exists(Path.Combine(output, "KeyVaultCLITest.csproj")).Should().BeTrue();
        }

        [Fact, IsLayer1]
        public void GeneratePOCOsFlow_ShortNames()
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration(true);
            var output = Path.GetTempPath();

            DeleteTestFiles(output, "ConfigurationSecrets.cs", "KeyVaultCLITest.csproj");
            GetStandardOutput("keyvault", "generatePOCOs", "-s", $"\"{config["appSecret"]}\"", "-a",
                $"\"{config["appId"]}\"", "-t", $"\"{config["tenantId"]}\"", "-k",
                config["keyvault"], "-m", "KeyVaultCLITest", "-o", output, "--namespace", "n", "-v", "1.2");

            File.Exists(Path.Combine(output, "ConfigurationSecrets.cs")).Should().BeTrue();
            File.Exists(Path.Combine(output, "KeyVaultCLITest.csproj")).Should().BeTrue();
        }
    }
}