using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Eshopworld.Core;
using Eshopworld.Tests.Core;
using EshopWorld.Tools.Tests.data;
using EShopWorld.Tools.Commands.Transform;
using EShopWorld.Tools.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

// ReSharper disable once CheckNamespace
namespace EShopWorld.Tools.Tests
{
    public class Resx2JsonCommandTest
    {
        public class GetJsonPath
        {
            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_WithDefaultCulture()
            {
                const string path = @"C:\SomeFolder\AResource.resx";
                const string outputPath = @"C:\OutFolder\";
                const string expectedJsonFile = "AResource." + TransformBase.JsonDefaultCulture + ".json";

                var cmdMock = new Mock<TransformCommand.Resx2JsonCommand>(Mock.Of<IBigBrother>(), Mock.Of<PathService>()) { CallBase = true };

                var result = cmdMock.Object.GetJsonPath(outputPath, path);

                result.Should().Be(Path.Combine(Path.GetDirectoryName(outputPath), expectedJsonFile));
            }

            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_WithSpecificCulture()
            {
                const string path = @"C:\SomeFolder\AResource.it-it.resx";
                const string outputPath = @"C:\OutFolder\";
                var expectedJsonFile = Path.GetFileNameWithoutExtension(path) + ".json";

                var cmdMock = new Mock<TransformCommand.Resx2JsonCommand> (Mock.Of<IBigBrother>(), Mock.Of<PathService>()) { CallBase = true };

                var result = cmdMock.Object.GetJsonPath(outputPath, path);

                result.Should().Be(Path.Combine(Path.GetDirectoryName(outputPath), expectedJsonFile));
            }
        }

        public class ConvertResx2Json
        {
            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_WithEmbeddedResxResource()
            {
                var resxPath = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf(@"\bin", StringComparison.Ordinal)) + @"\data\test.resx";
                var resx = File.ReadAllText(resxPath);

                var json = new TransformCommand.Resx2JsonCommand(Mock.Of<IBigBrother>(), new PathService())
                {
                    ResxProject = string.Empty,
                    JsonProject = string.Empty
                }.ConvertResx2Json(resx);

                var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                foreach (var key in jsonDict.Keys)
                {
                    var resourceValue = typeof(test).GetMethod($"get_{key}", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                    Assert.Equal(resourceValue, jsonDict[key]);
                }
            }
        }

        public class GetMergedResource
        {
            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_BaseResxDoesNotMerge()
            {
                const string fileContent = "some file content!";
                const string filePath = @"C:\AFolder\AFile.resx";

                var cmdMock = new Mock<TransformCommand.Resx2JsonCommand>(Mock.Of<IBigBrother>(), Mock.Of<PathService>()) { CallBase = true };
                cmdMock.Setup(x => x.ReadText(filePath)).Returns(fileContent);

                var result = cmdMock.Object.GetMergedResource(filePath);

                result.Should().Be(fileContent);
            }

            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_MergeWithWrongPath()
            {
                const string basefileContent = "some file content!";
                const string basefilePath = @"C:\AFolder\AFile.resx";
                const string wrongfileContent = "wrong file content!";
                const string wrongfilePath = @"C:\WrongFolder\AFile.resx";

                var cmdMock = new Mock<TransformCommand.Resx2JsonCommand>(Mock.Of<IBigBrother>(), Mock.Of<PathService>()) { CallBase = true };
                cmdMock.Object.ResourceDictionary = new Dictionary<string, List<string>>
                {
                    { Path.GetFileName(wrongfilePath), new List<string> { wrongfilePath } }
                };

                cmdMock.Setup(x => x.ReadText(basefilePath)).Returns(basefileContent);
                cmdMock.Setup(x => x.ReadText(wrongfilePath)).Returns(wrongfileContent);

                var result = cmdMock.Object.GetMergedResource(basefilePath);

                result.Should().Be(basefileContent);
            }

            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_MergeSingleLevel()
            {
                const string basefileContent = "some file content!";
                const string basefilePath = @"C:\AFolder\AnotherFolder\AFile.resx";
                const string mergefileContent = "merged file content!";
                const string mergefilePath = @"C:\AFolder\AFile.resx";

                var cmdMock = new Mock<TransformCommand.Resx2JsonCommand>(Mock.Of<IBigBrother>(), Mock.Of<PathService>()) { CallBase = true };
                cmdMock.Object.ResourceDictionary = new Dictionary<string, List<string>>
                {
                    { Path.GetFileName(mergefilePath), new List<string> { mergefilePath } }
                };

                cmdMock.Setup(x => x.ReadText(basefilePath)).Returns(basefileContent);
                cmdMock.Setup(x => x.ReadText(mergefilePath)).Returns(mergefileContent);
                cmdMock.Setup(x => x.MergeResx(mergefileContent, basefileContent)).Returns(mergefileContent);
                cmdMock.Setup(x => x.WriteText(It.IsAny<string>(), It.IsAny<string>())).Callback(() => { });

                var result = cmdMock.Object.GetMergedResource(basefilePath);

                result.Should().Be(mergefileContent);
            }

            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_MergeMultipleSources()
            {
                const string basefileContent = "some file content!";
                const string basefilePath = @"C:\AFolder\AnotherFolder\AndAnotherFolder\AFile.resx";
                const string wrongfileContent = "wrong file content!";
                const string wrongfilePath = @"C:\AFolder\AFile.resx";
                const string mergefileContent = "merged file content!";
                const string mergefilePath = @"C:\AFolder\AnotherFolder\AFile.resx";

                var cmdMock = new Mock<TransformCommand.Resx2JsonCommand>(Mock.Of<IBigBrother>(), Mock.Of<PathService>()) { CallBase = true };
                cmdMock.Object.ResourceDictionary = new Dictionary<string, List<string>>
                {
                    { Path.GetFileName(mergefilePath), new List<string> { mergefilePath } }
                };

                cmdMock.Setup(x => x.ReadText(basefilePath)).Returns(basefileContent);
                cmdMock.Setup(x => x.ReadText(mergefilePath)).Returns(mergefileContent);
                cmdMock.Setup(x => x.ReadText(wrongfilePath)).Returns(wrongfileContent);
                cmdMock.Setup(x => x.MergeResx(mergefileContent, basefileContent)).Returns(mergefileContent);
                cmdMock.Setup(x => x.WriteText(It.IsAny<string>(), It.IsAny<string>())).Callback(() => { });

                var result = cmdMock.Object.GetMergedResource(basefilePath);

                result.Should().Be(mergefileContent);
            }
        }

        public class MergeResx
        {
            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_SourceWithMore_ThanTarget()
            {
                const string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name=""CommonResource_One"" xml:space=""preserve"">
    <value>Default_One</value>
  </data>
  <data name=""CommonResource_Three"" xml:space=""preserve"">
    <value>Default_Three</value>
  </data>
  <data name=""CommonResource_Two"" xml:space=""preserve"">
    <value>Default_Two</value>
  </data>
</root>
";

                const string target = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name=""CommonResource_Three"" xml:space=""preserve"">
    <value>Level2_Three</value>
  </data>
</root>
";

                var result = new TransformCommand.Resx2JsonCommand(Mock.Of<IBigBrother>(), new PathService())
                {
                    JsonProject = string.Empty,
                    ResxProject = string.Empty
                }.MergeResx(source, target);

                // because we are calling XElement.ToString() there is no <xml> spec ceremony
                // so it needs to start at the root element and end without trivia
                result.Should().Be(@"<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name=""CommonResource_Three"" xml:space=""preserve"">
    <value>Level2_Three</value>
  </data>
  <data name=""CommonResource_One"" xml:space=""preserve"">
    <value>Default_One</value>
  </data>
  <data name=""CommonResource_Two"" xml:space=""preserve"">
    <value>Default_Two</value>
  </data>
</root>"
                );
            }

            [Fact, IsLayer0]
            [Trait("Command", "Transform")]
            [Trait("SubCommand ", "resx2json")]
            public void Test_SourceWithLess_ThanTarget()
            {
                const string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name=""CommonResource_Three"" xml:space=""preserve"">
    <value>Level2_Three</value>
  </data>
</root>
";

                const string target = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name=""CommonResource_One"" xml:space=""preserve"">
    <value>Default_One</value>
  </data>
  <data name=""CommonResource_Three"" xml:space=""preserve"">
    <value>Default_Three</value>
  </data>
  <data name=""CommonResource_Two"" xml:space=""preserve"">
    <value>Default_Two</value>
  </data>
</root>
";
                var result = new TransformCommand.Resx2JsonCommand(Mock.Of<IBigBrother>(), new PathService())
                {
                    JsonProject = string.Empty,
                    ResxProject = string.Empty
                }.MergeResx(source, target);

                // because we are calling XElement.ToString() there is no <xml> spec ceremony
                // so it needs to start at the root element and end without trivia
                result.Should().Be(@"<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name=""CommonResource_One"" xml:space=""preserve"">
    <value>Default_One</value>
  </data>
  <data name=""CommonResource_Three"" xml:space=""preserve"">
    <value>Default_Three</value>
  </data>
  <data name=""CommonResource_Two"" xml:space=""preserve"">
    <value>Default_Two</value>
  </data>
</root>"
                );
            }
        }

        public class ResxDataComparerTest
        {
            public class EqualsImp
            {
                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithDifferentNames()
                {
                    var element1 = new XElement("data", new XAttribute("name", "1"));
                    var element2 = new XElement("data", new XAttribute("name", "2"));
                    var result = new TransformBase.ResxDataComparer().Equals(element1, element2);

                    result.Should().BeFalse();
                }

                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithSameNames()
                {
                    const string nameValue = "the same name";
                    var element1 = new XElement("data", new XAttribute("name", nameValue));
                    var element2 = new XElement("data", new XAttribute("name", nameValue));
                    var result = new TransformBase.ResxDataComparer().Equals(element1, element2);

                    result.Should().BeTrue();
                }

                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithTwoNulls()
                {
                    var result = new TransformBase.ResxDataComparer().Equals(null, null);

                    result.Should().BeTrue();
                }

                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithFirstNull()
                {
                    var result = new TransformBase.ResxDataComparer().Equals(null, new XElement("something"));

                    result.Should().BeFalse();
                }

                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithSecondNull()
                {
                    var result = new TransformBase.ResxDataComparer().Equals(new XElement("something"), null);

                    result.Should().BeFalse();
                }
            }

            public class GetHashCodeImp
            {
                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithProperXElement()
                {
                    const string nameValue = "some name";
                    var element = new XElement("data", new XAttribute("name", nameValue));

                    var result = new TransformBase.ResxDataComparer().GetHashCode(element);

                    result.Should().Be(nameValue.GetHashCode());
                }

                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithNull()
                {
                    var result = new TransformBase.ResxDataComparer().GetHashCode(null);

                    result.Should().Be(0);
                }

                [Fact, IsLayer0]
                [Trait("Command", "Transform")]
                [Trait("SubCommand ", "resx2json")]
                public void Test_WithElementWithoutName()
                {
                    var element = new XElement("data");

                    var result = new TransformBase.ResxDataComparer().GetHashCode(element);

                    result.Should().Be(0);
                }
            }
        }
    }
}
