using System;
using System.ComponentModel.DataAnnotations;
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
            [StringLength(24, MinimumLength = 3, ErrorMessage = "KeyVault name must be between 3 and 24 characters long")]
            [Required]
            // ReSharper disable once MemberCanBePrivate.Global
            public string KeyVaultName { get; set; }

            [Option(
                Description = "name of master HSM backed key (defaults to 'MASTERKEY')",
                ShortName = "a",
                LongName = "masterKeyName",
                ShowInHelpText = true)]
            [StringLength(127, MinimumLength = 1, ErrorMessage = "Master Key Name cannot be empty or longer than 127 characters")]
            // ReSharper disable once StringLiteralTypo
            public string MasterKeyName { get; } = "MASTERKEY";

            [Option(
                Description = "name of master secret (defaults to 'MASTERSECRET')",
                ShortName = "b",
                LongName = "masterSecretName",
                ShowInHelpText = true)]
            [StringLength(127, MinimumLength = 1, ErrorMessage = "Master Secret Name cannot be empty or longer than 127 characters")]
            // ReSharper disable once StringLiteralTypo
            public string MasterSecretName { get; } = "MASTERSECRET";

            [Option(
                Description = "key strength of master key (defaults to 2048) - possible values are 2048, 3072, 4096",
                ShortName = "c",
                LongName = "masterKeyStrength",
                ShowInHelpText = true)]
            [RegularExpression("2048|3072|4096", ErrorMessage = "Master Key Strength must be either 2048, 3072 or 4096")]
            public int MasterKeyStrength { get; } = 2048;

            //private enum AllowedKeyStrengths : string {S2048="2048", S3072="3072", S4096="4096"};

            //256 is max (https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesmanaged.keysize?redirectedfrom=MSDN&view=netframework-4.7.2#System_Security_Cryptography_AesManaged_KeySize)           
            private const int MasterSecretStrength = 256;

            [Option(
                Description = "encryption algorithm for master secret (defaults to RSA-OAEP-256)- possible values 'RSA-OAEP-256','RSA1_5','RSA-OAEP'",
                ShortName = "e",
                LongName = "masterSecretEncryptionAlg",
                ShowInHelpText = true)]
            [RegularExpression("RSA\\-OAEP\\-256|RSA\\-OAEP|RSA1_5", ErrorMessage = "Secret Encryption Algorithm must be either RSA-OAEP or RSA1_5 or RSA-OAEP-256")]
            public string SecretEncryptionAlgorithm { get; } = JsonWebKeyEncryptionAlgorithm.RSAOAEP256;

            public RotateSDSKeysCommand(KeyVaultClient kvClient, IBigBrother bigBrother)
            {
                _kvClient = kvClient;
                _bigBrother = bigBrother;
            }

            public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {                
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
                    MasterSecretNewVersionId = masterSecret.SecretIdentifier.Version,
                    KeyVault =  KeyVaultName
                });
                
                return 0;
            }           
        }
    }
}
