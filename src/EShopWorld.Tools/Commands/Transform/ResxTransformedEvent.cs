using Eshopworld.Core;

namespace EShopWorld.Tools.Commands.Transform
{
    /// <summary>
    /// resx resources converted event
    /// </summary>
    public class ResxTransformedEvent : TelemetryEvent
    {
        /// <summary>
        /// input resx designation
        /// </summary>
        public string ResxProject { get; set; }
    }
}
