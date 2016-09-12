using System;
using Esw.DotNetCli.Resx2Json;
using FluentAssertions;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
public class PathHelperTest
{
    public class CreateDirectory
    {
        [Fact, Trait("Category", "Unit")]
        public void Test_WithValidPath()
        {
            var targetFolder = @"C:\" + Guid.NewGuid().ToString().Substring(0, 8);

            var pathMock = new Mock<PathHelper>();
            pathMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
                    .Verifiable();

            pathMock.Object.CreateIfDoesntExist(targetFolder);

            pathMock.Verify(x => x.CreateDirectory(targetFolder));
        }

        [Fact, Trait("Category", "Unit")]
        public void Test_WithExistingPath()
        {
            var targetFolder = @"C:\Windows";
            var pathMock = new Mock<PathHelper>();
            pathMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
                    .Verifiable();

            pathMock.Object.CreateIfDoesntExist(targetFolder);

            pathMock.Verify(x => x.CreateDirectory(targetFolder), Times.Never);
        }

        [Fact, Trait("Category", "Unit")]
        public void Test_WithInvalidPath()
        {
            const string targetFolder = @"C:\?!";
            var pathMock = new Mock<PathHelper>();

            var action = new Action(() => pathMock.Object.CreateIfDoesntExist(targetFolder));

            action.ShouldThrow<ArgumentException>();
        }
    }
}
