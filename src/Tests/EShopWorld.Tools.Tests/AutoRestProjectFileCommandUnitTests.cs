using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Commands.AutoRest.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class AutoRestProjectFileCommandUnitTests
    {
        private RenderProjectFileInternalCommand sut;
        private Mock<IRazorViewEngine> mockViewEngine;

        public AutoRestProjectFileCommandUnitTests()
        {
            mockViewEngine = new Mock<IRazorViewEngine>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockTempDataProvider = new Mock<ITempDataProvider>();
            sut = new RenderProjectFileInternalCommand(mockViewEngine.Object, mockTempDataProvider.Object, mockServiceProvider.Object);
        }

        [Fact, IsLayer0]
        [Trait("Command", "autorest")]
        [Trait("SubCommand ", "generateClient")]
        public void RenderViewToString_FindsView_Success()
        {
            //arrange         
            var view = new Mock<IView>();
            mockViewEngine.Setup(i => i.GetView(null, It.IsAny<string>(), true))
                // ReSharper disable once Mvc.ViewNotResolved
                .Returns(ViewEngineResult.Found("test", view.Object)).Verifiable();

            //act
            sut.RenderViewToString(new ProjectFileViewModel());

            //assert
            mockViewEngine.Verify();
        }

        [Fact, IsLayer0]
        [Trait("Command", "autorest")]
        [Trait("SubCommand ", "generateClient")]
        public void RenderViewToString_NonexistentView_ThrowsException()
        {
            //arrange
            // ReSharper disable once Mvc.ViewNotResolved
            mockViewEngine.Setup(i => i.GetView(null, It.IsAny<string>(), true)).Returns(ViewEngineResult.NotFound("blah", new List<string>()));

            //act+assert
            Assert.Throws<InvalidOperationException>(() => sut.RenderViewToString(new ProjectFileViewModel()));
        }

        [Fact, IsLayer0]
        [Trait("Command", "autorest")]
        [Trait("SubCommand ", "generateClient")]
        public void RenderViewToString_CallsViewRender_Success()
        {
            //arrange
            var viewMock = new Mock<IView>();
            mockViewEngine.Setup(i => i.GetView(null, It.IsAny<string>(), true))
                // ReSharper disable once Mvc.ViewNotResolved
                .Returns(ViewEngineResult.Found("test", viewMock.Object)).Verifiable();

            viewMock.Setup(i => i.RenderAsync(It.IsAny<ViewContext>())).Returns(Task.FromResult("")).Verifiable();

            //act
            sut.RenderViewToString(new ProjectFileViewModel());
            
            //assert
            viewMock.Verify();
        }
    }
}
