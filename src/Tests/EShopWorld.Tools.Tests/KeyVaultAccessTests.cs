using System.Collections.Concurrent;
using System.Collections.Generic;
using Eshopworld.Tests.Core;
using EShopWorld.Tools.Helpers;
using FluentAssertions;
using Microsoft.Azure.KeyVault.Models;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    public class KeyVaultAccessTests
    {
        [Fact, IsUnit]
        [Trait("Command", "keyvault")]
        [Trait("SubCommand ", "generatePOCOs")]
        public void RunSemanticChecks_NoTags_Fail()
        {
            //arrange
            var secret = new SecretItem("https://rmtestkeyvault.vault.azure.net:443/secrets/Secret1");
            //act
            var ret = KeyVaultExtensions.RunSemanticChecks(new List<SecretItem>(new[] {secret}), "Name");
            //asert
            ret.Should().BeFalse();
        }

        [Fact, IsUnit]
        [Trait("Command", "keyvault")]
        [Trait("SubCommand ", "generatePOCOs")]
        public void RunSemanticChecks_MissingTag_Fail()
        {
            //arrange
            var secret = new SecretItem("https://rmtestkeyvault.vault.azure.net:443/secrets/Secret1", null,
                new ConcurrentDictionary<string, string>(
                    new List<KeyValuePair<string, string>>(new[] {new KeyValuePair<string, string>("Blah", "Blah")})));
            //act
            var ret = KeyVaultExtensions.RunSemanticChecks(new List<SecretItem>(new[] { secret }), "Name");
            //asert
            ret.Should().BeFalse();
        }

        [Fact, IsUnit]
        [Trait("Command", "keyvault")]
        [Trait("SubCommand ", "generatePOCOs")]
        public void RunSemanticChecks_Success()
        {
            //arrange
            var secret = new SecretItem("https://rmtestkeyvault.vault.azure.net:443/secrets/Secret1", null,
                new ConcurrentDictionary<string, string>(
                    new List<KeyValuePair<string, string>>(new[]
                    {
                        new KeyValuePair<string, string>("Name", "Blah"),
                        new KeyValuePair<string, string>("Type", "Blah")
                    })));
            //act
            var ret = KeyVaultExtensions.RunSemanticChecks(new List<SecretItem>(new[] { secret }), "Name", "Type");
            //asert
            ret.Should().BeTrue();
        }
    }
}
