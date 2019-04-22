using System;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    [Collection(nameof(SecurityCLITestsL2Collection))]
    // ReSharper disable once InconsistentNaming
    public class SecurityCommandCLITests :CLIInvokingTestsBase
    {
        private readonly SecurityCLITestsL2Fixture _fixture;
        private const string TestMasterKeyName = "MK";
        private const string TestMasterSecretName = "MS";
        public SecurityCommandCLITests(SecurityCLITestsL2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Theory, IsLayer2]
        [InlineData("-k", "-a", "-b", "-c", "-e")]
        public async Task RotationFlow(string keyVaultParamName, string masterKeyParamName,string masterSecretParamName, string masterKeyStrengthParamName, string secretEncryptionAlgParamName )
        {
            
            //do not use defaults
            GetStandardOutput("security", "rotateSDSKeys", keyVaultParamName, SecurityCLITestsL2Fixture.KeyVaultName,
                masterKeyParamName, TestMasterKeyName, masterSecretParamName, TestMasterSecretName, masterKeyStrengthParamName, "2048", secretEncryptionAlgParamName,
                JsonWebKeyEncryptionAlgorithm.RSAOAEP256);

            var kvUrl = $"https://{SecurityCLITestsL2Fixture.KeyVaultName}.vault.azure.net/";
            
            var key = await _fixture.KeyVaultClient.GetKeyAsync(kvUrl, TestMasterKeyName);

            key.Should().NotBeNull();

            var secret = await _fixture.KeyVaultClient.GetSecretAsync(kvUrl, TestMasterSecretName);

            secret.Should().NotBeNull();
            secret.Value.Should().NotBeNullOrWhiteSpace();

            //rotate
            GetStandardOutput("security", "rotateSDSKeys", keyVaultParamName, SecurityCLITestsL2Fixture.KeyVaultName,
                masterKeyParamName, TestMasterKeyName, masterSecretParamName, TestMasterSecretName, masterKeyStrengthParamName, "2048", secretEncryptionAlgParamName,
                JsonWebKeyEncryptionAlgorithm.RSAOAEP256);


            var key2 = await _fixture.KeyVaultClient.GetKeyAsync(kvUrl, TestMasterKeyName);

            key2.Should().NotBeNull();
            key2.KeyIdentifier.Version.Should().NotBe(key.KeyIdentifier.Version);
            // ReSharper disable once PossibleInvalidOperationException
            key2.Attributes.Created.Should().BeAfter(key.Attributes.Created.Value);

            var nextLink = string.Empty;
            bool found;
            //check previous version is still there
            do
            {
                var versions = string.IsNullOrWhiteSpace(nextLink) ? await _fixture.KeyVaultClient.GetKeyVersionsAsync(kvUrl, TestMasterKeyName): await
                    _fixture.KeyVaultClient.GetKeyVersionsNextAsync(nextLink);

                found = versions.Any(v =>
                    v.Identifier.Version.Equals(key.KeyIdentifier.Version, StringComparison.Ordinal));
                nextLink = versions.NextPageLink;

            } while (!found && !string.IsNullOrWhiteSpace(nextLink));

            found.Should().BeTrue();

            //secret latest version checks
            var secret2 = await _fixture.KeyVaultClient.GetSecretAsync(kvUrl, TestMasterSecretName);

            secret2.Should().NotBeNull();
            secret2.Value.Should().NotBeNullOrWhiteSpace();
            secret2.SecretIdentifier.Version.Should().NotBe(secret.SecretIdentifier.Version);            
            // ReSharper disable once PossibleInvalidOperationException
            secret2.Attributes.Created.Should().BeAfter(secret.Attributes.Created.Value);

            nextLink = string.Empty;

            //check previous version is still there
            do
            {
                var versions = string.IsNullOrWhiteSpace(nextLink) ? await _fixture.KeyVaultClient.GetSecretVersionsAsync(kvUrl, TestMasterSecretName) : await
                    _fixture.KeyVaultClient.GetSecretVersionsNextAsync(nextLink);

                found = versions.Any(v =>
                    v.Identifier.Version.Equals(secret.SecretIdentifier.Version, StringComparison.Ordinal));
                nextLink = versions.NextPageLink;

            } while (!found && !string.IsNullOrWhiteSpace(nextLink));

            found.Should().BeTrue();
        }
    }
}
