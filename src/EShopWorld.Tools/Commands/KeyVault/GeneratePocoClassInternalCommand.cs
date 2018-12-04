using System;
using EShopWorld.Tools.Base;
using EShopWorld.Tools.Commands.KeyVault.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EShopWorld.Tools.Commands.KeyVault
{
    /// <inheritdoc />
    /// <summary>
    /// specific "command" to generate poco class that has recognized secrets as fields and is bindable to keyvault configuration extensions
    /// </summary>
    public class GeneratePocoClassInternalCommand : AbstractRazorCommand<GeneratePocoClassViewModel>
    { 
        /// <inheritdoc />      
        public GeneratePocoClassInternalCommand(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider):base(viewEngine, tempDataProvider, serviceProvider, "Views\\PocoClass.cshtml")
        {
        }
    }
}
