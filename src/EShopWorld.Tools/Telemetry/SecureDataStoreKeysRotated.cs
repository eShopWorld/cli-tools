using Eshopworld.Core;

namespace EShopWorld.Tools.Telemetry
{
    /// <summary>
    /// telemetry event for <see cref="T:EShopWorld.Tools.Commands.Security.SecurityCommand.RotateSDSKeysCommand"/>
    ///
    /// indicates Master Key and Master Secret were rotated and their new version identifiers
    /// </summary>
    public class SecureDataStoreKeysRotated : TelemetryEvent
    {
        /// <summary>
        /// name of the key containing the MasterKey
        /// </summary>
        public string MasterKeyName { get; set; }
        /// <summary>
        /// version identifier of the rotated MasterKey
        /// </summary>
        public string MasterKeyNewVersionId { get; set; }
        /// <summary>
        /// name of the secret containing MasterSecret
        /// </summary>
        public string MasterSecretName { get; set; }
        /// <summary>
        /// version identifier of the rotated MasterSecret
        /// </summary>
        public string MasterSecretNewVersionId { get; set; }
    }
}
