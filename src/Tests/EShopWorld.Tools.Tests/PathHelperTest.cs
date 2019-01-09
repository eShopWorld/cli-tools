using System.IO;
using System.Linq;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class PathHelperTest
    {
        public class CreateRelativePath
        {
            [Fact, IsUnit]
            public void Test_SecondBiggerThanFirst()
            {
                var folders = new[]
                {
                    "SomeFolder",
                    "AnotherFolder",
                    "AndYetAnotherFolder"
                };

                var firstPath = Path.Combine("C:\\", folders.First());
                var secondPath = Path.Combine(new[] { firstPath }.Concat(folders.Skip(1).Take(2)).ToArray());

                var result = new PathHelper().CreateRelativePath(firstPath, secondPath);

                result.Should().BeEquivalentTo(Path.Combine(new[] { "." }.Concat(folders.Skip(1).Take(2)).ToArray()));
            }

            [Fact, IsUnit]
            public void Test_FirstBiggerThanSecond()
            {
                var folders = new[]
                { 
                    "SomeFolder",
                    "AnotherFolder",
                    "AndYetAnotherFolder"
                };

                var secondPath = Path.Combine("C:\\", folders.First());
                var firstPath = Path.Combine(new[] { secondPath }.Concat(folders.Skip(1).Take(2)).ToArray());

                var result = new PathHelper().CreateRelativePath(firstPath, secondPath);

                result.Should().BeEquivalentTo(Path.Combine(new[] { "..", ".." }.Concat(folders.Take(1)).ToArray()));
            }

            [Fact, IsUnit]
            public void Test_FirstEqualsSecond()
            {
                var folders = new[]
                {
                    "SomeFolder",
                    "AnotherFolder",
                    "AndYetAnotherFolder"
                };

                var path = Path.Combine(new[] { "C:\\" }.Concat(folders.Take(3)).ToArray());

                var result = new PathHelper().CreateRelativePath(path, path);

                result.Should().BeEquivalentTo("");
            }

            [Fact, IsUnit]
            public void Ensure_UnrelativePaths_ReturnNull()
            {
                var folders = new[]
                {
                    "SomeFolder",
                    "AnotherFolder",
                    "AndYetAnotherFolder"
                };

                var firstPath = Path.Combine(new[] { "C:\\" }.Concat(folders.Skip(2).Take(1)).ToArray());
                var secondPath = Path.Combine(new[] { "D:\\" }.Concat(folders.Skip(1).Take(1)).ToArray());

                var result = new PathHelper().CreateRelativePath(firstPath, secondPath);

                result.Should().BeNull();
            }

            [Fact, IsUnit]
            public void Ensure_NonFileSchemes_ReturnNull()
            {
                var folders = new[]
                {
                    "SomeFolder",
                    "AnotherFolder",
                    "AndYetAnotherFolder"
                };

                var firstPath = Path.Combine(new[] { "C:\\" }.Concat(folders.Skip(2).Take(1)).ToArray());
                const string secondPath = "http://www.google.com";

                var result = new PathHelper().CreateRelativePath(firstPath, secondPath);

                result.Should().BeNull();
            }
        }

        public class EnforceSameFolders
        {
            [Fact, IsUnit]
            public void Test_WithOneLevelFolder()
            {
                const string extraFolder = "FolderTwo";
                const string sourceFolder = @"C:\FolderOne";
                const string targetFolder = @"C:\Target";
                var file = Path.Combine(sourceFolder, extraFolder, "file.txt");

                var helperMock = new Mock<PathHelper> {CallBase = true};
                helperMock.Setup(x => x.CreateDirectory(It.IsAny<string>())).Verifiable();

                helperMock.Object.EnforceSameFolders(sourceFolder, targetFolder, file);

                helperMock.Verify(x => x.CreateDirectory(Path.Combine(targetFolder, ".", extraFolder)));
            }

            [Fact, IsUnit]
            public void Test_WithTwoLevelsFolder()
            {
                const string extraFolderOne = "FolderTwo";
                const string extraFolderTwo = "FolderThree";
                const string sourceFolder = @"C:\FolderOne";
                const string targetFolder = @"C:\Target";
                var file = Path.Combine(sourceFolder, extraFolderOne, extraFolderTwo, "file.txt");

                var helperMock = new Mock<PathHelper> { CallBase = true };
                helperMock.Setup(x => x.CreateDirectory(It.IsAny<string>())).Verifiable();

                helperMock.Object.EnforceSameFolders(sourceFolder, targetFolder, file);

                helperMock.Verify(x => x.CreateDirectory(Path.Combine(targetFolder, ".", extraFolderOne, extraFolderTwo)));
            }

            [Fact, IsUnit]
            public void Test_WithTheSameFolder()
            {
                const string sourceFolder = @"C:\FolderOne\FolderTwo";
                var file = Path.Combine(sourceFolder, "file.txt");

                var helperMock = new Mock<PathHelper> { CallBase = true };
                helperMock.Setup(x => x.CreateDirectory(It.IsAny<string>())).Verifiable();

                helperMock.Object.EnforceSameFolders(sourceFolder, sourceFolder, file);

                helperMock.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Never);
            }
        }
    }
}
