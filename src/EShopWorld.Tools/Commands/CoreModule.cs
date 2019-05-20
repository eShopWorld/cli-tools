using System.IO;
using System.Reflection;
using Autofac;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using EShopWorld.Tools.Common;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Kusto;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ServiceFabric.Fluent;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;
using Module = Autofac.Module;

namespace EShopWorld.Tools.Commands
{
    /// <summary>
    /// azure level component registration into DI
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var atp = new AzureServiceTokenProvider();
            var token = atp.GetAccessTokenAsync("https://management.core.windows.net/").Result;
            var tokenCredentials = new TokenCredentials(token);

            var client = RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty, AzureEnvironment.AzureGlobalCloud))
                .Build();

            builder.RegisterInstance(client);
            builder.Register(c=> Azure.Authenticate(client, "3e14278f-8366-4dfd-bcc8-7e4e9d57f2c1"));       
            builder.Register(c=> new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(atp.KeyVaultTokenCallback))); //cannot use token from above - different resource     
            builder.RegisterType<PathService>().SingleInstance();

            builder.RegisterInstance(new ApplicationInsightsManagementClient(tokenCredentials));
            builder.RegisterInstance(new KustoManagementClient(tokenCredentials));
            builder.RegisterInstance(new ServiceFabricManagementClient(tokenCredentials));

            var configBuilder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            configBuilder.AddJsonFile("appsettings.json");

            var config = configBuilder.Build();
            builder.RegisterInstance(config);
            builder.RegisterInstance<IBigBrother>(new BigBrother(config["AIKey"], config["InternalAIKey"]));
        }
    }
}
