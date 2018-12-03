using System;
using System.IO;
using System.Reflection;
using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Commands.AutoRest.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EshopWorld.Tools.Unit.Tests
{    
    public class RenderProjectFileCommandIntTests
    {        
        private readonly IServiceProvider serviceProvider;

        public RenderProjectFileCommandIntTests()
        {
            var services = new ServiceCollection();
            AutoRestCommand.Run.ConfigureDefaultServices(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));       
            serviceProvider = services.BuildServiceProvider();                      
        }

        [Fact, Trait("Category", "Integration")]
        public void RenderView_GeneratesExpectedContent()
        {
            
            var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using (serviceScope.CreateScope())
            {
                var sut = serviceProvider.GetRequiredService<RenderProjectFileInternalCommand>();

                //generate project file                
                var output = sut.RenderViewToString(new ProjectFileViewModel{ProjectName = "test project", TFMs = new []{ "testTFM1", "testTFM2"}, Version = "1.2.3.4"}, "Views\\TestTemplate.cshtml");
                output.Should().Be("lorem ipsum tfm=testTFM1,testTFM2 version=1.2.3.4 projectName=test project");
            }
        }
    }
}
