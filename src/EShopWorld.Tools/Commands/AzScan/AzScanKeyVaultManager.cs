using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EShopWorld.Tools.Common;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// encapsulates secret working, key vault communication
    ///
    /// automatically detects value changing and tracks live secrets which allows for defunct secrets to be deleted
    /// </summary>
    public class AzScanKeyVaultManager
    {
        private readonly KeyVaultClient _kvClient;

        private readonly ConcurrentDictionary<SecretHeader, TrackedSecretBundle> _kvState =
            new ConcurrentDictionary<SecretHeader, TrackedSecretBundle>();

        private readonly ConcurrentDictionary<SecretHeader, DeletedSecretItem> _deletedSecrets =
            new ConcurrentDictionary<SecretHeader, DeletedSecretItem>();

        private const string KeyVaultLevelSeparator = "--";

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="kvClient">key vault client instance</param>
        public AzScanKeyVaultManager(KeyVaultClient kvClient)
        {
            _kvClient = kvClient;
        }

        /// <summary>
        /// attach tracked key vaults to the manager
        /// </summary>
        /// <param name="kvNames">list of tracked key (regional) vaults</param>
        /// <param name="secretPrefix">prefix for secrets to narrow loaded secrets</param>
        /// <returns>task result</returns>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task AttachKeyVaults(IEnumerable<string> kvNames, string secretPrefix)
        {
            var prefix = GetSecretPrefixLevelToken(secretPrefix);

            await Task.WhenAll(kvNames.Select(k => Task.Run(async ()=>
            {
                var secrets = await _kvClient.GetAllSecrets(k, prefix);
                foreach (var secret in secrets)
                {
                    _kvState.TryAdd(new SecretHeader(k, secret.SecretIdentifier.Name), new TrackedSecretBundle(secret, false));
                }
            })));


            await Task.WhenAll(kvNames.Select(k => Task.Run(async () =>
            {
                var secrets = await _kvClient.GetDeletedSecrets(k, prefix);
                foreach (var secret in secrets)
                {
                    _deletedSecrets.TryAdd(new SecretHeader(k, secret.Identifier.Name), secret);
                }
            })));
        }

        /// <summary>
        /// implement post processing of key vaults
        ///
        /// any secret marked as not refreshed within recognized prefixes will be deleted as consider no longer necessary
        /// </summary>
        /// <param name="secretPrefix">target secret naming prefix (as in prefix--name--suffix)</param>
        /// <returns><see cref="Task"/></returns>
        public async Task DetachKeyVaults(string secretPrefix)
        {
            //delete unused secrets
            var tasks = _kvState
                    .Where(l => !l.Value.Touched &&
                                l.Value.Secret.SecretIdentifier.Name.StartsWith(GetSecretPrefixLevelToken(secretPrefix)))
                    .Select(i => _kvClient.DeleteSecret(i.Key.KeyVaultName, i.Value.Secret)); //soft-delete

            await Task.WhenAll(tasks);
        }

        private static string GetSecretPrefixLevelToken(string secretPrefix)
        {
            return secretPrefix.EndsWith(KeyVaultLevelSeparator)
                ? secretPrefix
                : $"{secretPrefix}{KeyVaultLevelSeparator}";
        }

        /// <summary>
        /// implements secret workflow
        /// if new or changed, set in the underlying key vault
        ///
        /// <see cref="prefix"/> is required, followed by either processed/trimmed <see cref="name"/> or <see cref="suffix"/> or both
        /// </summary>
        /// <param name="keyVaultName">name of target key vault</param>
        /// <param name="prefix">secret name prefix - required</param>
        /// <param name="name">secret core name - optional</param>
        /// <param name="suffix">secret name suffix - optional</param>
        /// <param name="value">actual secret value</param>
        /// <param name="additionalSuffixes">additional suffixes to apply</param>
        /// <returns><see cref="Task"/></returns>
        public async Task SetKeyVaultSecretAsync(string keyVaultName,
            string prefix, string name, string suffix, string value, params string[] additionalSuffixes)
        {
            //prefix must always be specified
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("value required", nameof(prefix));
            }

            var trimmedName = !string.IsNullOrWhiteSpace(name) ? name.EswTrim(additionalSuffixes).ToPascalCase() : null;

            if (string.IsNullOrWhiteSpace(trimmedName) && string.IsNullOrWhiteSpace(suffix))
            {
                throw new ArgumentException($"both processed {nameof(name)} and {nameof(suffix)} cannot be empty");
            }

            var sb = new StringBuilder(prefix);
            if (!string.IsNullOrWhiteSpace(trimmedName))
            {
                sb.Append(KeyVaultLevelSeparator);
                sb.Append(trimmedName);
            }

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                sb.Append(KeyVaultLevelSeparator);
                sb.Append(suffix);
            }

            var targetName = sb.ToString();
            //soft-deleted? recover then
            if (IsDeleted(keyVaultName, targetName))
            {
                await ProcessSecretRecovery(keyVaultName, targetName);
                //now it can located and processed as "regular" secret
            }

            //detect new vs change, otherwise just track visit
            var trackedSecret = LocateSecret(keyVaultName, targetName);
            if (trackedSecret == null)
            {
                //new secret
                var secret = await _kvClient.SetKeyVaultSecretAsync(keyVaultName, targetName, value);
                AddNewSecret(keyVaultName, secret);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(trackedSecret.Secret.Value) || !trackedSecret.Secret.Value.Equals(value, StringComparison.Ordinal))
                {
                    //secret value changed
                    var newSecret = await _kvClient.SetKeyVaultSecretAsync(keyVaultName, targetName, value);
                    trackedSecret.Secret = newSecret;
                    trackedSecret.Touched = true;
                }
                else
                {
                    //no change, just track as refreshed
                    trackedSecret.Touched = true;
                }
            }
        }

        private async Task ProcessSecretRecovery(string keyVaultName, string targetName)
        {
            var recovered= await _kvClient.RecoverSecret(keyVaultName, targetName);
            var header = new SecretHeader(keyVaultName, targetName);
            if (!_kvState.TryAdd(header, new TrackedSecretBundle(recovered, false)))
            {
                throw new ApplicationException(
                    $"Failure adding new secret - {keyVaultName}:{targetName}"); //pure precautionary measure here
            }

            if (!_deletedSecrets.TryRemove(header, out _))
            {
                throw new ApplicationException(
                    $"Failure adding new secret - {keyVaultName}:{targetName}"); //pure precautionary measure here
            }
        }

        private bool IsDeleted(string keyVaultName, string targetName)
        {
            return _deletedSecrets.TryGetValue(new SecretHeader(keyVaultName, targetName), out _);
        }

        private void AddNewSecret(string keyVaultName, SecretBundle newSecret)
        {
            if (newSecret == null)
            {
                throw new ArgumentNullException(nameof(newSecret));
            }

            if (!_kvState.TryAdd(new SecretHeader(keyVaultName, newSecret.SecretIdentifier.Name),
                new TrackedSecretBundle(newSecret, true))) //mark as refreshed
            {
                throw new ApplicationException(
                    $"Failure adding new secret - {keyVaultName}:{newSecret.SecretIdentifier.Name}"); //pure precautionary measure here
            }
        }

        private TrackedSecretBundle LocateSecret(string keyVaultName, string targetName)
        {
            return _kvState.FirstOrDefault(i =>
                i.Key.KeyVaultName.Equals(keyVaultName, StringComparison.OrdinalIgnoreCase) &&
                i.Key.SecretName.Equals(targetName, StringComparison.OrdinalIgnoreCase)).Value;
        }

        private class TrackedSecretBundle
        {
            protected internal SecretBundle Secret { get; set; }
            protected internal bool Touched { get; set; }

            protected internal TrackedSecretBundle(SecretBundle secret, bool touched)
            {
                Secret = secret;
                Touched = touched;
            }
        }

        private class SecretHeader : IEquatable<SecretHeader>
        {
            internal readonly string KeyVaultName;
            internal readonly string SecretName;

            protected internal SecretHeader(string keyVaultName, string secretName)
            {
                KeyVaultName = keyVaultName;
                SecretName = secretName;
            }

            public bool Equals(SecretHeader other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(KeyVaultName, other.KeyVaultName) && string.Equals(SecretName, other.SecretName);
            }

            public override int GetHashCode()
            {
                return $"{KeyVaultName}:{SecretName}".GetHashCode();
            }
        }
    }
}
