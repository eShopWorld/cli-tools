﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EShopWorld.Tools.Common;
using Kusto.Cloud.Platform.Utils;
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

        private readonly Dictionary<string, IList<TrackedSecretBundle>> _kvInitialState = new Dictionary<string, IList<TrackedSecretBundle>>();

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
        /// <param name="kvNames">list of tracked keyvaults</param>
        /// <param name="secretPrefix">prefix for secrets to narrow loaded secrets</param>
        /// <returns>task result</returns>
        public async Task AttachKeyVaults(IEnumerable<string> kvNames, string secretPrefix)
        {
            foreach (var kv in kvNames)
            {
                _kvInitialState.Add(kv,
                    (await _kvClient.GetAllSecrets(kv, secretPrefix))?.Select(i => new TrackedSecretBundle(i, false)).ToList());
            }
        }

        /// <summary>
        /// implement post processing of key vaults
        ///
        /// any secret marked as not refreshed within recognized prefixes will be deleted as consider no longer necessary
        /// </summary>
        /// <param name="secretPrefix">target secret naming prefix (as in prefix--name--suffix)</param>
        /// <returns><see cref="Task"/></returns>
        public void DetachKeyVaults(string secretPrefix)
        {
             if (!_kvInitialState.Any())
            {
                throw new ApplicationException($"No key vaults have been attached, this instance must be initialized via {nameof(AttachKeyVaults)} call first");
            }

            //cross check all matching prefix secrets that have not been touched
            _kvInitialState.ForEach(k =>
                k.Value.Where(s => s.Secret.SecretIdentifier.Name.StartsWith(secretPrefix) && !s.Touched)
                    .ForEach(async i => await _kvClient.DeleteSecret(k.Key, i.Secret)));
        }

        /// <summary>
        /// implements secret workflow
        /// if new or changed, set in the underlying key vault
        /// </summary>
        /// <param name="keyVaultName">name of target key vault</param>
        /// <param name="prefix">secret name prefix</param>
        /// <param name="name">secret core name</param>
        /// <param name="suffix">secret name suffix</param>
        /// <param name="value">actual secret value</param>
        /// <param name="additionalSuffixes">additional suffixes to apply</param>
        /// <returns><see cref="Task"/></returns>
        public async Task SetKeyVaultSecretAsync(string keyVaultName,
            string prefix, string name, string suffix, string value, params string[] additionalSuffixes)
        {
            var trimmedName = name.EswTrim(additionalSuffixes).ToCamelCase();
            var targetName = $"{prefix}{(!string.IsNullOrWhiteSpace(trimmedName) ? "--" + trimmedName : "")}--{suffix}";
            
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
                if (!trackedSecret.Secret.Value.Equals(value, StringComparison.Ordinal))
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

        private void AddNewSecret(string keyVaultName, SecretBundle newSecret)
        {
            if (string.IsNullOrWhiteSpace(keyVaultName) || !_kvInitialState.ContainsKey(keyVaultName))
            {
                throw new ApplicationException($"Attempt to use unattached key vault {keyVaultName}");
            }

            if (newSecret == null)
            {
                throw new ArgumentNullException(nameof(newSecret));
            }

            _kvInitialState[keyVaultName].Add(new TrackedSecretBundle(newSecret, true)); //mark as refreshed
        }

        private TrackedSecretBundle LocateSecret(string keyVaultName, string targetName)
        {
            if (string.IsNullOrWhiteSpace(keyVaultName) || !_kvInitialState.ContainsKey(keyVaultName))
                throw new ApplicationException($"Attempt to use unattached key vault {keyVaultName}");

            return _kvInitialState[keyVaultName].FirstOrDefault(i =>
                i.Secret.SecretIdentifier.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        }

        private class TrackedSecretBundle
        {
            protected internal SecretBundle Secret { get; set; }
            protected internal bool Touched  {get; set; }

            public TrackedSecretBundle(SecretBundle secret, bool touched)
            {
                Secret = secret;
                Touched = touched;
            }
        }
    }
}
