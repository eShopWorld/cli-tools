using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace EShopWorld.Tools.Commands
{
    /// <summary>
    /// abstract class to run razor view generator for specified view and view model
    /// </summary>
    /// <typeparam name="T">type of model for the template</typeparam>
    public abstract class RazorInternalCommandBase<T>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IRazorViewEngine _viewEngine;
        private readonly string _viewPath;

        /// <summary>
        /// DI constructor to supply all necessary services for razor engine to work
        /// </summary>
        /// <param name="viewEngine">view engine itself</param>
        /// <param name="tempDataProvider">temporary cross-request data storage provider</param>
        /// <param name="serviceProvider">service provider for other services as requested by the view</param>
        /// <param name="viewPath">path to the view</param>
        public RazorInternalCommandBase(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider, string viewPath)
        {
            _serviceProvider = serviceProvider;
            _tempDataProvider = tempDataProvider;
            _viewEngine = viewEngine;
            _viewPath = viewPath;
        }


        internal async Task<string> RunViewEngine(string viewPath, T model)
        {
            var view = FindView(viewPath);
            var actionContext = GetActionContext();
            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<T>(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                    {
                        Model = model
                    },
                    new TempDataDictionary(
                        actionContext.HttpContext,
                        _tempDataProvider),
                    output,
                    new HtmlHelperOptions());

                await view.RenderAsync(viewContext);
                return output.ToString();
            }
        }

        internal ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
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

        internal void Run(string outputFile, T model)
        {
            var output = RenderViewToString(model);
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(Directory.GetParent(outputFile).FullName);
                File.WriteAllText(outputFile, output);
            }
        }

        internal string RenderViewToString(T viewModel, string alternateView = null)
        {
            var actionContext = GetActionContext();

            var view = FindView(alternateView ?? _viewPath);

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<T>(
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

        /// <summary>
        /// render the template with the model
        /// </summary>
        /// <param name="model">model instance</param>
        /// <param name="outputFile">output path</param>
        public void Render(T model, string outputFile)
        {
            Run(outputFile, model);
        }
    }
}
