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

        private readonly ConcurrentDictionary<SecretHeader, DeletedSecretItem> _deletedSecrets;
            

        private const string KeyVaultLevelSeparator = "--";

        private string _attachedPrefix;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="kvClient">key vault client instance</param>
        public AzScanKeyVaultManager(KeyVaultClient kvClient)
        {
            _kvClient = kvClient;
            _deletedSecrets =
                new ConcurrentDictionary<SecretHeader, DeletedSecretItem>(/*new SecretHeaderEqualityComparer()*/);
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
            _attachedPrefix = secretPrefix;
            var prefix = GetSecretPrefixLevelToken(secretPrefix);

            var tasks = kvNames.Select(k => Task.Run(async () =>
            {
                var secrets = await _kvClient.GetAllSecrets(k, prefix);
                foreach (var secret in secrets)
                {
                    _kvState.TryAdd(new SecretHeader(k, secret.SecretIdentifier.Name),
                        new TrackedSecretBundle(secret, false));
                }
            })).ToList();

            tasks.AddRange(kvNames.Select(k => Task.Run(async () =>
            {
                var secrets = await _kvClient.GetDeletedSecrets(k, prefix);
                foreach (var secret in secrets)
                {
                    _deletedSecrets.TryAdd(new SecretHeader(k, secret.Identifier.Name), secret);
                }
            })));


            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// implement post processing of key vaults
        ///
        /// any secret marked as not refreshed within recognized prefixes will be deleted as consider no longer necessary
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public async Task DetachKeyVaults()
        {
            if (string.IsNullOrWhiteSpace(_attachedPrefix))
            {
                throw new ApplicationException("No KV attached");
            }

            //delete unused secrets
            var tasks = _kvState
                    .Where(l => !l.Value.Touched &&
                                l.Value.Secret.SecretIdentifier.Name.StartsWith(GetSecretPrefixLevelToken(_attachedPrefix), StringComparison.Ordinal))
                    .Select(i => _kvClient.DeleteSecret(i.Key.KeyVaultName, i.Value.Secret.SecretIdentifier.Name)); //soft-delete

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
            _kvState.TryAdd(header, new TrackedSecretBundle(recovered, false));
            _deletedSecrets.TryRemove(header, out _);
        }

        private bool IsDeleted(string keyVaultName, string targetName) =>_deletedSecrets.ContainsKey(new SecretHeader(keyVaultName, targetName));

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

            public override bool Equals(object obj)
            {
                return obj is SecretHeader header && Equals(header);
            }

            public  bool Equals(SecretHeader other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(KeyVaultName, other.KeyVaultName, StringComparison.OrdinalIgnoreCase) 
                       && string.Equals(SecretName, other.SecretName, StringComparison.OrdinalIgnoreCase);
            }

            public override int GetHashCode()
            {
                return $"{KeyVaultName}:{SecretName}".ToLowerInvariant().GetHashCode();
            }
        }

        private class SecretHeaderEqualityComparer : IEqualityComparer<SecretHeader>
        {
            public bool Equals(SecretHeader x, SecretHeader y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x==null || y==null)
                {
                    return false;
                }

                return x.Equals(y);
            }

            public int GetHashCode(SecretHeader obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
