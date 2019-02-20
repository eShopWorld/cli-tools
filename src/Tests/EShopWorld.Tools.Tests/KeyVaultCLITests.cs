using System.IO;
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

        [Theory, IsLayer2]
        [InlineData("--keyVault", "--appName","--output","--namespace", "--version")]
        [InlineData("-k", "-m", "-o", "-n", "-v")]

        // ReSharper disable once InconsistentNaming
        public void GeneratePOCOsFlow(string keyVaultParam, string appNameParam, string outputParam, string namespaceParam, string versionParam)
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration(true);
            var output = Path.GetTempPath();

            DeleteTestFiles(output, "Configuration.cs", "KeyVaultCLITest.csproj");
            GetStandardOutput("keyvault", "generatePOCOs", keyVaultParam,
                config["InputTestKeyVault"], appNameParam, "KeyVaultCLITest", outputParam, output, namespaceParam, "n", versionParam, "1.2");

            File.Exists(Path.Combine(output, "Configuration.cs")).Should().BeTrue();
            File.ReadAllText(Path.Combine(output, "Configuration.cs")).Should().Be(@"namespace n
{
    using System;

    public class KeyvaultclitestType
    {
        public _eventType _event
        {
            get;
            set;
        }

        public string keyVaultItem
        {
            get;
            set;
        }

        public class _eventType
        {
            public string _1secret
            {
                get;
                set;
            }

            public string a_b
            {
                get;
                set;
            }
        }
    }
}");
            File.Exists(Path.Combine(output, "KeyVaultCLITest.csproj")).Should().BeTrue();
            File.ReadAllText(Path.Combine(output, "KeyVaultCLITest.csproj")).Should().Be(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Company>eShopWorld</Company>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <PackageId>eShopWorld.KeyVaultCLITest.Configuration</PackageId>
    <Version>1.2</Version>
    <Authors>eShopWorld</Authors>
    <Product>KeyVaultCLITest</Product>
    <Description>c# poco representation of the KeyVaultCLITest configuration Azure KeyVault</Description>
    <Copyright>eShopWorld</Copyright>
    <AssemblyVersion>1.2</AssemblyVersion>
  </PropertyGroup>
</Project>");
        }
    }
}
