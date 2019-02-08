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
        /// remove - and _ and spaces and '.'
        /// </summary>
        /// <param name="input">string to pascal case</param>
        /// <returns>output</returns>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var pascal = System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), "(?:^|-|_|\\s|\\.)(.)", match => match.Groups[1].Value.ToUpperInvariant());
            return pascal.Length > 0 ? pascal.Substring(0, 1).ToLowerInvariant() + pascal.Substring(1) : pascal;
        }

        /// <summary>
        /// remove recognized environmental suffix from the name but keep unrecognized ones
        /// </summary>
        /// <param name="input">input values</param>
        /// <param name="suffixes">suffixes to strip out</param>
        /// <returns></returns>
        public static string EswTrim(this string input, params string[] suffixes)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;


            var tokenList = new List<string>(new[]
                {"esw-", "-ci", "-test", "-sand", "-prep", "-prod", "-integration" /*~sierra-integration sub */});

            //add regions
            tokenList.AddRange(RegionHelper.DeploymentRegionsToList().Select(r => $"-{r.ToRegionCode().ToLowerInvariant()}"));

            tokenList.AddRange(suffixes);

            var newInput = input;

            foreach (var token in tokenList)
            {
                newInput = newInput.Replace(token, "", StringComparison.OrdinalIgnoreCase);
            }

            return newInput;
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

        private static bool RegionCheck(this string name, string region, Func<DeploymentRegion, string> conversionLogic, string regionPrefix="")
        {
            var regionList = RegionHelper.DeploymentRegionsToList();

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
