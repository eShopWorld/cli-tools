using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
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
using Microsoft.Rest;

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
            var vault =  await AzClient.Vaults
                .Define($"esw-{rg.Name}")
                    .WithRegion(rg.RegionName)
                .WithExistingResourceGroup(rg)
                .DefineAccessPolicy()
                    .ForObjectId(_testConfig.TargetIdentityObjectId) //so that CLI can write and test                
                    .AllowSecretPermissions(SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set, SecretPermissions.Delete, SecretPermissions.Recover)
                .Attach()
                .CreateAsync();

            //enable soft-delete
            var credentials = Container.Resolve<TokenCredentials>();
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"https://management.azure.com/subscriptions/{EswDevOpsSdk.SierraIntegrationSubscriptionId}/resourceGroups/{rg.Name}/providers/Microsoft.KeyVault/vaults/{vault.Name}?api-version=2018-02-14")
            {
                Content = new StringContent("{\"properties\":{\"enableSoftDelete\":true}}", Encoding.UTF8,
                    "application/json")
            };

            await credentials.ProcessHttpRequestAsync(request, CancellationToken.None);

            (await httpClient.SendAsync(request)).EnsureSuccessStatusCode();
            return vault;
        }

        internal async Task<IList<SecretBundle>> LoadAllKeyVaultSecrets(string regionCode)
        {
            return await _keyVaultClient.GetAllSecrets(GetRegionalKVName(regionCode));
        }

        internal async Task<IList<SecretItem>> LoadAllDisabledKeyVaultSecrets( string regionCode)
        {
            return await _keyVaultClient.GetDisabledSecrets(GetRegionalKVName(regionCode));
        }

        internal async Task<IList<DeletedSecretItem>> LoadAllDeletedSecrets(string regionCode)
        {
            return await _keyVaultClient.GetDeletedSecrets(GetRegionalKVName(regionCode));
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
            var kvName = GetRegionalKVName(regionCode);
            try
            {
                if (await _keyVaultClient.GetDeletedSecretWithHttpMessages(kvName, name) != null)
                {
                    await _keyVaultClient.RecoverSecret(kvName, name);
                }
            }
            catch (KeyVaultErrorException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
            }

            await _keyVaultClient.SetKeyVaultSecretAsync(kvName, name, value);
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
        }

        protected abstract Task DeleteResources();
        
    }
}
