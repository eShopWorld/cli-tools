using System;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using EShopWorld.Tools;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class SecurityCLITestsL2Fixture : IDisposable
    {
        private IContainer _container;   
        private TestConfig _testConfig;
        private IAzure _azClient;
        internal KeyVaultClient KeyVaultClient;

        private const string ResourceGroupName = "SecurityCLITestRG";
        internal const string KeyVaultName = "SecurityCLITestKV";

        public SecurityCLITestsL2Fixture()
        {
            SetupFixture();
            CreateResources().GetAwaiter().GetResult();
        }

        private async Task CreateResources()
        {
            _azClient = _container.Resolve<IAzure>();

            var rg = _azClient.ResourceGroups.Define(ResourceGroupName).WithRegion(Region.EuropeWest);
            await _azClient.Vaults.Define(KeyVaultName)
                .WithRegion(Region.EuropeWest)                
                .WithNewResourceGroup(rg)                
                .DefineAccessPolicy()
                    .ForObjectId(_testConfig.TargetIdentityObjectId)
                    .AllowKeyAllPermissions()
                    .AllowSecretAllPermissions()
                .Attach()                
                .WithSku(SkuName.Premium) //must be premium due to HSM
                .CreateAsync();
        }

        private async Task DeleteResources()
        {
            if (_azClient != null)
            {
                await _azClient.ResourceGroups.DeleteByNameAsync(ResourceGroupName);
            }
        }

        private void SetupFixture()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(Program).Assembly);
            builder.Register((ctx) => ctx.Resolve<Azure.IAuthenticated>().WithSubscription(EswDevOpsSdk.SierraIntegrationSubscriptionId)).SingleInstance();
            _container = builder.Build();

            KeyVaultClient = _container.Resolve<KeyVaultClient>();

            var testConfigRoot = EswDevOpsSdk.BuildConfiguration();

            _testConfig = new TestConfig();
            testConfigRoot.GetSection("CLIToolingIntTest").Bind(_testConfig);
        }

        public void Dispose()
        {
            DeleteResources().GetAwaiter().GetResult();
            _container.Dispose();
        }
    }

    [CollectionDefinition(nameof(SecurityCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class SecurityCLITestsL2Collection : ICollectionFixture<SecurityCLITestsL2Fixture>
    { }
}
