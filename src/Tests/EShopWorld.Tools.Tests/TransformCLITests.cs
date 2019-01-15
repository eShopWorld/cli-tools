using System;
using System.IO;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// L3 CLI tests for transform command
    /// </summary>
    public class TransformCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer1]
        public void CheckOptions()
        {
            var content = GetStandardOutput("transform", "resx2json", "-h");
            content.Should().ContainAll("-r", "--resx-project", "-j", "--json-project");
        }

        [Fact, IsLayer1]
        public void Resx2JsonFlow_LongNames()
        {
            var output = Path.GetTempPath();
            var resxPath = Path.Combine(AppContext.BaseDirectory, "data");
            DeleteTestFiles(resxPath, "test.en.json");
            GetStandardOutput("transform", "resx2json", "--resx-project", resxPath, "--json-project", output);
            File.Exists(Path.Combine(output, "test.en.json")).Should().BeTrue();
        }

        [Fact, IsLayer1]
        public void Resx2JsonFlow_ShortNames()
        {
            var output = Path.GetTempPath();
            var resxPath = Path.Combine(AppContext.BaseDirectory, "data");
            DeleteTestFiles(resxPath, "test.en.json");
            GetStandardOutput("transform", "resx2json", "-r", resxPath, "-j", output);
            File.Exists(Path.Combine(output, "test.en.json")).Should().BeTrue();
        }
    }
}
