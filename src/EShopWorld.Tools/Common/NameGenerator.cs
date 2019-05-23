using Eshopworld.DevOps;

// ReSharper disable InconsistentNaming
namespace EShopWorld.Tools.Common
{
    internal static class NameGenerator
    {

        /// <summary>
        /// get regional "V0" RG name
        /// </summary>
        /// <param name="env">environment name</param>
        /// <param name="region">region code</param>
        /// <returns>"V0" regional platform RG Name</returns>
        internal static string GetV0PlatformRegionalRGName(string env, DeploymentRegion region) => $"{region.ToRegionCode()}-platform-{env}".ToLowerInvariant();

        /// <summary>
        /// get name of regional platform level key vault
        /// </summary>
        /// <param name="env">environment name</param>
        /// <param name="region">region code</param>
        /// <returns>regional platform level key vault</returns>
        internal static string GetRegionalPlatformKVName(string env, DeploymentRegion region) => $"esw-platform-{env}-{region.ToRegionCode()}".ToLowerInvariant();

        /// <summary>
        /// get name of platform resource group
        /// </summary>
        /// <param name="env">environment name</param>
        /// <returns>platform resource group name</returns>
        internal static string GetPlatformRGName(string env) => $"platform-{env}".ToLowerInvariant();

        /// <summary>
        /// get name of domain resource group
        /// </summary>
        /// <param name="domain">domain name</param>
        /// <param name="env">environment name</param>
        /// <returns>domain resource group name</returns>
        internal static string GetDomainRGName(string domain, string env) =>
            $"{domain}-{env}".ToLowerInvariant();

        /// <summary>
        /// get regional platform resource group name
        /// </summary>
        /// <param name="env">environment name</param>
        /// <param name="r">region code</param>
        /// <returns>platform resource group name</returns>
        internal static string GetRegionalPlatformRGName(string env, DeploymentRegion r) =>
            $"platform-{env}-{r.ToRegionCode()}".ToLowerInvariant();

        /// <summary>
        /// get name of domain regional key vault
        /// </summary>
        /// <param name="domain">domain name</param>
        /// <param name="env">environment name</param>
        /// <param name="r">region code</param>
        /// <returns>domain regional key vault</returns>
        internal static string GetDomainRegionalKVName(string domain, string env, DeploymentRegion r) =>
            $"esw-{domain}-{env}-{r.ToRegionCode()}".ToLowerInvariant();

        /// <summary>
        /// name of the cluster certificate secret in platform regional resource group
        /// </summary>
        internal const string ServiceFabricPlatformKVCertSecretName = "cluster-cert";
    }
}
