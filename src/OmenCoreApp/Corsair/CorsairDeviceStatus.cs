namespace OmenCore.Corsair
{
    public class CorsairDeviceStatus
    {
        public int BatteryPercent { get; set; }
        public int PollingRateHz { get; set; }
        public string FirmwareVersion { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = "USB";
        
        /// <summary>
        /// Additional notes or limitations for this device (e.g., "Wireless mouse connects through this receiver")
        /// </summary>
        public string? Notes { get; set; }
    }
}
