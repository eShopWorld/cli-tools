using System;
using System.Collections.Generic;
using System.Linq;
using Eshopworld.DevOps;

namespace EShopWorld.Tools.Helpers
{
    /// <summary>
    /// extension stuff to camel case given string - based on Humanize package but with adjusted regexp
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// camel case given input
        ///
        /// remove - and _
        /// </summary>
        /// <param name="input">string to pascal case</param>
        /// <returns>output</returns>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var pascal = System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), "(?:^|-|_|\\s)(.)", match => match.Groups[1].Value.ToUpperInvariant());
            return pascal.Length > 0 ? pascal.Substring(0, 1).ToLowerInvariant() + pascal.Substring(1) : pascal;
        }

        /// <summary>
        /// remove recognized environmental suffix from the name but keep unrecognized ones
        /// </summary>
        /// <param name="input">input values</param>
        /// <param name="suffixes">suffixes to strip out</param>
        /// <returns></returns>
        public static string StripRecognizedSuffix(this string input, params string[] suffixes)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string suffixDetected;
            if ((suffixDetected = suffixes.FirstOrDefault(input.EndsWith)) != null)
            {
                return input.Remove(input.Length-suffixDetected.Length);
            }

            return input;
        }

        /// <summary>
        /// check region against recognized regions using abbreviated name (e.g. 'eastus')
        /// </summary>
        /// <param name="name">resource name</param>
        /// <param name="region">target region</param>
        /// <returns>true if region matches</returns>
        public static bool RegionAbbreviatedNameCheck(this string name, string region)
        {
            return name.RegionCheck(region, (i) => i.ToRegionName().ToCamelCase().ToLowerInvariant());
        }

        /// <summary>
        /// check region against recognized regions using name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <param name="region">target region</param>
        /// <returns>true if region matches</returns>
        public static bool RegionNameCheck(this string name, string region)
        {           
            return name.RegionCheck(region, (i) => i.ToRegionName());
        }

        /// <summary>
        /// check region against recognized regions using code
        /// </summary>
        /// <param name="name">resource name</param>
        /// <param name="region">target region</param>
        /// <returns>true if region matches</returns>
        public static bool RegionCodeCheck(this string name, string region)
        {
            return name.RegionCheck(region, (i) => i.ToRegionCode(),"-");
        }

        private static IList<DeploymentRegion> DeploymentRegionsToList()
        {
            var list = new List<DeploymentRegion>();

            foreach (var item in Enum.GetValues(typeof(DeploymentRegion)))
            {
                list.Add((DeploymentRegion) item);
            }

            return list;
        }

        private static bool RegionCheck(this string name, string region, Func<DeploymentRegion, string> conversionLogic, string regionPrefix="")
        {
            var regionList = DeploymentRegionsToList();

            var matched = false;

            var targetRegion =
                regionList.FirstOrDefault(r => (matched=region.Equals(r.ToRegionCode(), StringComparison.OrdinalIgnoreCase)));

            if (!matched)
                return false; //unrecognized region, no match

            var regionValueToCheck = conversionLogic(targetRegion);

            if (string.IsNullOrWhiteSpace(name) ||
                !regionList.Any(r => name.EndsWith($"{regionPrefix}{conversionLogic(r)}", StringComparison.OrdinalIgnoreCase) || name.EndsWith($"{regionPrefix}{conversionLogic(r)}-lb", StringComparison.OrdinalIgnoreCase))) //not suffixed with known region, "global resource"
                return true;

            return name.EndsWith($"{regionPrefix}{regionValueToCheck}", StringComparison.OrdinalIgnoreCase) || name.EndsWith($"{regionPrefix}{regionValueToCheck}-lb", StringComparison.OrdinalIgnoreCase);
        }
    }
}
