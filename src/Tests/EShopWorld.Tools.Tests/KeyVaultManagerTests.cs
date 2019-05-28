using System;
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
using Polly;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class KeyVaultManagerTests
    {
        [Fact,IsLayer1]
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
            var testSecretGuid = Guid.NewGuid().ToString().Replace("-", ""); //naming convention

            var testSecret = $"Test--{testSecretGuid}";

            await kvClient.SetSecretWithHttpMessagesAsync(kvUrl, obsoleteSecretName, "dummy");
            await kvClient.SetSecretWithHttpMessagesAsync(kvUrl, testSecret, "dummy");
            await kvClient.DeleteSecretAsync(kvUrl, testSecret);

            await Policy
                .Handle<KeyVaultErrorException>(s=>s.Response.StatusCode==HttpStatusCode.NotFound)
                .WaitAndRetryForeverAsync(w => new TimeSpan(100))
                .ExecuteAsync(() => kvClient.GetDeletedSecretWithHttpMessagesAsync(kvUrl, testSecret));

            await kvClient.GetDeletedSecretWithHttpMessagesAsync(kvUrl, testSecret);

            await kvManager.AttachKeyVaults(new[] {kvName }, "Test");
            await kvManager.SetKeyVaultSecretAsync(kvName, "Test", testSecretGuid, "", "newDummy");
            await kvManager.DetachKeyVaults();

            (await kvClient.GetSecretAsync(kvUrl, testSecret)).Should().NotBeNull();
            await kvClient.DeleteSecretAsync(kvUrl, testSecret);
            (await Assert.ThrowsAsync<KeyVaultErrorException>(() => kvClient.GetSecretAsync(kvUrl, obsoleteSecretName)))
                .Response.StatusCode
                .Should()
                .Be(HttpStatusCode.NotFound);
            
        }
    }
}
