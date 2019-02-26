using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault.WebKey;

namespace EShopWorld.Tools.Commands.Security
{
    /// <summary>
    /// Top level security command - wrapper for sub-commands
    ///
    /// does not do anything on its own
    /// </summary>
    [Command("security", Description = "various security related commands"), HelpOption]
    [Subcommand(typeof(RotateSDSKeysCommand))]
    public class SecurityCommand
    {
        /// <summary>
        /// output appropriate message to denote sub-command is missing
        /// </summary>
        /// <param name="app">app instance</param>
        /// <param name="console">console</param>
        /// <returns></returns>
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }

        [Command("rotateSDSKeys", Description =
            "rotates Secure Data Store keys - MasterKey and MasterSecret - by adding their new versions")]
        // ReSharper disable once InconsistentNaming
        internal class RotateSDSKeysCommand
        {
            private readonly KeyVaultClient _kvClient;
            private readonly IBigBrother _bigBrother;

            [Option(
                Description = "name of the vault to open",
                ShortName = "k",
                LongName = "keyVault",
                ShowInHelpText = true)]
            [Required]
            // ReSharper disable once MemberCanBePrivate.Global
            public string KeyVaultName { get; set; }

            [Option(
                Description = "name of master HSM backed key (defaults to 'RSAHSMKEY')",
                ShortName = "a",
                LongName = "masterKeyName",
                ShowInHelpText = true)]
            // ReSharper disable once StringLiteralTypo
            public string MasterKeyName { get; } = "RSAHSMKEY";

            [Option(
                Description = "name of master secret (defaults to 'MASTERSECRET')",
                ShortName = "b",
                LongName = "masterSecretName",
                ShowInHelpText = true)]
            // ReSharper disable once StringLiteralTypo
            public string MasterSecretName { get; } = "MASTERSECRET";

            [Option(
                Description = "key strength of master key (defaults to 2048) - possible values are 2048, 3072, 4096",
                ShortName = "c",
                LongName = "masterKeyStrength",
                ShowInHelpText = true)]
            public int MasterKeyStrength { get; } = 2048;

            private static readonly int[] AllowedKeyStrengths = {2048, 3072, 4096};

            //256 is max (https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesmanaged.keysize?redirectedfrom=MSDN&view=netframework-4.7.2#System_Security_Cryptography_AesManaged_KeySize)           
            private static readonly int MasterSecretStrength  = 256;

            [Option(
                Description = "encryption algorithm for master secret (defaults to RSA-OAEP-256)- possible values 'RSA-OAEP-256','RSA1_5','RSA-OAEP'",
                ShortName = "e",
                LongName = "masterSecretEncryptionAlg",
                ShowInHelpText = true)]
            public string SecretEncryptionAlgorithm { get; } = JsonWebKeyEncryptionAlgorithm.RSAOAEP256;

            public RotateSDSKeysCommand(KeyVaultClient kvClient, IBigBrother bigBrother)
            {
                _kvClient = kvClient;
                _bigBrother = bigBrother;
            }

            public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                ValidateOptions();

                var kvUrl = $"https://{KeyVaultName}.vault.azure.net/";
                //create/rotate master key

                var masterKey = await _kvClient.CreateKeyAsync(kvUrl, MasterKeyName,
                    new NewKeyParameters() {Kty = "RSA-HSM", KeySize = MasterKeyStrength});

                //create/rotate master secret
                var aesManaged = new AesManaged { KeySize = MasterSecretStrength };

                //but encrypt it with master key first

                var encryptResult = await _kvClient.EncryptAsync(kvUrl,
                    masterKey.KeyIdentifier.Name, masterKey.KeyIdentifier.Version, SecretEncryptionAlgorithm,
                    aesManaged.Key);

                //persist
                var masterSecret = await _kvClient.SetSecretAsync(kvUrl, MasterSecretName, Convert.ToBase64String(encryptResult.Result), null, "base64");

                _bigBrother.Publish(new SecureDataStoreKeysRotated
                {
                    MasterKeyName = MasterKeyName, MasterSecretName = MasterSecretName,
                    MasterKeyNewVersionId = masterKey.KeyIdentifier.Version,
                    MasterSecretNewVersionId = masterSecret.SecretIdentifier.Version
                });
                
                return 0;
            }

            private void ValidateOptions()
            {
                if (string.IsNullOrWhiteSpace(KeyVaultName))
                {
                    throw new ArgumentException("missing key vault name", nameof(KeyVaultName));
                }

                if (string.IsNullOrWhiteSpace(MasterKeyName))
                {
                    throw new ArgumentException("missing master key name", nameof(MasterKeyName));
                }

                if (string.IsNullOrWhiteSpace(MasterSecretName))
                {
                    throw new ArgumentException("missing master secret name", nameof(MasterSecretName));
                }

                if (!AllowedKeyStrengths.Contains(MasterKeyStrength))
                {
                    throw new ArgumentException($"invalid key strength - {MasterKeyStrength} - allowed values are {string.Join(',', AllowedKeyStrengths)}", nameof(MasterKeyStrength));
                }

                if (!JsonWebKeyEncryptionAlgorithm.AllAlgorithms.Contains(SecretEncryptionAlgorithm))
                {
                    throw new ArgumentException(
                        $"invalid secret encryption algorithm- {SecretEncryptionAlgorithm} - allowed values are {string.Join(',', JsonWebKeyEncryptionAlgorithm.AllAlgorithms)}");
                }
            }
        }
    }
}
