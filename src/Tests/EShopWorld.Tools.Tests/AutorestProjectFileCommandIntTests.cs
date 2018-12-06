using System;
using System.IO;
using System.Reflection;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Commands.AutoRest.Models;
using EShopWorld.Tools.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EshopWorld.Tools.Unit.Tests
{    
    public class AutoRestProjectFileCommandIntTests
    {        
        private readonly IServiceProvider _serviceProvider;

        public AutoRestProjectFileCommandIntTests()
        {
            var services = new ServiceCollection();
            AspNetRazorEngineServiceSetup.ConfigureDefaultServices<RenderProjectFileInternalCommand>(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));       
            _serviceProvider = services.BuildServiceProvider();                      
        }

        [Fact, IsIntegration]
        [Trait("Command", "autorest")]
        [Trait("SubCommand ", "generateClient")]
        public void RenderView_GeneratesExpectedContent()
        {
            
            var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using (serviceScope.CreateScope())
            {
                var sut = _serviceProvider.GetRequiredService<RenderProjectFileInternalCommand>();

                //generate project file                
                var output = sut.RenderViewToString(new ProjectFileViewModel{ProjectName = "test project", TFMs = new []{ "testTFM1", "testTFM2"}, Version = "1.2.3.4"}, "Views//TestAutorestProjectFileTemplate.cshtml");
                output.Should().Be("lorem ipsum tfm=testTFM1,testTFM2 version=1.2.3.4 projectName=test project");
            }
        }
    }
}
