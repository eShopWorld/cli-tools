using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;

namespace EShopWorld.Tools.Commands
{
    public abstract class RazorCommandBase: CommandBase
    {
        protected internal override void ConfigureDI()
        {
            var applicationEnvironment = PlatformServices.Default.Application;

            ServiceCollection.AddSingleton(applicationEnvironment);
            ServiceCollection.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            var applicationName = Assembly.GetEntryAssembly().GetName().Name;
            IFileProvider fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
           
            ServiceCollection.AddSingleton<IHostingEnvironment>(new HostingEnvironment
            {
                ApplicationName = applicationName,
                WebRootFileProvider = fileProvider,
            });

            ServiceCollection.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(fileProvider);
            });

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            ServiceCollection.AddSingleton<DiagnosticSource>(diagnosticSource);
            ServiceCollection.AddLogging();
            ServiceCollection.AddMvc();
        }

    }
}
