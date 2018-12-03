using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EShopWorld.Tools.Commands.AutoRest;
using EShopWorld.Tools.Commands.AutoRest.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace EshopWorld.Tools.Unit.Tests
{
    public class RenderProjectFileCommandUnitTests
    {
        private RenderProjectFileInternalCommand sut;
        private Mock<IRazorViewEngine> mockViewEngine;
        private Mock<IServiceProvider> mockServiceProvider;
        private Mock<ITempDataProvider> mockTempDataProvider;

        public RenderProjectFileCommandUnitTests()
        {
            mockViewEngine = new Mock<IRazorViewEngine>();
            mockServiceProvider = new Mock<IServiceProvider>();
            mockTempDataProvider = new Mock<ITempDataProvider>();
            sut = new RenderProjectFileInternalCommand(mockViewEngine.Object, mockTempDataProvider.Object, mockServiceProvider.Object);
        }

        [Fact, Trait("Category", "Unit")]
        public void RenderViewToString_FindsView_Success()
        {
            //arrange         
            var view = new Mock<IView>();
            mockViewEngine.Setup(i => i.GetView(null, It.IsAny<string>(), true))
                .Returns(ViewEngineResult.Found("test", view.Object)).Verifiable();

            //act
            sut.RenderViewToString(new ProjectFileViewModel(), "blah");

            //assert
            mockViewEngine.Verify();
        }

        [Fact, Trait("Category", "Unit")]
        public void RenderViewToString_NonexistentView_ThrowsException()
        {
            //arrange
            mockViewEngine.Setup(i => i.GetView(null, It.IsAny<string>(), true)).Returns(ViewEngineResult.NotFound("blah", new List<string>()));

            //act+assert
            Assert.Throws<InvalidOperationException>(() => sut.RenderViewToString(new ProjectFileViewModel(), "blah"));
        }

        [Fact, Trait("Category", "Unit")]
        public void RenderViewToString_CallsViewRender_Success()
        {
            //arrange
            var viewMock = new Mock<IView>();
            mockViewEngine.Setup(i => i.GetView(null, It.IsAny<string>(), true))
                .Returns(ViewEngineResult.Found("test", viewMock.Object)).Verifiable();

            viewMock.Setup(i => i.RenderAsync(It.IsAny<ViewContext>())).Returns(Task.FromResult("")).Verifiable();

            //act
            var output = sut.RenderViewToString(new ProjectFileViewModel(), "blah");
            
            //assert
            viewMock.Verify();
        }
    }
}
