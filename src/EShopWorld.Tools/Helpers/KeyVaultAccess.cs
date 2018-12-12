using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;

namespace EShopWorld.Tools.Helpers
{
    /// <summary>
    /// this class encapsulates operations against the key vault
    /// </summary>
    public static class KeyVaultAccess
    {       
        internal static async Task<IList<SecretItem>> GetAllSecrets(string tenantId, string appId, string appSecret, string keyVaultName, string typeTagName, string nameTagName, string appName)
        {
            //authenticate using the  application id and secrets
            var authContext = new AuthenticationContext($"https://login.windows.net/{tenantId}"); 
            var credential = new ClientCredential(appId, appSecret);
            var token = await authContext.AcquireTokenAsync("https://vault.azure.net", credential);

            //open the vault
            var kv = new KeyVaultClient(new TokenCredentials(token.AccessToken), new HttpClientHandler());

            //iterate via secret pages
            var allSecrets = new List<SecretItem>();
            IPage<SecretItem> secrets = null;
            do
            {
                secrets = await kv.GetSecretsAsync(!string.IsNullOrWhiteSpace(secrets?.NextPageLink) ? secrets.NextPageLink : $"https://{keyVaultName}.vault.azure.net/");
                allSecrets                    
                    .AddRange(secrets.Where(i => i.Tags!=null && i.Tags.Contains(new KeyValuePair<string, string>(typeTagName, appName)))); //filter for the target app only, use the type tag

            } while (!string.IsNullOrWhiteSpace(secrets.NextPageLink));

            if (!RunSemanticChecks(allSecrets, typeTagName, nameTagName))
                throw new ApplicationException("Validation of secret's metadata failed, see console for details");

            return allSecrets;
        }

        /// <summary>
        /// run some basic semantic level checks against secrets
        /// </summary>
        /// <param name="allSecrets">secrets to checks</param>
        /// <param name="requiredTags">collection of required tags</param>
        /// <returns>validation result</returns>
        internal static bool RunSemanticChecks(List<SecretItem> allSecrets, params string[] requiredTags)
        {
            bool result = true;

            foreach (var secret in allSecrets)
            {
                foreach (var tag in requiredTags)
                {
                    if (secret.Tags == null)
                    {
                        result = false;
                        Console.Out.WriteLine($"Secret {secret.Identifier.Name} has no tags");
                        break;                        
                    }

                    if (secret.Tags.ContainsKey(tag)) continue;

                    result = false;
                    Console.Out.WriteLine($"Secret {secret.Identifier.Name} is missing required tag {tag}");
                }
            }

            return result;
        }
    }
}
