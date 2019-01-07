﻿using Autofac;
using Eshopworld.Core;
using Eshopworld.DevOps;
using Eshopworld.Telemetry;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;

namespace EShopWorld.Tools.Commands
{
    /// <summary>
    /// azure level component registration into DI
    /// </summary>
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var atp = new AzureServiceTokenProvider();
            var token = atp.GetAccessTokenAsync("https://management.core.windows.net/", string.Empty).Result;
            var tokenCredentials = new TokenCredentials(token);

            var client = RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty, AzureEnvironment.AzureGlobalCloud))
                .Build();

            builder.RegisterInstance(client);
            builder.Register(c=> Azure.Authenticate(client, null));       
            builder.Register(c=> new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(atp.KeyVaultTokenCallback))); //cannot use token from above - different resource     

            var config = EswDevOpsSdk.BuildConfiguration();
            builder.RegisterInstance(config).SingleInstance();
            builder.RegisterInstance<IBigBrother>(new BigBrother(config["AIKey"], config["InternalAIKey"])).SingleInstance();
        }
    }
}
