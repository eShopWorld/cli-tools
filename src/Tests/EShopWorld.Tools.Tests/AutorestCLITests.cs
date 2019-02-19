using System.IO;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once InconsistentNaming
    public class AutorestCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer2]
        public void CheckOptionsForGenerateProjectFile()
        {
            var console = GetStandardOutput("autorest", "generateProjectFile", "--help");

            console.Should().NotBeNullOrWhiteSpace();
            console.Should().ContainAll("-s", "--swagger", "-o", "--output", "-t", "--tfm");
        }

        [Theory, IsLayer2]
        [InlineData("--swagger","--output")]
        [InlineData("-s", "-o")]

        public void GenerateProjectFileFlow(string swaggerParam, string outputParam)
        {
            var output = Path.GetTempPath();
            DeleteTestFiles(output, "SwaggerPetStore.csproj");
            GetStandardOutput("autorest", "generateProjectFile", swaggerParam,
                "https://raw.githubusercontent.com/Azure/autorest/master/Samples/1a-code-generation-minimal/pétstöre.json",
                outputParam, output);
            var projectFile = Path.Combine(output, "SwaggerPetStore.csproj");
            File.Exists(projectFile).Should().BeTrue();
            File.ReadAllText(projectFile).Should().Be(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net462;netstandard2.0</TargetFrameworks>
    <Company>eShopWorld</Company>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <PackageId>eShopWorld.SwaggerPetstore.Configuration</PackageId>
    <Version>1.0.0</Version>
    <Authors>eShopWorld</Authors>
    <Product>SwaggerPetstore</Product>
    <Description>client side library for SwaggerPetstore API</Description>
    <Copyright>eShopWorld</Copyright>
    <AssemblyVersion>1.0.0</AssemblyVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System.Net.Http"" />
    <PackageReference Include=""Microsoft.Rest.ClientRuntime"" Version=""2.*"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.*"" />
  </ItemGroup>
</Project>");
        }    
    }
}
