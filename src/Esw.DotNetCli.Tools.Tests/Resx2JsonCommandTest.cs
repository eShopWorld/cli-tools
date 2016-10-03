using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Esw.DotNetCli.Tools;
using Esw.DotNetCli.Tools.Tests.data;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

// ReSharper disable once CheckNamespace
public class Resx2JsonCommandTest
{
    public class Run
    {
        [Fact, Trait("Category", "Integration")]
        public void TheOneTestToRuleThemAll()
        {
            // #1 clean up before the test

            // #2 run the command to export the json

            // #3 ensure folder structure

            // #4 inspect all the json and compare it to the resx
            // #4.1 use roslyn to emit the assembly with the resources on the integration project
        }
    }

    public class ConvertResx2Json
    {
        [Fact, Trait("Category", "Integration")]
        public void Test_WithEmbebeddedResxResource()
        {
            var resx = File.ReadAllText(@".\data\test.resx");

            var json = new Resx2JsonCommand(string.Empty, string.Empty).ConvertResx2Json(resx);

            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            foreach (var key in jsonDict.Keys)
            {
                var resourceValue = typeof(test).GetMethod($"get_{key}").Invoke(null, null);
                Assert.Equal(resourceValue, jsonDict[key]);
            }
        }
    }

    public class GetMergedResource
    {
        [Fact, Trait("Category", "Unit")]
        public void Test_BaseResxDoesNotMerge()
        {
            const string fileContent = "some file content!";
            const string filePath = @"C:\AFolder\AFile.resx";

            var cmdMock = new Mock<Resx2JsonCommand> {CallBase = true};
            cmdMock.Object.ResourceDictionary = new Dictionary<string, List<string>>();
            cmdMock.Setup(x => x.ReadText(filePath)).Returns(fileContent);

            var result = cmdMock.Object.GetMergedResource(filePath);

            result.Should().Be(fileContent);
        }

        [Fact, Trait("Category", "Unit")]
        public void Test_MergeWithWrongPath()
        {
            const string basefileContent = "some file content!";
            const string basefilePath = @"C:\AFolder\AFile.resx";
            const string wrongfileContent = "wrong file content!";
            const string wrongfilePath = @"C:\WrongFolder\AFile.resx";

            var cmdMock = new Mock<Resx2JsonCommand> { CallBase = true };
            cmdMock.Object.ResourceDictionary = new Dictionary<string, List<string>>
            {
                { Path.GetFileName(wrongfilePath), new List<string> { wrongfilePath } }
            };

            cmdMock.Setup(x => x.ReadText(basefilePath)).Returns(basefileContent);
            cmdMock.Setup(x => x.ReadText(wrongfilePath)).Returns(wrongfileContent);

            var result = cmdMock.Object.GetMergedResource(basefilePath);

            result.Should().Be(basefileContent);
        }

        [Fact, Trait("Category", "Unit")]
        public void Test_MergeSingleLevel()
        {
            const string basefileContent = "some file content!";
            const string basefilePath = @"C:\AFolder\AnotherFolder\AFile.resx";
            const string mergefileContent = "merged file content!";
            const string mergefilePath = @"C:\AFolder\AFile.resx";

            var cmdMock = new Mock<Resx2JsonCommand> { CallBase = true };
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

        [Fact, Trait("Category", "Unit")]
        public void Test_MergeMutipleSources()
        {
            const string basefileContent = "some file content!";
            const string basefilePath = @"C:\AFolder\AnotherFolder\AndAnotherFolder\AFile.resx";
            const string wrongfileContent = "wrong file content!";
            const string wrongfilePath = @"C:\AFolder\AFile.resx";
            const string mergefileContent = "merged file content!";
            const string mergefilePath = @"C:\AFolder\AnotherFolder\AFile.resx";

            var cmdMock = new Mock<Resx2JsonCommand> { CallBase = true };
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
        [Fact, Trait("Category", "Unit")]
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

            var result = new Resx2JsonCommand("", "").MergeResx(source, target);

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

        [Fact, Trait("Category", "Unit")]
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

            var result = new Resx2JsonCommand("", "").MergeResx(source, target);

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
            [Fact, Trait("Category", "Unit")]
            public void Test_WithDifferentNames()
            {
                var element1 = new XElement("data", new XAttribute("name", "1"));
                var element2 = new XElement("data", new XAttribute("name", "2"));
                var result = new Resx2JsonCommand.ResxDataComparer().Equals(element1, element2);

                result.Should().BeFalse();
            }

            [Fact, Trait("Category", "Unit")]
            public void Test_WithSameNames()
            {
                const string nameValue = "the same name";
                var element1 = new XElement("data", new XAttribute("name", nameValue));
                var element2 = new XElement("data", new XAttribute("name", nameValue));
                var result = new Resx2JsonCommand.ResxDataComparer().Equals(element1, element2);

                result.Should().BeTrue();
            }

            [Fact, Trait("Category", "Unit")]
            public void Test_WithTwoNulls()
            {
                var result = new Resx2JsonCommand.ResxDataComparer().Equals(null, null);

                result.Should().BeTrue();
            }

            [Fact, Trait("Category", "Unit")]
            public void Test_WithFirstNull()
            {
                var result = new Resx2JsonCommand.ResxDataComparer().Equals(null, new XElement("something"));

                result.Should().BeFalse();
            }

            [Fact, Trait("Category", "Unit")]
            public void Test_WithSecondNull()
            {
                var result = new Resx2JsonCommand.ResxDataComparer().Equals(new XElement("something"), null);

                result.Should().BeFalse();
            }
        }

        public class GetHashCodeImp
        {
            [Fact, Trait("Category", "Unit")]
            public void Test_WithProperXElement()
            {
                const string nameValue = "some name";
                var element = new XElement("data", new XAttribute("name", nameValue));

                var result = new Resx2JsonCommand.ResxDataComparer().GetHashCode(element);

                result.Should().Be(nameValue.GetHashCode());
            }

            [Fact, Trait("Category", "Unit")]
            public void Test_WithNull()
            {
                var result = new Resx2JsonCommand.ResxDataComparer().GetHashCode(null);

                result.Should().Be(0);
            }

            [Fact, Trait("Category", "Unit")]
            public void Test_WithElementWithoutName()
            {
                var element = new XElement("data");

                var result = new Resx2JsonCommand.ResxDataComparer().GetHashCode(element);

                result.Should().Be(0);
            }
        }
    }
}
