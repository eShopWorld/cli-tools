using System.Collections.Generic;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;

namespace EShopWorld.Tools.Helpers
{
    /// <summary>
    /// this class encapsulates operations against the key vault
    /// </summary>
    internal static class KeyVaultExtensions
    {       
        internal static async Task<IList<SecretBundle>> GetAllSecrets(this KeyVaultClient client, string keyVaultName)
        {        
            //iterate via secret pages
            var allSecrets = new List<SecretBundle>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = !string.IsNullOrWhiteSpace(secrets?.NextPageLink) ? await client.GetSecretsNextAsync(secrets.NextPageLink) : await client.GetSecretsAsync($"https://{keyVaultName}.vault.azure.net/");
                foreach (var secretItem in secrets)
                {
                    allSecrets.Add(await client.GetSecretAsync(secretItem.Identifier.Identifier));
                }

            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task DeleteAllSecrets(this KeyVaultClient client, string keyVaultName)
        {
            var list = await client.GetAllSecrets(keyVaultName);
            foreach (var s in list)
            {
                await client.DeleteSecretAsync($"https://{keyVaultName}.vault.azure.net/", s.SecretIdentifier.Name);
            }
        }

        internal static async Task SetKeyVaultSecretAsync(this KeyVaultClient client, string keyVaultName,
            string prefix, string name, string suffix, string value, bool removeEnvironmentToken = true,
            params string[] additionalSuffixes)
        {
            var suffixesToRemove = new List<string>();
            if (removeEnvironmentToken)
            {
                suffixesToRemove.AddRange(new[] {"-ci", "-test", "-sand", "-preprod", "-prod"});
            }

            suffixesToRemove.AddRange(additionalSuffixes);

            await client.SetSecretWithHttpMessagesAsync($"https://{keyVaultName}.vault.azure.net/", $"{prefix}--{name.StripRecognizedSuffix(suffixesToRemove.ToArray()).ToCamelCase()}--{suffix}", value);
        }
    }
}
