using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;

namespace EShopWorld.Tools.Helpers
{
    /// <summary>
    /// this class encapsulates operations against the key vault
    /// </summary>
    public static class KeyVaultExtensions
    {       
        /// TODO: consider moving to package
        internal static async Task<IList<SecretItem>> GetAllSecrets(this KeyVaultClient client, string keyVaultName)
        {        
            //iterate via secret pages
            var allSecrets = new List<SecretItem>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = await client.GetSecretsAsync(!string.IsNullOrWhiteSpace(secrets?.NextPageLink) ? secrets.NextPageLink : $"https://{keyVaultName}.vault.azure.net/");
                allSecrets                    
                    .AddRange(secrets);

            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task SetKeyVaultSecretAsync(this KeyVaultClient client, string keyVaultName, string prefix, string name, string suffix, string value)
        {
            await client.SetSecretWithHttpMessagesAsync($"https://{keyVaultName}.vault.azure.net/", $"{prefix}-{name.StripRecognizedSuffix("-ci", "-test", "-sand", "-preprod", "-prod").ToCamelCase()}-{suffix}", value);
        }
    }
}
