using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once InconsistentNaming
    public abstract class AzScanCLITestsL2FixtureBase : IDisposable
    {
        private readonly string _environment;
        protected IAzure AzClient;
        protected static readonly IContainer Container; //there is an issue with BB and multiple instances being set up concurrently - there is probably no need to 
        private KeyVaultClient _keyVaultClient;
        private KeyVaultManagementClient _keyVaultManagementClient;
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

            builder.Register(ctx=> new KeyVaultManagementClient(ctx.Resolve<RestClient>()) {SubscriptionId =  EswDevOpsSdk.SierraIntegrationSubscriptionId});

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
            _keyVaultManagementClient = Container.Resolve<KeyVaultManagementClient>();

            var testConfigRoot = EswDevOpsSdk.BuildConfiguration();

            _testConfig = new TestConfig();
            testConfigRoot.GetSection("CLIToolingIntTest").Bind(_testConfig);

            CreateResources().GetAwaiter().GetResult();
        }

        protected abstract Task CreateResources();


        // ReSharper disable once InconsistentNaming
        protected async Task<VaultInner> SetupOutputKV(IResourceGroup rg)
        {
            var vaultName = $"esw-{rg.Name}";
            var exists = true;
            var tenantGuid = Guid.Parse("3e14278f-8366-4dfd-bcc8-7e4e9d57f2c1");

            try
            {
                await _keyVaultManagementClient.Vaults.GetDeletedWithHttpMessagesAsync(vaultName, rg.RegionName);
            }
            catch (CloudException e) when(e.Response.StatusCode==HttpStatusCode.NotFound)
            {
                exists = false;
            }

            var vault = await _keyVaultManagementClient.Vaults.CreateOrUpdateAsync(rg.Name, vaultName,
                new VaultCreateOrUpdateParametersInner
                {
                    Properties = new VaultProperties(tenantGuid, new Sku(SkuName.Standard),
                        new List<AccessPolicyEntry>()
                        {
                            new AccessPolicyEntry(tenantGuid, _testConfig.TargetIdentityObjectId,
                                new Permissions(secrets: new List<string>()
                                {
                                    SecretPermissions.Get.Value,
                                    SecretPermissions.List.Value,
                                    SecretPermissions.Set.Value,
                                    SecretPermissions.Delete.Value,
                                    SecretPermissions.Recover.Value
                                }))
                        }, enableSoftDelete: true, createMode: exists ? CreateMode.Recover: CreateMode.Default), Location = rg.RegionName
                });

            return vault;
        }

        internal async Task<IList<SecretBundle>> LoadAllKeyVaultSecrets(string regionCode)
        {
            return await _keyVaultClient.GetAllSecrets(GetRegionalKVName(regionCode));
        }

        internal async Task<IList<DeletedSecretItem>> LoadAllDeletedSecrets(string regionCode)
        {
            return await _keyVaultClient.GetDeletedSecrets(GetRegionalKVName(regionCode));
        }

        internal async Task DeleteAllSecretsAcrossRegions()
        {
            await Task.WhenAll(RegionHelper.DeploymentRegionsToList()
                .Select(r => _keyVaultClient.DeleteAllSecrets(GetRegionalKVName(r.ToRegionCode()))));
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
