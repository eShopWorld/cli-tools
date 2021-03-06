﻿using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using EShopWorld.Tools.Common;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    public class AzScanCLITestsL2Fixture : AzScanCLITestsL2FixtureBase
    {
        public IServiceBusNamespace TestServiceBusNamespace { get; private set; }
        public ICosmosDBAccount TestCosmosDbAccount { get; private set; }
        public IPublicIPAddress WeIpAddress { get; private set; }
        public IPublicIPAddress EusIpAddress { get; private set; }
        public IPublicIPAddress SaIpAdress { get; private set; }

        private const string DomainAResourceGroupName = "a-integration";
        private const string DomainBResourceGroupName = "b-integration";
        internal const string SierraIntegrationSubscription = "sierra-integration";
        private const string PlatformResourceGroupName = "platform-integration";
        private const string PlatformRegionalResourceGroupNameFormat = "platform-integration-{0}";

        public AzScanCLITestsL2Fixture() : base("integration")
        {
        }

        protected override async Task CreateResources()
        {
            //set up more then one "domain"/RG to test filtering
            var rgDomainATop = await AzClient.ResourceGroups.Define(DomainAResourceGroupName)
                .WithRegion(Region.EuropeWest).CreateAsync();
            var rgB = await AzClient.ResourceGroups.Define(DomainBResourceGroupName).WithRegion(Region.EuropeWest)
                .CreateAsync();
            //platform RG
            var platformRg = await AzClient.ResourceGroups.Define(PlatformResourceGroupName)
                .WithRegion(Region.EuropeNorth).CreateAsync();

            //setup regional resources
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                var regionalRg = await AzClient.ResourceGroups
                    .Define(GetRegionalResourceGroupName(region.ToRegionCode()))
                    .WithRegion(Region.EuropeWest).CreateAsync();

                await AzClient.ResourceGroups
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

        private static string GetRegionalPlatformResourceGroupName(string regionCode)
        {
            return string.Format(PlatformRegionalResourceGroupNameFormat, regionCode).ToLowerInvariant();
        }

        private async Task<ApplicationInsightsComponent> SetupAzureMonitor(IResourceGroup rg)
        {
            var aiClient = Container.Resolve<ApplicationInsightsManagementClient>();
            aiClient.SubscriptionId = AzClient.SubscriptionId;
            return await aiClient.Components.CreateAIInstanceIfNotExists($"{rg.Name}".ToLowerInvariant(), rg.Name);
        }

        private async Task<IServiceBusNamespace> SetupServiceBusNamespace(IResourceGroup rg)
        {
            return await AzClient.ServiceBusNamespaces.Define($"esw-{rg.Name}".ToLowerInvariant())
                .WithRegion(Region.EuropeWest).WithExistingResourceGroup(rg).CreateAsync();
        }

        // ReSharper disable once InconsistentNaming
        private async Task<ICosmosDBAccount> SetupCosmosDBInstance(IResourceGroup rg)
        {
            return await AzClient.CosmosDBAccounts.Define($"esw-{rg.Name}".ToLowerInvariant())
                .WithRegion(Region.EuropeWest)
                .WithExistingResourceGroup(rg).WithDataModelSql().WithEventualConsistency()
                .WithWriteReplication(Region.EuropeWest)
                .CreateAsync();
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetupNetwork(IResourceGroup rg)
        {
            var camelRgName = rg.Name.ToCamelCase();
            const string testApi1 = "TestAPI1";
            const string testApi2 = "TestAPI2";

            WeIpAddress = await AzClient.PublicIPAddresses.Define($"{camelRgName}.wepip").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg).WithStaticIP().WithSku(PublicIPSkuType.Basic).CreateAsync();

            AzClient.LoadBalancers.Define($"{camelRgName}.welb").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg)
                .DefineLoadBalancingRule(testApi1)
                .WithProtocol(TransportProtocol.Tcp)
                .FromExistingPublicIPAddress(WeIpAddress)
                .FromFrontendPort(1111)
                .ToBackend(testApi1)
                .WithProbe($"{testApi1}-probe")
                .Attach()
                .DefineLoadBalancingRule(testApi2)
                .WithProtocol(TransportProtocol.Tcp)
                .FromExistingPublicIPAddress(WeIpAddress)
                .FromFrontendPort(1112)
                .ToBackend(testApi2)
                .WithProbe($"{testApi2}-probe")
                .Attach()
                .WithSku(LoadBalancerSkuType.Basic)
                .DefineHttpProbe($"{testApi1}-probe")
                .WithRequestPath("/Probe")
                .WithPort(1111)
                .Attach()
                .DefineHttpProbe($"{testApi2}-probe")
                .WithRequestPath("/Probe")
                .WithPort(1112)
                .Attach()
                .Create();

            EusIpAddress = await AzClient.PublicIPAddresses.Define($"{camelRgName}.euspip")
                .WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg).WithStaticIP().WithSku(PublicIPSkuType.Basic).CreateAsync();

            AzClient.LoadBalancers.Define($"{camelRgName}.euslb").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg)
                .DefineLoadBalancingRule(testApi1)
                .WithProtocol(TransportProtocol.Tcp)
                .FromExistingPublicIPAddress(EusIpAddress)
                .FromFrontendPort(2222)
                .ToBackend(testApi1)
                .WithProbe($"{testApi1}-probe")
                .Attach()
                .DefineLoadBalancingRule(testApi2)
                .WithProtocol(TransportProtocol.Tcp)
                .FromExistingPublicIPAddress(EusIpAddress)
                .FromFrontendPort(2223)
                .ToBackend(testApi2)
                .WithProbe($"{testApi2}-probe")
                .Attach()
                .WithSku(LoadBalancerSkuType.Basic)
                .DefineHttpProbe($"{testApi1}-probe")
                .WithRequestPath("/Probe")
                .WithPort(2222)
                .Attach()
                .DefineHttpProbe($"{testApi2}-probe")
                .WithRequestPath("/Probe")
                .WithPort(2223)
                .Attach()
                .Create();

            SaIpAdress = await AzClient.PublicIPAddresses.Define($"{camelRgName}.sapip")
                .WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg).WithStaticIP().WithSku(PublicIPSkuType.Basic).CreateAsync();

            AzClient.LoadBalancers.Define($"{camelRgName}.salb").WithRegion(Region.EuropeNorth)
                .WithExistingResourceGroup(rg)
                .DefineLoadBalancingRule(testApi1)
                .WithProtocol(TransportProtocol.Tcp)
                .FromExistingPublicIPAddress(SaIpAdress)
                .FromFrontendPort(3333)
                .ToBackend(testApi1)
                .WithProbe($"{testApi1}-probe")
                .Attach()
                .DefineLoadBalancingRule(testApi2)
                .WithProtocol(TransportProtocol.Tcp)
                .FromExistingPublicIPAddress(SaIpAdress)
                .FromFrontendPort(3334)
                .ToBackend(testApi2)
                .WithProbe($"{testApi2}-probe")
                .Attach()
                .WithSku(LoadBalancerSkuType.Basic)
                .DefineHttpProbe($"{testApi1}-probe")
                .WithRequestPath("/Probe")
                .WithPort(3222)
                .Attach()
                .DefineHttpProbe($"{testApi2}-probe")
                .WithRequestPath("/Probe")
                .WithPort(3223)
                .Attach()
                .Create();

            await AzClient.DnsZones.Define($"{camelRgName}.private".ToLowerInvariant()).WithExistingResourceGroup(rg)
                .DefineCNameRecordSet(testApi1).WithAlias($"{camelRgName}.frontdoor.net").Attach() // test api 1 - Global record
                .DefineCNameRecordSet(testApi2).WithAlias($"{camelRgName}.frontdoor.net").Attach() // test api 2 - Global record
                .DefineARecordSet($"{testApi1}-we").WithIPv4Address(WeIpAddress.IPAddress)
                .Attach() //test api 1 LB - WE
                .DefineARecordSet($"{testApi1}-eus").WithIPv4Address(EusIpAddress.IPAddress)
                .Attach() //test api 1 LB - EUS
                .DefineARecordSet($"{testApi1}-sa").WithIPv4Address(SaIpAdress.IPAddress)
                .Attach() //test api 1 LB - SA
                .DefineARecordSet($"{testApi2}-we").WithIPv4Address(WeIpAddress.IPAddress)
                .Attach() //test api 2 LB - WE
                .DefineARecordSet($"{testApi2}-eus").WithIPv4Address(EusIpAddress.IPAddress)
                .Attach() //test api 2 LB - EUS
                .DefineARecordSet($"{testApi2}-sa").WithIPv4Address(SaIpAdress.IPAddress)
                .Attach() //test api 2 LB - SA
                .CreateAsync();
        }

        protected override async Task DeleteResources()
        {
            if (AzClient != null)
            {
                foreach (var region in RegionHelper.DeploymentRegionsToList())
                {
                    await AzClient.ResourceGroups.DeleteRGIfExists(
                        GetRegionalResourceGroupName(region.ToRegionCode()));

                    await AzClient.ResourceGroups.DeleteRGIfExists(
                        GetRegionalPlatformResourceGroupName(region.ToRegionCode()));
                }

                await Task.WhenAll(
                    AzClient.ResourceGroups.DeleteRGIfExists(DomainAResourceGroupName),
                    AzClient.ResourceGroups.DeleteRGIfExists(DomainBResourceGroupName));
            }
        }
    }

    [CollectionDefinition(nameof(AzScanCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class AzScanCLITestsL2Collection : ICollectionFixture<AzScanCLITestsL2Fixture>
    { }
}
