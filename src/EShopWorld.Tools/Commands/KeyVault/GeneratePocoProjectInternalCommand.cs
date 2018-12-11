using System;
using EShopWorld.Tools.Base;
using EShopWorld.Tools.Commands.KeyVault.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EShopWorld.Tools.Commands.KeyVault
{
    public class GeneratePocoProjectInternalCommand : AbstractRazorCommand<GeneratePocoProjectViewModel>
    {
        /// <inheritdoc/>
        public GeneratePocoProjectInternalCommand(IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider):base(viewEngine, tempDataProvider, serviceProvider, "Commands\\KeyVault\\Views\\PocoProjectFile.cshtml")
        {
                
        }
    }
}
