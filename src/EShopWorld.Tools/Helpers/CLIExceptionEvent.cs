using System;
using Eshopworld.Core;

namespace EShopWorld.Tools.Helpers
{
    // ReSharper disable once InconsistentNaming
    public sealed class CLIExceptionEvent : ExceptionEvent
    {
        public string CommandType { get; set; }

        public string Arguments { get; set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="e">outer exception reference</param>
        public CLIExceptionEvent(Exception e):base(e)
        {            
        }
    }
}
