using Eshopworld.Core;

namespace EShopWorld.Tools.Telemetry
{
    /// <summary>
    /// event describing POCO being generated for certain keyvault
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class KeyVaultPOCOGeneratedEvent : TelemetryEvent
    {
        /// <summary>
        /// name of KV
        /// </summary>
        public string KeyVaultName { get; set; }
        /// <summary>
        /// namespace
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// version id
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// name of the app
        /// </summary>
        public string AppName { get; set; }
    }
}
