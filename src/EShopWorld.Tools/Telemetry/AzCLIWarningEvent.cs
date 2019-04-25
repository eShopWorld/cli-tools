using Eshopworld.Core;

namespace EShopWorld.Tools.Telemetry
{
    public class AzCLIWarningEvent : TelemetryEvent
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
        /// warning message
        /// </summary>
        public string Warning { get; set; }
    }
}
