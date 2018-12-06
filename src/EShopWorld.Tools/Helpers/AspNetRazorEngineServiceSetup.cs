using System.Diagnostics;
using System.IO;
using System.Reflection;
using EShopWorld.Tools.Base;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;

namespace EShopWorld.Tools.Helpers
{
    public static class AspNetRazorEngineServiceSetup
    {
        /// <summary>
        /// sets up all necessary dependencies for asp.net razor engine
        /// </summary>
        /// <typeparam name="C">underlying command that runs razor</typeparam>
        /// <param name="services">service collection</param>
        /// <param name="customApplicationBasePath">base path for probing</param>
        public static void ConfigureDefaultServices<C>(IServiceCollection services, string customApplicationBasePath) where C : CommandBase
        {
            var applicationEnvironment = PlatformServices.Default.Application;

            services.AddSingleton(applicationEnvironment);
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            IFileProvider fileProvider;
            string applicationName;
            if (!string.IsNullOrEmpty(customApplicationBasePath))
            {
                applicationName = Assembly.GetEntryAssembly().GetName().Name;
                fileProvider = new PhysicalFileProvider(customApplicationBasePath);
            }
            else
            {
                applicationName = Assembly.GetEntryAssembly().GetName().Name;
                fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            }

            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment
            {
                ApplicationName = applicationName,
                WebRootFileProvider = fileProvider,
            });

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(fileProvider);
            });

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddLogging();
            services.AddMvc();
            services.AddTransient<C>();
        }
    }
}
