using Eshopworld.Tests.Core;
using EShopWorld.Tools.Common;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class ProjectFileBuilderTests
    {
        [Fact, IsLayer0]
        public void MainPackageFlow()
        {
            var str = ProjectFileBuilder.CreateEswNetStandard20NuGet("testApp", "1.2.3", "c# poco representation of the testApp configuration Azure KeyVault")
                .GetContent();

            const string expectedContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Company>eShopWorld</Company>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <PackageId>eShopWorld.testApp.Configuration</PackageId>
    <Version>1.2.3</Version>
    <Authors>eShopWorld</Authors>
    <Product>testApp</Product>
    <Description>c# poco representation of the testApp configuration Azure KeyVault</Description>
    <Copyright>eShopWorld</Copyright>
    <AssemblyVersion>1.2.3</AssemblyVersion>
  </PropertyGroup>
</Project>";
            str.Should().Be(expectedContent);
        }

        [Fact, IsLayer0]
        public void MultiplePropertyGroups()
        {
            var str = ProjectFileBuilder.Default()
                .WithPropertyGroup()
                .WithProperty("P1", "val1")
                .Attach()
                .WithPropertyGroup()
                .WithProperty("P2", "val2")
                .Attach()
                .GetContent();

            const string expectedContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <P1>val1</P1>
  </PropertyGroup>
  <PropertyGroup>
    <P2>val2</P2>
  </PropertyGroup>
</Project>";

            str.Should().Be(expectedContent);
        }

        [Fact, IsLayer0]
        public void MultipleAuthorsPropertyTest()
        {
            var str = ProjectFileBuilder.Default()
                .WithPropertyGroup()
                .WithAuthors("a1", "a2")
                .Attach()                
                .GetContent();

            const string expectedContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <Authors>a1,a2</Authors>
  </PropertyGroup>
</Project>";

            str.Should().Be(expectedContent);
        }
    }
}
