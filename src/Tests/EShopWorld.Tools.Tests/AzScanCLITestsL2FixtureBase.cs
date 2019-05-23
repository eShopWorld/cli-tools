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
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Configuration;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once InconsistentNaming
    public abstract class AzScanCLITestsL2FixtureBase : IDisposable
    {
        private readonly string _environment;
        protected IAzure AzClient;
        protected static readonly IContainer Container; //there is an issue with BB and multiple instances being set up concurrently - there is probably no need to 
        private KeyVaultClient _keyVaultClient;
        private TestConfig _testConfig;

        private const string OutputKeyVaultNameFormat = "esw-{0}-{1}-{2}";
        internal const string TestDomain = "a";

        static  AzScanCLITestsL2FixtureBase()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(Program).Assembly);
            builder.Register((ctx) => ctx.Resolve<Azure.IAuthenticated>()
                    .WithSubscription(EswDevOpsSdk.SierraIntegrationSubscriptionId))
                .SingleInstance();

            Container = builder.Build();
        }

        protected AzScanCLITestsL2FixtureBase(string environment)
        {
            _environment = environment;
            SetupFixture();
        }

        private void SetupFixture()
        {
            AzClient = Container.Resolve<IAzure>();

            _keyVaultClient = Container.Resolve<KeyVaultClient>();
            var testConfigRoot = EswDevOpsSdk.BuildConfiguration();

            _testConfig = new TestConfig();
            testConfigRoot.GetSection("CLIToolingIntTest").Bind(_testConfig);

            CreateResources().GetAwaiter().GetResult();
        }

        protected abstract Task CreateResources();


        // ReSharper disable once InconsistentNaming
        protected async Task<IVault> SetupOutputKV(IResourceGroup rg)
        {
            return await AzClient.Vaults
                .Define($"esw-{rg.Name}")
                    .WithRegion(rg.RegionName)
                .WithExistingResourceGroup(rg)
                .DefineAccessPolicy()
                    .ForObjectId(_testConfig.TargetIdentityObjectId) //so that CLI can write and test                
                    .AllowSecretPermissions(SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set, SecretPermissions.Delete, SecretPermissions.Recover)
                .Attach()
                .CreateAsync();
        }



        internal Task<IList<SecretBundle>> LoadAllKeyVaultSecretsAsync([NotNull] string regionCode)
        {
            return _keyVaultClient.GetAllSecrets(GetRegionalKVName(regionCode));
        }

        internal Task<IList<SecretItem>> LoadAllDisabledKeyVaultSecretsAsync([NotNull] string regionCode)
        {
            return _keyVaultClient.GetDisabledSecrets(GetRegionalKVName(regionCode));
        }

        internal async Task DeleteAllSecretsAcrossRegions()
        {
            foreach (var region in RegionHelper.DeploymentRegionsToList())
            {
                await _keyVaultClient.DeleteAllSecrets(GetRegionalKVName(region.ToRegionCode()));
            }
        }

        internal async Task SetSecret(string regionCode, string name, string value)
        {
            await _keyVaultClient.SetKeyVaultSecretAsync(GetRegionalKVName(regionCode), name, value);
        }

        // ReSharper disable once InconsistentNaming
        private string GetRegionalKVName([NotNull] string regionCode)
        {
            return string.Format(OutputKeyVaultNameFormat, TestDomain, _environment, regionCode.ToLowerInvariant());
        }

        protected string GetRegionalResourceGroupName([NotNull] string regionCode)
        {
            return $"{TestDomain}-{_environment}-{regionCode}".ToLowerInvariant();
        }

        public void Dispose()
        {
            DeleteResources().GetAwaiter().GetResult();
            Container.Dispose();
        }

        protected abstract Task DeleteResources();
        
    }
}
