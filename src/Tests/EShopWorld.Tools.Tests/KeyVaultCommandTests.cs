using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.KeyVault;
using EShopWorld.Tools.Commands.KeyVault.Models;
using EShopWorld.Tools.Helpers;
using FluentAssertions;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EshopWorld.Tools.Unit.Tests
{    
    public class KeyVaultCommandTests
    {
        [Fact, IsIntegration]
        [Trait("Command", "keyvault")]

        public void GeneratePocoClass_Success()
        {   
            var secrets = new List<SecretItem>(new[]
            {
                new SecretItem
                {
                    Id = "https://rmtestkeyvault.vault.azure.net:443/secrets/Secret1",
                    Tags = new Dictionary<string, string>(new List<KeyValuePair<string, string>>(
                        new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("Type", "appName"),
                            new KeyValuePair<string, string>("Name", "Field1")
                        }))
                },
                new SecretItem
                {
                    Id = "https://rmtestkeyvault.vault.azure.net:443/secrets/Secret2",
                    Tags = new Dictionary<string, string>(new List<KeyValuePair<string, string>>(
                        new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("Type", "diffApp"),
                            new KeyValuePair<string, string>("Name", "Field2")
                        }))
                },
                new SecretItem
                {
                    Id = "https://rmtestkeyvault.vault.azure.net:443/secrets/Secret3",
                    Tags = new Dictionary<string, string>(new List<KeyValuePair<string, string>>(
                        new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("Type", "appName"),
                            new KeyValuePair<string, string>("Name", "Field3"),
                            new KeyValuePair<string, string>("Obsolete", "true")
                        }))
                }
            });
            //act
            // Initialize the necessary services
            var services = new ServiceCollection();
            AspNetRazorEngineServiceSetup.ConfigureDefaultServices<GeneratePocoClassInternalCommand>(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var provider = services.BuildServiceProvider();
            var serviceScope = provider.GetRequiredService<IServiceScopeFactory>();
            using (serviceScope.CreateScope())
            {
                //generate POCOs
                var pocoCommand = provider.GetRequiredService<GeneratePocoClassInternalCommand>();
                var content = pocoCommand.RenderViewToString(new GeneratePocoClassViewModel
                {
                    Namespace = "appName",
                    Fields = secrets.Select(i => new Tuple<string, bool>(
                        i.Tags != null && i.Tags.ContainsKey("Name") ? i.Tags["Name"] : i.Identifier.Name,
                        i.Tags != null && i.Tags.ContainsKey("Obsolete") && Convert.ToBoolean(i.Tags["Obsolete"])))
                });
                content.Should().Be(
                    "using System;\r\n\r\nnamespace eShopWorld.appName\r\n{\r\n    public class ConfigurationSecrets\r\n    {\r\n\t\tpublic string Field1 {get; set;}\n\t\tpublic string Field2 {get; set;}\n\t\t[Obsolete]\n\t\tpublic string Field3 {get; set;}\n    }\r\n}");
            }
        }

        [Fact, IsIntegration]
        [Trait("Command", "keyvault")]
        public void GeneratePocoProject_Success()
        {
            var services = new ServiceCollection();
            AspNetRazorEngineServiceSetup.ConfigureDefaultServices<GeneratePocoProjectInternalCommand>(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var provider = services.BuildServiceProvider();
            var serviceScope = provider.GetRequiredService<IServiceScopeFactory>();
            using (serviceScope.CreateScope())
            {
                //generate project file
                var projectCommand = provider.GetRequiredService<GeneratePocoProjectInternalCommand>();
                var content = projectCommand.RenderViewToString(new GeneratePocoProjectViewModel { AppName = "appName", Version = "1.1.1" });
                content.Should().Be("<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <PropertyGroup>\r\n    <TargetFramework>netstandard2.0</TargetFramework>\r\n    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>\r\n    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>\r\n    <PackageId>eShopWorld.appName.ConfigurationSecrets</PackageId>\r\n    <Version>1.1.1</Version>\r\n    <Authors>eShopWorld</Authors>\r\n    <Company>eShopWorld</Company>\r\n    <Product>product</Product>\r\n    <Description>C# KeyVault representation for  appName</Description>\r\n    <Copyright>eShopWorld</Copyright>    \r\n    <AssemblyVersion>1.1.1</AssemblyVersion>\r\n  </PropertyGroup>\r\n</Project>");
            }
        }
    }
}
