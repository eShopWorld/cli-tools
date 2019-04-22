using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using EShopWorld.Tools;
using EShopWorld.Tools.Common;
using JetBrains.Annotations;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ServiceBus.Fluent;
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

        public IServiceBusNamespace TestServiceBusNamespace { get; private set; }
        public ICosmosDBAccount TestCosmosDbAccount { get; private set; }
        public IPublicIPAddress WeIpAddress { get; private set; }
        public IPublicIPAddress EusIpAddress { get; private set; }


        internal const string TestDomain = "a";
        private const string DomainAResourceGroupName = "a-integration";
        private const string DomainAWeResourceGroupNameFormat = "a-integration-{0}";
        private const string DomainBResourceGroupName = "b-integration";
        private const string OutputKeyVaultNameFormat = "esw-a-integration-{0}";
        internal const string SierraIntegrationSubscription = "sierra-integration";
        private const string PlatformResourceGroupName = "platform-integration";
        private const string PlatformRegionalResourceGroupNameFormat = "platform-integration-{0}";

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
            var testConfigRoot = EswDevOpsSdk.BuildConfiguration();

            _testConfig = new TestConfig();
            testConfigRoot.GetSection("CLIToolingIntTest").Bind(_testConfig);
        }

        private async Task CreateResources()
        {
            _azClient = _container.Resolve<IAzure>();

            //set up more then one "domain"/RG to test filtering
            var rgDomainATop = await _azClient.ResourceGroups.Define(DomainAResourceGroupName).WithRegion(Region.EuropeWest).CreateAsync();
            var rgB = await _azClient.ResourceGroups.Define(DomainBResourceGroupName).WithRegion(Region.EuropeWest).CreateAsync();
            //platform RG
            var platformRg = await _azClient.ResourceGroups.Define(PlatformResourceGroupName).WithRegion(Region.EuropeNorth).CreateAsync();

            //setup regional resources
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var regionalRg = await _azClient.ResourceGroups
                    .Define(GetRegionalResourceGroupName(region.ToRegionCode()))
                    .WithRegion(Region.EuropeWest).CreateAsync();

                await _azClient.ResourceGroups
                   .Define(GetRegionalPlatformResourceGroupName(region.ToRegionCode())).WithRegion(Region.EuropeWest)
                   .CreateAsync();

                await SetupOutputKV(regionalRg);
            }

            var cosmosTestResourceTask = SetupCosmosDBInstance(rgDomainATop);
            var sbTestResourceTask = SetupServiceBusNamespace(rgDomainATop);

            await Task.WhenAll(
                SetupAzureMonitor(rgDomainATop),
                SetupAzureMonitor(rgB),
                sbTestResourceTask,
                SetupServiceBusNamespace(rgB),
                cosmosTestResourceTask,
                SetupCosmosDBInstance(rgB),
                SetupNetwork(platformRg));

            TestCosmosDbAccount = cosmosTestResourceTask.Result;
            TestServiceBusNamespace = sbTestResourceTask.Result;
        }

        private static string GetRegionalResourceGroupName([NotNull] string regionCode)
        {
            return string.Format(DomainAWeResourceGroupNameFormat, regionCode).ToLowerInvariant();
        }

        private static string GetRegionalPlatformResourceGroupName([NotNull] string regionCode)
        {
            return string.Format(PlatformRegionalResourceGroupNameFormat, regionCode).ToLowerInvariant();
        }

        // ReSharper disable once InconsistentNaming
        private async Task<IVault> SetupOutputKV(IResourceGroup rg)
        {
            return await _azClient.Vaults
                .Define($"esw-{rg.Name}")
                    .WithRegion(rg.RegionName)
                .WithExistingResourceGroup(rg)
                .DefineAccessPolicy()
                    .ForObjectId(_testConfig.TargetIdentityObjectId) //so that CLI can write and test                
                    .AllowSecretPermissions(SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set, SecretPermissions.Delete)
                .Attach()
                .CreateAsync();
        }

        private async Task<ApplicationInsightsComponent> SetupAzureMonitor(IResourceGroup rg)
        {
            var aiClient = _container.Resolve<ApplicationInsightsManagementClient>();
            aiClient.SubscriptionId = _azClient.SubscriptionId;
            return await aiClient.Components.CreateAIInstanceIfNotExists($"{rg.Name}".ToLowerInvariant(), rg.Name);
        }

        private async Task<IServiceBusNamespace> SetupServiceBusNamespace(IResourceGroup rg)
        {
            return await _azClient.ServiceBusNamespaces.Define($"esw-{rg.Name}".ToLowerInvariant())
                .WithRegion(Region.EuropeWest).WithExistingResourceGroup(rg).CreateAsync();
        }

        // ReSharper disable once InconsistentNaming
        private async Task<ICosmosDBAccount> SetupCosmosDBInstance(IResourceGroup rg)
        {
            return await _azClient.CosmosDBAccounts.Define($"esw-{rg.Name}".ToLowerInvariant()).WithRegion(Region.EuropeWest)
                .WithExistingResourceGroup(rg).WithDataModelSql().WithEventualConsistency().WithWriteReplication(Region.EuropeWest)
                .CreateAsync();
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetupNetwork(IResourceGroup rg)
        {
            var camelRgName = rg.Name.ToCamelCase();
            string testAPI1 = "TestAPI1", testAPI2 = "TestAPI2";            

            WeIpAddress= await _azClient.PublicIPAddresses.Define($"{camelRgName}.wepip").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg).WithStaticIP().WithSku(PublicIPSkuType.Basic).CreateAsync();

            _azClient.LoadBalancers.Define($"{camelRgName}.welb").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg)
                .DefineLoadBalancingRule(testAPI1)
                    .WithProtocol(TransportProtocol.Tcp)
                    .FromExistingPublicIPAddress(WeIpAddress)
                    .FromFrontendPort(1111)
                    .ToBackend(testAPI1)
                    .WithProbe($"{testAPI1}-probe")
                    .Attach()
                .DefineLoadBalancingRule(testAPI2)
                    .WithProtocol(TransportProtocol.Tcp)
                    .FromExistingPublicIPAddress(WeIpAddress)
                    .FromFrontendPort(1112)
                    .ToBackend(testAPI2)
                    .WithProbe($"{testAPI2}-probe")
                    .Attach()
                .WithSku(LoadBalancerSkuType.Basic)
                .DefineHttpProbe($"{testAPI1}-probe")
                    .WithRequestPath("/Probe")
                    .WithPort(1111)
                    .Attach()
                .DefineHttpProbe($"{testAPI2}-probe")
                    .WithRequestPath("/Probe")
                    .WithPort(1112)
                    .Attach()
                .Create();

            EusIpAddress = await _azClient.PublicIPAddresses.Define($"{camelRgName}.euspip").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg).WithStaticIP().WithSku(PublicIPSkuType.Basic).CreateAsync();

            _azClient.LoadBalancers.Define($"{camelRgName}.euslb").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg)
                .DefineLoadBalancingRule(testAPI1)
                    .WithProtocol(TransportProtocol.Tcp)
                    .FromExistingPublicIPAddress(EusIpAddress)
                    .FromFrontendPort(2222)
                    .ToBackend(testAPI1)
                    .WithProbe($"{testAPI1}-probe")
                    .Attach()
                .DefineLoadBalancingRule(testAPI2)
                    .WithProtocol(TransportProtocol.Tcp)
                    .FromExistingPublicIPAddress(EusIpAddress)
                    .FromFrontendPort(2223)
                    .ToBackend(testAPI2)
                    .WithProbe($"{testAPI2}-probe")
                    .Attach()
                .WithSku(LoadBalancerSkuType.Basic)
                .DefineHttpProbe($"{testAPI1}-probe")
                    .WithRequestPath("/Probe")
                    .WithPort(2222)
                    .Attach()
                .DefineHttpProbe($"{testAPI2}-probe")
                    .WithRequestPath("/Probe")
                    .WithPort(2223)
                    .Attach()
                .Create();

            await _azClient.DnsZones.Define($"{camelRgName}.dns".ToLowerInvariant()).WithExistingResourceGroup(rg)
                .DefineCNameRecordSet("TestAPI1").WithAlias($"{camelRgName}-api.azureedge.net").Attach() //Global record
                .DefineARecordSet("TestAPI1-we-lb").WithIPv4Address(WeIpAddress.IPAddress).Attach() //test api 1 LB -WE
                .DefineARecordSet("TestAPI1-eus-lb").WithIPv4Address(EusIpAddress.IPAddress).Attach() //test api 1 LB - EUS
                .DefineARecordSet("TestAPI1-we").WithIPv4Address("3.3.3.3").Attach() //test api 1 AG - WE
                .DefineARecordSet("TestAPI1-eus").WithIPv4Address("4.4.4.4").Attach() //test api 1 AG - EUS
                .DefineARecordSet("TestAPI2-we").WithIPv4Address(WeIpAddress.IPAddress).Attach() //test api 2 - internal API - LB only - WE
                .DefineARecordSet("TestAPI2-eus").WithIPv4Address(EusIpAddress.IPAddress).Attach() //test api 2 - internal API - LB only - EUS
                .CreateAsync();
        }

        internal Task<IList<SecretBundle>> LoadAllKeyVaultSecretsAsync([NotNull] string regionCode)
        {
            return _keyVaultClient.GetAllSecrets(GetRegionalKVName(regionCode));
        }

        internal async Task DeleteAllSecretsAcrossRegions()
        {
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _keyVaultClient.DeleteAllSecrets(GetRegionalKVName(region.ToRegionCode()));
            }
        }

        // ReSharper disable once InconsistentNaming
        private static string GetRegionalKVName([NotNull] string regionCode)
        {
            return string.Format(OutputKeyVaultNameFormat, regionCode.ToLowerInvariant());
        }

        public void Dispose()
        {
            //DeleteResources().GetAwaiter().GetResult();
            _container.Dispose();
        }

        private async Task DeleteResources()
        {
            if (_azClient != null)
            {
                foreach (var region in RegionHelper.DeploymentRegionsToList())
                {
                    await _azClient.ResourceGroups.DeleteRGIfExists(
                        GetRegionalResourceGroupName(region.ToRegionCode()));

                    await _azClient.ResourceGroups.DeleteRGIfExists(
                        GetRegionalPlatformResourceGroupName(region.ToRegionCode()));
                }

                await Task.WhenAll(
                    _azClient.ResourceGroups.DeleteRGIfExists(DomainAResourceGroupName),
                    _azClient.ResourceGroups.DeleteRGIfExists(DomainBResourceGroupName));
            }
        }
    }

    [CollectionDefinition(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanCLITestsL2Collection : ICollectionFixture<AzScanCLITestsL2Fixture>
    { }
}
