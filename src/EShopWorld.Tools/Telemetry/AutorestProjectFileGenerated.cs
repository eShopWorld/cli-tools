using Eshopworld.Core;

namespace EShopWorld.Tools.Telemetry
{
    /// <summary>
    /// event describing the project file generated for given swagger file
    /// </summary>
    public class AutorestProjectFileGenerated : TelemetryEvent
    {
        /// <summary>
        /// swagger file designation
        /// </summary>
        public string SwaggerFile { get; set; }
    }
}
