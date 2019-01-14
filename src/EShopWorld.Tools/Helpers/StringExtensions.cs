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

            var pascal = System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), "(?:^|-|_)(.)", match => match.Groups[1].Value.ToUpperInvariant());
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
        /// check region against recognized regions using code
        /// </summary>
        /// <param name="name">resource name</param>
        /// <param name="region">target region</param>
        /// <returns>true if region matches</returns>
        public static bool ShortRegionCheck(this string name, string region)
        {
            var codeLìst = ConvertDeploymentRegions((i) => i.ToRegionCode());

            var enumerable = codeLìst as string[] ?? codeLìst.ToArray();
            if (!enumerable.Contains(region, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"region {region} not recognized, check whether correctly targeting name vs. code", nameof(region));

            return name.RegionCheck(region, enumerable);
        }

        private static IEnumerable<string> ConvertDeploymentRegions(Func<DeploymentRegion, string> conversionLogic)
        {
            var list = new List<string>();

            foreach (var item in Enum.GetValues(typeof(DeploymentRegion)))
            {
                list.Add(conversionLogic((DeploymentRegion) item));
            }

            return list;
        }

        /// <summary>
        /// check region
        ///
        /// if not suffixed with region (at any level e.g. LB), consider valid
        /// otherwise check required suffix
        /// </summary>
        /// <param name="name">name to check</param>
        /// <param name="regionList">list of regions to recognize</param>
        /// <param name="region">region to check against</param>
        /// <returns>true if checks pass</returns>
        public static bool RegionCheck(this string name, string region, IEnumerable<string> regionList)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                !regionList.Any(r => name.EndsWith(r, StringComparison.OrdinalIgnoreCase) || name.EndsWith($"{r}-lb", StringComparison.OrdinalIgnoreCase))) //not suffixed with region
                return true;

            return name.EndsWith(region, StringComparison.OrdinalIgnoreCase) || name.EndsWith($"{region}-lb", StringComparison.OrdinalIgnoreCase);
        }
    }
}
