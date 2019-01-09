using System.IO;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    // ReSharper disable once InconsistentNaming
    public class AutorestCLITests : CLIInvokingTestsBase
    {
        [Fact, IsLayer1]
        public void CheckOptionsForGenerateProjectFile()
        {
            var console = GetStandardOutput("autorest", "generateProjectFile", "--help");

            console.Should().NotBeNullOrWhiteSpace();
            console.Should().ContainAll("-s", "--swagger", "-o", "--output", "-t", "--tfm");
        }

        [Fact, IsLayer1]
        public void GenerateProjectFileFlow_LongNames()
        {
            var output = Path.GetTempPath();
            DeleteTestFiles(output, "SwaggerPetStore.csproj");
            GetStandardOutput("autorest", "generateProjectFile", "--swagger",
                "https://raw.githubusercontent.com/Azure/autorest/master/Samples/1a-code-generation-minimal/pétstöre.json",
                "--output", output);

            File.Exists(Path.Combine(output, "SwaggerPetStore.csproj")).Should().BeTrue();
        }

        [Fact, IsLayer1]
        public void GenerateProjectFileFlow_ShortNames()
        {
            var output = Path.GetTempPath();
            DeleteTestFiles(output, "SwaggerPetStore.csproj");
            GetStandardOutput("autorest", "generateProjectFile", "-s",
                "https://raw.githubusercontent.com/Azure/autorest/master/Samples/1a-code-generation-minimal/pétstöre.json",
                "-o", output);

            File.Exists(Path.Combine(output, "SwaggerPetStore.csproj")).Should().BeTrue();
        }
    }
}
