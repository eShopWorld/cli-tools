using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using EShopWorld.Tools;
using EShopWorld.Tools.Helpers;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    public class AzScanCLITestsL2Fixture : IDisposable
    {
        internal IContainer Container;
        internal KeyVaultClient KeyVaultClient;
        internal TestConfig TestConfig;

        private IAzure _azClient;

        internal const string DomainAResourceGroupName = "CLITestDomainAResourceGroup";
        internal const string DomainBResourceGroupName = "CLITestDomainBResourceGroup";

        public const string OutputKeyVaultName = "CLIToolsAzScanTestOutput"; //create it in Domain A

        public const string SierraIntegrationSubscription = "sierra-integration"; 

        private static Region Region = Region.EuropeWest;

        public const string TargetRegionName = "we";

        public AzScanCLITestsL2Fixture()
        {
            SetupFixture();
            
            CreateResources().GetAwaiter().GetResult();        
        }

        private void SetupFixture()
        {            
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(Program).Assembly);
            builder.Register((ctx) => ctx.Resolve<Azure.IAuthenticated>().WithSubscription(EswDevOpsSdk.SierraIntegrationSubscriptionId)).SingleInstance();
            Container = builder.Build();

            KeyVaultClient = Container.Resolve<KeyVaultClient>();
            var testConfigRoot  = EswDevOpsSdk.BuildConfiguration(true);
            TestConfig = new TestConfig();
            testConfigRoot.GetSection("CLIToolingIntTest").Bind(TestConfig);
        }

        private async Task CreateResources()
        {
            _azClient = Container.Resolve<IAzure>();
          
            //set up more then one "domain"/RG to test filtering
            var rgA = await _azClient.ResourceGroups.Define(DomainAResourceGroupName).WithRegion(Region).CreateAsync();
            var rgB = await _azClient.ResourceGroups.Define(DomainBResourceGroupName).WithRegion(Region).CreateAsync();

            await Task.WhenAll(
                SetupOutputKV(rgA)/*,
                SetupAzureMonitorAsync(rgA),
                SetupAzureMonitorAsync(rgB),
                SetupServiceBusNamespace(rgA),
                SetupServiceBusNamespace(rgB),
                SetupCosmosDBInstance(rgA),
                SetupCosmosDBInstance(rgB)*/);
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetupOutputKV(IResourceGroup rg)
        {            
            await _azClient.Vaults
                .Define(OutputKeyVaultName)
                    .WithRegion(rg.RegionName)
                .WithExistingResourceGroup(rg)
                .WithEmptyAccessPolicy()
                .DefineAccessPolicy()
                //.("DevOpsApp")
                    .ForObjectId(TestConfig.TargetIdentityObjectId) //so that CLI can write
                    .AllowSecretPermissions(SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set)
                .Attach()
                .CreateAsync();         
        }

        private async Task SetupAzureMonitorAsync(IResourceGroup rg)
        {
            var aiClient = Container.Resolve<ApplicationInsightsManagementClient>();
            aiClient.SubscriptionId = _azClient.SubscriptionId;
            await aiClient.Components.CreateAIInstanceIfNotExists($"{rg.Name}-AI", rg.Name);
        }

        private async Task SetupServiceBusNamespace(IResourceGroup rg)
        {
            await _azClient.ServiceBusNamespaces.Define($"{rg.Name}-SBNamespace").WithRegion(Region).WithExistingResourceGroup(rg).CreateAsync();
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetupCosmosDBInstance(IResourceGroup rg)
        {
            await _azClient.CosmosDBAccounts.Define($"{rg.Name}-Cosmos".ToLowerInvariant()).WithRegion(Region)
                .WithExistingResourceGroup(rg).WithDataModelSql().WithEventualConsistency().WithWriteReplication(Region)
                .CreateAsync();
        }

        internal async Task<IList<SecretItem>> LoadAllKeyVaultSecrets()
        {
            return await KeyVaultClient.GetAllSecrets(OutputKeyVaultName);
        }

        public void Dispose()
        {
            DeleteResources().GetAwaiter().GetResult();
            Container.Dispose();
        }

        private async Task DeleteResources()
        {
            if (_azClient != null) await _azClient.ResourceGroups.DeleteRGIfExists(DomainAResourceGroupName);
        }
    }

    [CollectionDefinition(nameof(CLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class CLITestsL2Collection : ICollectionFixture<AzScanCLITestsL2Fixture>
    { }
}
