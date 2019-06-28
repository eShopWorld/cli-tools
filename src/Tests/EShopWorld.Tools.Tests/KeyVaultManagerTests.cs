using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using EShopWorld.Tools;
using EShopWorld.Tools.Commands.AzScan;
using FluentAssertions;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Fluent;
using EShopWorld.Tools.Common;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class KeyVaultManagerTests
    {
        /// <summary>
        /// test bed used to diagnose "random" exception thrown by <see cref="Microsoft.Rest.RetryDelegatingHandler"/> when running concurrent requests
        ///
        /// https://github.com/Azure/azure-sdk-for-net/issues/3224
        /// </summary>
        /// <returns>task</returns>
        [Fact, IsDev]
        public async Task DeleteObjectDisposeTest()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(Program).Assembly);
            builder.Register((ctx) => ctx.Resolve<Azure.IAuthenticated>()
                    .WithSubscription(EswDevOpsSdk.SierraIntegrationSubscriptionId))
                .SingleInstance();

            var container = builder.Build();
            var config = EswDevOpsSdk.BuildConfiguration();


            var kvName = config["KeyVaultManagerTestKeyVault"];
            var kvUrl = $"https://{kvName}.vault.azure.net/";
            var kvClient = container.Resolve<KeyVaultClient>();

            var tasks = new List<Task>();

            for (var y = 0; y < 10; y++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var testSecretGuid = Guid.NewGuid().ToString().Replace("-", "").ToPascalCase(); //naming convention

                    var testSecret = $"Test--{testSecretGuid}";

                    await kvClient.SetSecretWithHttpMessagesAsync(kvUrl, testSecret, "blah");
                    await kvClient.DeleteSecret(kvName, testSecret);

                    for (var x = 0; x < 10; x++)
                    {
                        
                            await kvClient.RecoverSecret(kvName, testSecret);
                            await kvClient.DeleteSecret(kvName, testSecret);
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        [Fact, IsLayer1]
        public async Task SoftDeleteFlowTest()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(Program).Assembly);
            builder.Register((ctx) => ctx.Resolve<Azure.IAuthenticated>()
                    .WithSubscription(EswDevOpsSdk.SierraIntegrationSubscriptionId))
                .SingleInstance();

            var container = builder.Build();
            var config = EswDevOpsSdk.BuildConfiguration();

            var kvManager = container.Resolve<AzScanKeyVaultManager>();

            var kvName = config["KeyVaultManagerTestKeyVault"];
            var kvUrl = $"https://{kvName}.vault.azure.net/";
            var kvClient = container.Resolve<KeyVaultClient>();

            var obsoleteSecretName = $"Test--{Guid.NewGuid().ToString()}";
            var testSecretGuid = Guid.NewGuid().ToString().Replace("-", "").ToPascalCase(); //naming convention

            var testSecret = $"Test--{testSecretGuid}";

            try
            {

                await kvClient.SetSecretWithHttpMessagesAsync(kvUrl, obsoleteSecretName, "dummy");
                await kvClient.SetSecretWithHttpMessagesAsync(kvUrl, testSecret, "dummy");
                await kvClient.DeleteSecret(kvName, testSecret);

                (await kvClient.GetDeletedSecretWithHttpMessagesAsync(kvUrl, testSecret)).Response.IsSuccessStatusCode
                    .Should().BeTrue();

                await kvManager.AttachKeyVaults(new[] {kvName}, "Test");
                await kvManager.SetKeyVaultSecretAsync(kvName, "Test", testSecretGuid, "", "newDummy");
                await kvManager.DetachKeyVaults();

                (await kvClient.GetSecretAsync(kvUrl, testSecret)).Should().NotBeNull();
                await kvClient.DeleteSecretAsync(kvUrl, testSecret);
                (await Assert.ThrowsAsync<KeyVaultErrorException>(() =>
                        kvClient.GetSecretAsync(kvUrl, obsoleteSecretName)))
                    .Response.StatusCode
                    .Should()
                    .Be(HttpStatusCode.NotFound);
            }
            finally
            {
                await kvClient.DeleteAllSecrets(kvName); //clean up (in case)
            }
        }
    }
}
