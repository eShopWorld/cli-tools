using System;
using EShopWorld.Tools.Commands.KeyVault.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EShopWorld.Tools.Commands.KeyVault
{
    public class GeneratePocoProjectInternalCommand : RazorInternalCommandBase<GeneratePocoProjectViewModel>
    {
        /// <inheritdoc/>
        public GeneratePocoProjectInternalCommand(IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider):base(viewEngine, tempDataProvider, serviceProvider, "Commands\\KeyVault\\Views\\PocoProjectFile.cshtml")
        {
                
        }
    }
}
