using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Commands.AutoRest.Models;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EshopWorld.Tools.Unit.Tests
{    
    public class AutoRestProjectFileCommandIntTests
    {        

        [Fact, IsIntegration]
        [Trait("Command", "autorest")]
        [Trait("SubCommand ", "generateClient")]
        public void RenderView_GeneratesExpectedContent()
        {
            var command = new AutoRestCommand.GenerateProjectFileCommand();
            command.ConfigureDI(Mock.Of<IConsole>());
            var serviceProvider = command.ServiceCollection.BuildServiceProvider();

            var sut = serviceProvider.GetRequiredService<RenderProjectFileInternalCommand>();

            //generate project file                
            var output = sut.RenderViewToString(new ProjectFileViewModel{ProjectName = "test project", TFMs = new []{ "testTFM1", "testTFM2"}, Version = "1.2.3.4"}, "Views//TestAutorestProjectFileTemplate.cshtml");
            output.Should().Be("lorem ipsum tfm=testTFM1,testTFM2 version=1.2.3.4 projectName=test project");
        }
    }
}
