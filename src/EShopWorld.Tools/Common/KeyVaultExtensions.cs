using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;
using Microsoft.Rest.TransientFaultHandling;
using Polly;

namespace EShopWorld.Tools.Common
{
    /// <summary>
    /// this class encapsulates operations against the key vault
    /// </summary>
    internal static class KeyVaultExtensions
    {
        internal static async Task<IList<SecretBundle>> GetAllSecrets(this KeyVaultClient client, string keyVaultName, string prefix = null)
        {

            //iterate via secret pages
            var allSecrets = new List<SecretBundle>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = !string.IsNullOrWhiteSpace(secrets?.NextPageLink)
                    ? await client.GetSecretsNextAsync(secrets.NextPageLink)
                    : await client.GetSecretsAsync(GetKeyVaultUrlFromName(keyVaultName));
                allSecrets.AddRange(await Task.WhenAll(secrets
                    .Where(i => i.Attributes.Enabled.GetValueOrDefault() &&
                                (string.IsNullOrWhiteSpace(prefix) || i.Identifier.Name.StartsWith(prefix)))
                    .Select(s => client.GetSecretAsync(s.Identifier.Identifier))));

            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task<SecretBundle> GetSecret(this KeyVaultClient client, string keyVaultName,
            string secretName)
        {
            return await client.GetSecretAsync(GetKeyVaultUrlFromName(keyVaultName), secretName);
        }

        internal static async Task<IList<SecretItem>> GetDisabledSecrets(this KeyVaultClient client, string keyVaultName, string prefix=null)
        {
            //iterate via secret pages
            var allSecrets = new List<SecretItem>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = !string.IsNullOrWhiteSpace(secrets?.NextPageLink) ? await client.GetSecretsNextAsync(secrets.NextPageLink) : await client.GetSecretsAsync(GetKeyVaultUrlFromName(keyVaultName));

                allSecrets.AddRange(secrets.Where(s =>
                    !s.Attributes.Enabled.GetValueOrDefault() &&
                    (string.IsNullOrWhiteSpace(prefix) || s.Identifier.Name.StartsWith(prefix))));

            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task DeleteAllSecrets(this KeyVaultClient client, string keyVaultName)
        {
            var list = await client.GetAllSecrets(keyVaultName);
            await Task.WhenAll(list.Select(i => client.DeleteSecret(keyVaultName, i.SecretIdentifier.Name)));
        }

        internal static async Task DeleteSecret(this KeyVaultClient client, string keyVaultName, string secretName, double confirmationWaitTime = 100, bool confirmDelete = true)
        {
            var keyVaultUrl = GetKeyVaultUrlFromName(keyVaultName);
            await client.DeleteSecretAsync(keyVaultUrl, secretName);

            if (!confirmDelete)
                return;

            try
            {
                //wait for full delete - note that this is "forever" in the scope of the specific status code
                await Policy
                    .Handle<NullReferenceException>()
                    .Or<ObjectDisposedException>()
                    .Or<KeyVaultErrorException>(r =>
                        r.Response?.StatusCode == HttpStatusCode.NotFound)
                    .Or<HttpRequestWithStatusException>(r =>
                        r.StatusCode == HttpStatusCode.NotFound)
                    .WaitAndRetryForeverAsync(w => TimeSpan.FromMilliseconds(confirmationWaitTime))
                    .ExecuteAsync(() =>
                        client.GetDeletedSecretAsync(keyVaultUrl, secretName));
            }
            catch (KeyVaultErrorException e) when (e.Response.StatusCode == HttpStatusCode.Forbidden)
            {
                //ignore - simply do not wait for delete to finish
            }
        }

        internal static async Task<SecretBundle> SetKeyVaultSecretAsync(this KeyVaultClient client, string keyVaultName,
            string name, string value, double recoveryLoopWaitTime = 100)
        {

            var result = await Policy
                .Handle<KeyVaultErrorException>(r =>
                    r.Response.StatusCode == HttpStatusCode.NotFound || r.Response.StatusCode == HttpStatusCode.Conflict)
                .Or<HttpRequestWithStatusException>(r => r.StatusCode == HttpStatusCode.NotFound || r.StatusCode==HttpStatusCode.Conflict)
                .WaitAndRetryForeverAsync(w => TimeSpan.FromMilliseconds(recoveryLoopWaitTime))
                .ExecuteAsync(() => client.SetSecretWithHttpMessagesAsync(GetKeyVaultUrlFromName(keyVaultName), name, value));

            return result.Body;
        }

        internal static async Task<DeletedSecretBundle> GetDeletedSecret(this KeyVaultClient client,
            string keyVaultName, string secretName)
        {
            return await client.GetDeletedSecretAsync(GetKeyVaultUrlFromName(keyVaultName), secretName);
        }

        internal static async Task<AzureOperationResponse<DeletedSecretBundle>>
            GetDeletedSecretWithHttpMessages(this KeyVaultClient client, string keyVaultName, string secretName)
        {
            return await client.GetDeletedSecretWithHttpMessagesAsync(GetKeyVaultUrlFromName(keyVaultName), secretName);
        }

        internal static async Task<IList<DeletedSecretItem>> GetDeletedSecrets(this KeyVaultClient client,
            string keyVaultName, string prefix = null)
        {
            //iterate via secret pages
            var allSecrets = new List<DeletedSecretItem>();
            IPage<DeletedSecretItem> secrets = null;
            do
            {
                secrets = !string.IsNullOrWhiteSpace(secrets?.NextPageLink)
                    ? await client.GetDeletedSecretsNextAsync(secrets.NextPageLink)
                    : await client.GetDeletedSecretsAsync(GetKeyVaultUrlFromName(keyVaultName));

                allSecrets.AddRange(secrets.Where(secret => string.IsNullOrWhiteSpace(prefix) || secret.Identifier.Name.StartsWith(prefix)));
            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            return allSecrets;
        }

        internal static async Task<SecretBundle> RecoverSecret(this KeyVaultClient client, string keyVaultName, string secretName, double recoveryLoopWaitTime = 100)
        {
            var keyVaultUrl = GetKeyVaultUrlFromName(keyVaultName);

            try
            {
                await client.RecoverDeletedSecretAsync(keyVaultUrl, secretName);
            }
            catch (KeyVaultErrorException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                //forget this one as there are naming convention duplicates in DNS
            }

            //wait for full recovery - note that this is "forever" in the scope of the specific status codes
            var response = await Policy
                .Handle<NullReferenceException>()
                .Or<ObjectDisposedException>()
                .Or<KeyVaultErrorException>(r =>
                    r.Response.StatusCode == HttpStatusCode.NotFound ||
                    r.Response.StatusCode == HttpStatusCode.Conflict)
                .Or<HttpRequestWithStatusException>(r => r.StatusCode == HttpStatusCode.NotFound || r.StatusCode == HttpStatusCode.Conflict)
                .WaitAndRetryForeverAsync(w => TimeSpan.FromMilliseconds(recoveryLoopWaitTime))
                .ExecuteAsync(() => client.GetSecretWithHttpMessagesAsync(keyVaultUrl, secretName,string.Empty /*latest*/));

            return response.Body;

        }

        private static string GetKeyVaultUrlFromName(string name)
        {
            return $"https://{name}.vault.azure.net/";
        }
    }
}
