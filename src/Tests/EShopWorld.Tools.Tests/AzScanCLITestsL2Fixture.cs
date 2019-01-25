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
        private IContainer _container;
        private KeyVaultClient _keyVaultClient;
        private TestConfig _testConfig;

        private IAzure _azClient;

        internal const string DomainAResourceGroupName = "CLITestDomainAResourceGroup";
        private const string DomainBResourceGroupName = "CLITestDomainBResourceGroup";
        public const string OutputKeyVaultName = "CLIToolsAzScanTestOutput"; //create it in Domain A
        public const string SierraIntegrationSubscription = "sierra-integration"; 
        private static readonly Region Region = Region.EuropeWest;
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
            _container = builder.Build();

            _keyVaultClient = _container.Resolve<KeyVaultClient>();
            var testConfigRoot  = EswDevOpsSdk.BuildConfiguration(true);
            _testConfig = new TestConfig();
            testConfigRoot.GetSection("CLIToolingIntTest").Bind(_testConfig);
        }

        private async Task CreateResources()
        {
            _azClient = _container.Resolve<IAzure>();
          
            //set up more then one "domain"/RG to test filtering
            var rgA = await _azClient.ResourceGroups.Define(DomainAResourceGroupName).WithRegion(Region).CreateAsync();
            var rgB = await _azClient.ResourceGroups.Define(DomainBResourceGroupName).WithRegion(Region).CreateAsync();

            await Task.WhenAll(
                SetupOutputKV(rgA),
                SetupAzureMonitorAsync(rgA),
                SetupAzureMonitorAsync(rgB),
                SetupServiceBusNamespace(rgA),
                SetupServiceBusNamespace(rgB),
                SetupCosmosDBInstance(rgA),
                SetupCosmosDBInstance(rgB),
                SetupDNS(rgA));
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetupOutputKV(IResourceGroup rg)
        {            
            await _azClient.Vaults
                .Define(OutputKeyVaultName)
                    .WithRegion(rg.RegionName)
                .WithExistingResourceGroup(rg)
                .DefineAccessPolicy()
                    .ForObjectId(_testConfig.TargetIdentityObjectId) //so that CLI can write and test
                    .AllowSecretPermissions(SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set)
                .Attach()
                .CreateAsync();         
        }

        private async Task SetupAzureMonitorAsync(IResourceGroup rg)
        {
            var aiClient = _container.Resolve<ApplicationInsightsManagementClient>();
            aiClient.SubscriptionId = _azClient.SubscriptionId;
            await aiClient.Components.CreateAIInstanceIfNotExists($"{rg.Name}-ci", rg.Name);
        }

        private async Task SetupServiceBusNamespace(IResourceGroup rg)
        {
            await _azClient.ServiceBusNamespaces.Define($"{rg.Name}-ci").WithRegion(Region).WithExistingResourceGroup(rg).CreateAsync();
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetupCosmosDBInstance(IResourceGroup rg)
        {
            await _azClient.CosmosDBAccounts.Define($"{rg.Name}-ci".ToLowerInvariant()).WithRegion(Region)
                .WithExistingResourceGroup(rg).WithDataModelSql().WithEventualConsistency().WithWriteReplication(Region)
                .CreateAsync();
        }

        private async Task SetupDNS(IResourceGroup rg)
        {
            var camelRgName = rg.Name.ToCamelCase();

            await _azClient.DnsZones.Define($"{camelRgName}.dns").WithExistingResourceGroup(rg)
                .DefineCNameRecordSet($"{camelRgName}-api").WithAlias($"{camelRgName}-api.azureedge.net").Attach() //Global record
                .DefineARecordSet("TestAPI1-we-lb").WithIPv4Address("1.1.1.1").Attach() //test api 1 LB -WE
                .DefineARecordSet("TestAPI1-eus-lb").WithIPv4Address("2.2.2.2").Attach() //test api 1 LB - EUS
                .DefineARecordSet("TestAPI1-we").WithIPv4Address("3.3.3.3").Attach() //test api 1 AG - WE
                .DefineARecordSet("TestAPI1-eus").WithIPv4Address("4.4.4.4").Attach() //test api 1 AG - EUS
                .DefineARecordSet("TestAPI2-we").WithIPv4Address("5.5.5.5").Attach() //test api 2 - internal API - LB only - WE
                .DefineARecordSet("TestAPI2-eus").WithIPv4Address("6.6.6.6").Attach() //test api 2 - internal API - LB only - EUS
                .CreateAsync();
        }

        internal Task<IList<SecretBundle>> LoadAllKeyVaultSecretsAsync()
        {
            return _keyVaultClient.GetAllSecrets(OutputKeyVaultName);
        }

        public void Dispose()
        {
            DeleteResources().GetAwaiter().GetResult();
            _container.Dispose();
        }

        private async Task DeleteResources()
        {
            if (_azClient != null)
            {
                //TODO: uncomment
                //await Task.WhenAll(
                //    _azClient.ResourceGroups.DeleteRGIfExists(DomainAResourceGroupName),
                //    _azClient.ResourceGroups.DeleteRGIfExists(DomainBResourceGroupName));
            }
        }
    }

    [CollectionDefinition(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanCLITestsL2Collection : ICollectionFixture<AzScanCLITestsL2Fixture>
    { }
}
