using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eshopworld.DevOps;
using static System.String;

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

            return  !IsNullOrWhiteSpace(pascal) ? pascal.Substring(0, 1).ToLowerInvariant() + pascal.Substring(1) : pascal;
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
            return IsNullOrWhiteSpace(input) ? input : Regex.Replace(input.ToLowerInvariant(), "(?:^|-|_|\\s|\\.)(.)", match => match.Groups[1].Value.ToUpperInvariant());
        }

        /// <summary>
        /// checks whether this is an unsigned int - use regexp instead of try parse as we really do not care about output
        /// </summary>
        /// <param name="input">input value</param>
        /// <returns>true if unsigned int</returns>
        public static bool IsUnsignedInt(this string input)
        {
            return !IsNullOrWhiteSpace(input) && Regex.IsMatch(input, "^\\d+$");
        }

        /// <summary>
        /// remove recognized environmental suffix from the name but keep unrecognized ones
        ///
        /// this works by "trim start" and "trim end" so the order of prefixes and suffixes is important to respect the order in the naming convention
        /// </summary>
        /// <param name="input">input values</param>
        /// <param name="suffixes">additional suffixes to strip out</param>
        /// <returns>trimmed string - core name</returns>
        public static string EswTrim(this string input, params string[] suffixes)
        {
            if (IsNullOrWhiteSpace(input))
                return input;


            var prefixList = new List<string>(new[]
            {
                "esw-ci", "esw-test", "esw-sand", "esw-prep", "esw-prod", "esw-integration", "esw-"
            });

            IEnumerable<string> suffixList = new List<string>(new[]
            {
                "-ci", "-test", "-sand", "-prep", "-prod", "-integration" /*~sierra-integration sub */
            });

            //prepend regions
            suffixList = RegionHelper.DeploymentRegionsToList().Select(r => $"-{r.ToRegionCode().ToLowerInvariant()}").Aggregate(suffixList, (current, suffix) => current.Prepend(suffix));
            //prepend additional suffixes
            suffixList = suffixes.Aggregate(suffixList, (current, suffix) => current.Prepend(suffix));

            var newInput = input;

            foreach (var token in prefixList)
            {
                newInput = newInput.StartsWith(token, StringComparison.OrdinalIgnoreCase)?  newInput.Substring(token.Length) : newInput;
            }

            foreach (var token in suffixList)
            {
                newInput = newInput.EndsWith(token, StringComparison.OrdinalIgnoreCase) ? newInput.Substring(0, newInput.Length - token.Length) : newInput;
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

            if (IsNullOrWhiteSpace(name) ||
                !regionList.Any(r => name.EndsWith($"{regionPrefix}{conversionLogic(r)}", StringComparison.OrdinalIgnoreCase) || name.EndsWith($"{regionPrefix}{conversionLogic(r)}-lb", StringComparison.OrdinalIgnoreCase))) //not suffixed with known region, "global resource"
                return true;

            return name.EndsWith($"{regionPrefix}{regionValueToCheck}", StringComparison.OrdinalIgnoreCase) || name.EndsWith($"{regionPrefix}{regionValueToCheck}-lb", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// ensure property name conforms to grammar rules
        ///
        /// if it starts with number, prefix with underscore
        /// dashes replaced with underscore
        /// c# keywords prefixed with underscore
        ///
        /// this assumes secret names are given as an input with Azure key value - naming rules allow for a-z, A0Z, 0-9
        /// </summary>
        /// <param name="value">property name</param>
        /// <returns>grammar compliant property name</returns>
        public static string SanitizePropertyName(this string value)
        {
            var ret = value;
            if (IsNullOrWhiteSpace(ret))
            {
                return ret;
            }

            if (KeywordList.Contains(ret, StringComparer.OrdinalIgnoreCase) || Regex.IsMatch(ret, "^\\d"))
            {
                ret = $"_{ret}";
            }

            return ret.Replace('-', '_');
        }

        /// <summary>
        /// removes fabric scheme prefix from service instance name
        ///
        /// e.g. fabric:/CaptainHook.ServiceFabric/EndpointDispatcherActorService -> CaptainHook.ServiceFabric/EndpointDispatcherActorService
        /// </summary>
        /// <param name="value">full service instance name</param>
        /// <returns>processed string</returns>
        public static string RemoveFabricScheme(this string value)
        {
            if (IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var regexp = new Regex("^fabric:/");
            return regexp.Replace(value, "");
        }

        private static readonly List<string> KeywordList = new List<string>(new[]
        {
            "abstract", "as", "base", "bool",
            "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed",
            "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private",
            "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
            "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
            "unchecked", "unsafe", "ushort", "using", "using", "static", "virtual", "void", "volatile", "while"
        });
    }
}
