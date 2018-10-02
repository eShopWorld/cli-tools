using EShopWorld.Tools.Commands.AutoRest.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace EShopWorld.Tools.Commands.AutoRest
{
    public class RenderProjectFileCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IRazorViewEngine _viewEngine;

        /// <summary>
        /// DI constructor to supply all necessary services for razor engine to work
        /// </summary>
        /// <param name="viewEngine">view engine itself</param>
        /// <param name="tempDataProvider">temporary cross-request data storage provider</param>
        /// <param name="serviceProvider">service provider for other services as requested by the view</param>
        public RenderProjectFileCommand(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _tempDataProvider = tempDataProvider;
            _viewEngine = viewEngine;
        }

        public virtual void Render(ProjectFileViewModel model, string outputFile)
        {
            Run(outputFile, model, "Views\\ProjectFile.cshtml");
        }

        public virtual string RenderViewToString(ProjectFileViewModel viewModel, string viewPath)
        {
            var actionContext = GetActionContext();

            var view = FindView(viewPath);

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<ProjectFileViewModel>(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                    {
                        Model = viewModel
                    },
                    new TempDataDictionary(
                        actionContext.HttpContext,
                        _tempDataProvider),
                    output,
                    new HtmlHelperOptions());

                view.RenderAsync(viewContext).GetAwaiter().GetResult();
                return output.ToString();
            }

            ActionContext GetActionContext()
            {
                var httpContext = new DefaultHttpContext
                {
                    RequestServices = _serviceProvider
                };

                return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            }
        }

        internal virtual void Run(string outputFile, ProjectFileViewModel model, string viewPath)
        {
            var output = RenderViewToString(model, viewPath);
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(Directory.GetParent(outputFile).FullName);
                File.WriteAllText(outputFile, output);
            }
        }

        /// <summary>
        /// find view using the (mostly relative) path
        /// 
        /// we do not support - given the context- finding view using web app conventions
        /// </summary>
        /// <param name="viewPath">view path</param>
        /// <returns><see cref="IView"/> instance</returns>
        /// <exception cref="InvalidOperationException">if view cannot be found</exception>

        internal IView FindView(string viewPath)
        {
            var getViewResult = _viewEngine.GetView(null, viewPath, true);

            if (getViewResult.Success)
                return getViewResult.View;

            var searchedLocations = getViewResult.SearchedLocations;

            var errorMessage = string.Join(
                Environment.NewLine,
                new[] { $"Unable to find view '{viewPath}'. The following locations were searched:" }.Concat(
                    searchedLocations));

            throw new InvalidOperationException(errorMessage);
        }
    }


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
            Regex pattern = new Regex("\\d+(\\.\\d+)*");
            Match m = pattern.Match(unsanitised);
            return m?.Value ?? throw new ApplicationException($"Unrecognized version number {unsanitised}");
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
