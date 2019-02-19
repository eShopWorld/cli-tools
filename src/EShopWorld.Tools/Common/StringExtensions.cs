﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eshopworld.DevOps;

namespace EShopWorld.Tools.Common
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
            var pascal = ToPascalCase(input);

            return  !string.IsNullOrWhiteSpace(pascal) ? pascal.Substring(0, 1).ToLowerInvariant() + pascal.Substring(1) : pascal;
        }

        /// <summary>
        /// pascal case given input
        ///
        /// remove - and _ and spaces and '.'
        /// </summary>
        /// <param name="input">given input</param>
        /// <returns>pascal case string</returns>
        public static string ToPascalCase(this string input)
        {
            return string.IsNullOrWhiteSpace(input) ? input : Regex.Replace(input.ToLowerInvariant(), "(?:^|-|_|\\s|\\.)(.)", match => match.Groups[1].Value.ToUpperInvariant());
        }

        /// <summary>
        /// checks whether this is an unsigned int - use regexp instead of try parse as we really do not care about output
        /// </summary>
        /// <param name="input">input value</param>
        /// <returns>true if unsigned int</returns>
        public static bool IsUnsignedInt(this string input)
        {
            return !string.IsNullOrWhiteSpace(input) && Regex.IsMatch(input, "^\\d+$");
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

        /// <summary>
        /// ensure property name conforms to grammar rules
        ///
        /// if it starts with number, prefix with underscore
        ///
        /// this assumes secret names are given as an input with Azure key value - naming rules allow for a-z, A0Z, 0-9
        /// </summary>
        /// <param name="value">property name</param>
        /// <returns>gr</returns>
        public static string SanitizePropertyName(this string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, "^\\d"))
                return value;

            return $"_{value}";
        }
    }
}
