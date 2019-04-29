using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;

namespace EShopWorld.Tools.Common
{
    /// <summary>
    /// this class encapsulates operations against the key vault
    /// </summary>
    internal static class KeyVaultExtensions
    {       
        internal static async Task<IList<SecretBundle>> GetAllSecrets(this KeyVaultClient client, string keyVaultName, string prefix=null)
        {        
            //iterate via secret pages
            var allSecrets = new List<SecretBundle>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = !string.IsNullOrWhiteSpace(secrets?.NextPageLink) ? await client.GetSecretsNextAsync(secrets.NextPageLink) : await client.GetSecretsAsync(GetKeyVaultUrlFromName(keyVaultName));
                foreach (var secretItem in secrets.Where(s=>s.Attributes.Enabled.GetValueOrDefault()))
                {
                    if (!string.IsNullOrWhiteSpace(prefix) && !secretItem.Identifier.Name.StartsWith(prefix)) //if prefix is specified, only load those
                    {
                        continue;
                    }

                    allSecrets.Add(await client.GetSecretAsync(secretItem.Identifier.Identifier));
                }

            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task<IList<SecretItem>> GetDisabledSecrets(this KeyVaultClient client, string keyVaultName)
        {
            //iterate via secret pages
            var allSecrets = new List<SecretItem>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = !string.IsNullOrWhiteSpace(secrets?.NextPageLink) ? await client.GetSecretsNextAsync(secrets.NextPageLink) : await client.GetSecretsAsync(GetKeyVaultUrlFromName(keyVaultName));

                allSecrets.AddRange(secrets.Where(s => !s.Attributes.Enabled.GetValueOrDefault()));
            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task DeleteAllSecrets(this KeyVaultClient client, string keyVaultName)
        {
            var list = await client.GetAllSecrets(keyVaultName);
            foreach (var s in list)
            {
                await client.DeleteSecretAsync(GetKeyVaultUrlFromName(keyVaultName), s.SecretIdentifier.Name);
            }
        }

        internal static async Task DeleteSecret(this KeyVaultClient client, string keyVaultName, SecretBundle secret)
        {
            await client.DeleteSecretAsync(GetKeyVaultUrlFromName(keyVaultName), secret.SecretIdentifier.Name);
        }

        internal static async Task DisableSecret(this KeyVaultClient client, string keyVaultName, SecretBundle secret)
        {
            secret.Attributes.Enabled = false;
            //disable current version - no need to create new one with "set"
            await client.UpdateSecretWithHttpMessagesAsync(GetKeyVaultUrlFromName(keyVaultName),
                secret.SecretIdentifier.Name, secret.SecretIdentifier.Version, secretAttributes: secret.Attributes);
        }

        internal static async Task<SecretBundle> SetKeyVaultSecretAsync(this KeyVaultClient client, string keyVaultName,
            string name, string value)
        {

            var result = await client.SetSecretWithHttpMessagesAsync(GetKeyVaultUrlFromName(keyVaultName), name, value);
            return result.Body;
        }

        private static string GetKeyVaultUrlFromName(string name)
        {
            return $"https://{name}.vault.azure.net/";
        }
    }
}
