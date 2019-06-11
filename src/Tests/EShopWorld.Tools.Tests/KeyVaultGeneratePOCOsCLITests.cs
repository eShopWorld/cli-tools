using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools;
using EShopWorld.Tools.Common;
using FluentAssertions;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// tests for keyvault CLI command
    /// </summary>
    public class KeyVaultGeneratePOCOsCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer2]
        public void CheckOptions()
        {
            var content = GetStandardOutput("keyvault", "generatePOCOs", "-h");
            content.Should().ContainAll("--keyVault", "-k",
                "--appName", "-m", "--namespace", "-n", "--output", "-o", "--version", "-v");
        }

        [Theory, IsLayer2]
        [InlineData("-k", "-m", "-o", "-n", "-v")]

        // ReSharper disable once InconsistentNaming
        public async Task GeneratePOCOsFlow(string keyVaultParam, string appNameParam, string outputParam, string namespaceParam, string versionParam)
        {
            //config load
            var config = EswDevOpsSdk.BuildConfiguration();
            var output = Path.GetTempPath();

            DeleteTestFiles(output, "Configuration.cs", "KeyVaultCLITest.csproj");
            //make sure the reserved "obsolete" secret is soft-deleted
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(Program).Assembly);

            builder.Register((ctx) => ctx.Resolve<Azure.IAuthenticated>()
                    .WithSubscription(EswDevOpsSdk.SierraIntegrationSubscriptionId))
                .SingleInstance();

            var container = builder.Build();
            var kvClient = container.Resolve<KeyVaultClient>();
            var kvName = config["POCOBindInputTestKeyVault"];
            var deletedSecrets = await kvClient.GetDeletedSecrets(kvName);
            const string secretName = "SoftDeleted--Secret";

            if (deletedSecrets.All(i => i.Identifier.Name != secretName))
            {
                await kvClient.SetKeyVaultSecretAsync(kvName, secretName, "blah");
                await kvClient.DeleteSecret(kvName, secretName);
            }

            InvokeCLI("keyvault", "generatePOCOs", keyVaultParam,
                kvName, appNameParam, "KeyVaultCLITest", outputParam, output, namespaceParam, "n", versionParam, "1.2");

            File.Exists(Path.Combine(output, "Configuration.cs")).Should().BeTrue();
            File.ReadAllText(Path.Combine(output, "Configuration.cs")).Should().Be(@"namespace n
{
    using System;

    public class KeyVaultCLITestConfiguration
    {
        public _eventConfiguration _event
        {
            get;
            set;
        }

        public string keyVaultItem
        {
            get;
            set;
        }

        public SomeDisabledConfiguration SomeDisabled
        {
            get;
            set;
        }

        public SoftDeletedConfiguration SoftDeleted
        {
            get;
            set;
        }

        public class _eventConfiguration
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

        public class SomeDisabledConfiguration
        {
            public string EnabledSecretA
            {
                get;
                set;
            }
        }

        public class SoftDeletedConfiguration
        {
            [System.ObsoleteAttribute(""The underlying platform resource is no longer provisioned"")]
            public string Secret
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
