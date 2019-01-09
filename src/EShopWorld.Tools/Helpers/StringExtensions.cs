using System.Linq;

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

            var pascal = System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), "(?:^|-|_)(.)", match => match.Groups[1].Value.ToUpper());
            return pascal.Length > 0 ? pascal.Substring(0, 1).ToLower() + pascal.Substring(1) : pascal;
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
    }
}
