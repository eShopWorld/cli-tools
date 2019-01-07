using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace EShopWorld.Tools.Commands.AutoRest
{   
    public static class SwaggerJsonParser
    {
        /// <summary>
        /// parse out title and version and return as a tuple
        /// </summary>
        /// <param name="fileUrl">url to the json file with swagger metadata</param>
        /// <returns>tuple with sanitised title and version</returns>
        public static (string, string) ParsetOut(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ApplicationException($"Invalid url to swagger json file {fileUrl}");

            using (var client = new WebClient())
            {
                var json = client.DownloadString(fileUrl);
                var fragment = JsonSerializer.CreateDefault()
                    .Deserialize<SwaggerFragment>(
                        new JsonTextReader(new StringReader(json)));

                return fragment?.Info != null
                    ? (SanitizeTitle(fragment.Info.Title), SanitizeVersion(fragment.Info.Version))
                    : (null, null);
            }
        }

        /// <summary>
        /// sanitise title so that csproj can be named as such
        /// </summary>
        /// <param name="unsanitized">swagger title as it comes from swagger</param>
        /// <returns>sanitised version</returns>
        public static string SanitizeTitle(string unsanitized)
        {
            return unsanitized?
                .Replace(" ", "")
                .Replace("/", "")
                .Replace("?", "")
                .Replace(":", "")
                .Replace("&", "")
                .Replace("\\", "")
                .Replace("*", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace(">", "")
                .Replace("|", "")
                .Replace("#", "")
                .Replace("%", "");
        }

        /// <summary>
        /// sanitise version coming from the swagger so that we can use it within the csproj and version the nuget package ultimately
        /// </summary>
        /// <param name="unsanitised">unsanitised version coming from the swagger</param>
        /// <returns>sanitised version</returns>
        public static string SanitizeVersion(string unsanitised)
        {
            var pattern = new Regex("\\d+(\\.\\d+)*");
            var m = pattern.Match(unsanitised);
            return m.Value;
        }

        public class SwaggerFragment
        {
            [JsonProperty("info")]
            public SwaggerInfoFragment Info { get; set; }
        }

        public class SwaggerInfoFragment
        {
            [JsonProperty("version")]
            public string Version { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }
        }
    }
}
