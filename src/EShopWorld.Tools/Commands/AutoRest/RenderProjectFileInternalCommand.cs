using System;
using System.IO;
using System.Linq;
using EShopWorld.Tools.Commands.AutoRest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace EShopWorld.Tools.Commands.AutoRest
{
    public class RenderProjectFileInternalCommand
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
        public RenderProjectFileInternalCommand(
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
            Run(outputFile, model, "Commands\\AutoRest\\Views\\ProjectFile.cshtml");
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
}
