using Eshopworld.Core;

namespace EShopWorld.Tools.Telemetry
{
    /// <summary>
    /// timed event for any command executed
    /// </summary>
    public class EswCliToolCommandExecutionTimedEvent : TimedTelemetryEvent
    {
        /// <summary>
        /// name of command that produced the exception
        /// </summary>
        public string CommandType { get; set; }

        /// <summary>
        /// arguments passed to the command
        /// </summary>
        public string Arguments { get; set; }

    }
}
