using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using Microsoft.Azure.KeyVault.Models;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// some extensions for <see cref="T:IEnumerable&lt;Microsoft.Azure.KeyVault.Models.SecretBundle&gt;"/>
    /// </summary>
    public static class SecretBundleCollectionFluentAssertionsExtensions
    {
        /// <summary>
        /// ensure name and value defined secret assertion is met
        /// </summary>
        /// <param name="assert">assertion chain</param>
        /// <param name="name">expected name</param>
        /// <param name="value">expected value</param>
        /// <param name="nameComparison">string comparison for name</param>
        /// <param name="valueComparison">string comparison for value</param>
        /// <returns>constraint instance</returns>
        public static AndWhichConstraint<GenericCollectionAssertions<SecretBundle>, SecretBundle> HaveSecret(this GenericCollectionAssertions<SecretBundle> assert, string name, string value, StringComparison nameComparison = StringComparison.Ordinal, StringComparison valueComparison = StringComparison.OrdinalIgnoreCase)
        {
            return assert.Contain(s =>
                s.SecretIdentifier.Name.Equals(name,
                    nameComparison) &&
                s.Value.Equals(value,
                    valueComparison));
        }

        /// <summary>
        /// ensure secret by given name does not exist in the loaded collection
        /// </summary>
        /// <param name="assert">assertion chain</param>
        /// <param name="name">expected name</param>
        /// <param name="nameComparison">string comparison for name</param>
        /// <returns>constraint instance</returns>
        public static AndConstraint<GenericCollectionAssertions<SecretBundle>> NotHaveSecretByName(this GenericCollectionAssertions<SecretBundle> assert, string name, StringComparison nameComparison = StringComparison.Ordinal)
        {
            return assert.NotContain(s =>
                s.SecretIdentifier.Name.Equals(name,
                    nameComparison));
        }

        /// <summary>
        /// check number of secrets with certain prefix
        /// </summary>
        /// <param name="assert">assertion chain</param>
        /// <param name="namePrefix">secret prefix</param>
        /// <param name="count">expected count</param>
        /// <param name="nameComparison">string comparison for name</param>
        /// <returns>constraint instance</returns>
        public static AndConstraint<GenericCollectionAssertions<SecretBundle>> HaveSecretCountWithNameStarting(this GenericCollectionAssertions<SecretBundle> assert, string namePrefix, int count, StringComparison nameComparison = StringComparison.Ordinal)
        {
            return assert.Match(c => c.Count(s => s.SecretIdentifier.Name.StartsWith(namePrefix, nameComparison))==count);
        }
    }
}
