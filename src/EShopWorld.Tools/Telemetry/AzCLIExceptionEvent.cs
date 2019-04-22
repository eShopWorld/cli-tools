using System;
using Eshopworld.Core;

namespace EShopWorld.Tools.Telemetry
{
    /// <summary>
    /// generic CLI exception telemetry event
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class AzCLIExceptionEvent : ExceptionEvent
    {
        /// <summary>
        /// name of command that produced the exception
        /// </summary>
        public string CommandType { get; set; }

        /// <summary>
        /// arguments passed to the command
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="e">outer exception reference</param>
        public AzCLIExceptionEvent(Exception e):base(e)
        {            
        }
    }
}
