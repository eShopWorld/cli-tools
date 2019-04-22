using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.Fluent;

namespace EShopWorld.Tools.Commands.AzScan
{
    /// <summary>
    /// this abstract class appends key rotation switch to az scan base class
    /// </summary>
    public abstract class AzScanKeyRotationCommandBase : AzScanCommandBase
    {
        /// <summary>
        /// boolean flag to indicate secondary key should be used instead of primary
        /// </summary>
        [Option(
            Description = "flag to switch to secondary key",
            ShortName = "2",
            LongName = "switchToSecondaryKey",
            ShowInHelpText = true)]
        public bool UseSecondaryKey { get; set; }

        protected AzScanKeyRotationCommandBase()
        {        
        }

        protected AzScanKeyRotationCommandBase(Azure.IAuthenticated authenticated, AzScanKeyVaultManager keyVaultManager,
            IBigBrother bigBrother, string secretPrefix) : base(authenticated, keyVaultManager, bigBrother, secretPrefix)
        {           
        }
    }
}
