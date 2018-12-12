﻿using System;
using EShopWorld.Tools.Commands.AutoRest.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EShopWorld.Tools.Commands.AutoRest
{
    /// <summary>
    /// internal "command" to render the project file
    /// </summary>
    public class RenderProjectFileInternalCommand : RazorInternalCommandBase<ProjectFileViewModel>
    {    
        /// <summary>
        /// DI constructor to supply all necessary services for razor engine to work
        /// </summary>
        /// <param name="viewEngine">view engine itself</param>
        /// <param name="tempDataProvider">temporary cross-request data storage provider</param>
        /// <param name="serviceProvider">service provider for other services as requested by the view</param>
        public RenderProjectFileInternalCommand(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider):base(viewEngine, tempDataProvider, serviceProvider, "Commands\\AutoRest\\Views\\ProjectFile.cshtml")
        {          
        }    
    }
}
