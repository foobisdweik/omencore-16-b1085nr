using System.Collections.Generic;
using OmenCore.Corsair;

namespace OmenCore.Models
{
    public class AppConfig
    {
        public string EcDevicePath { get; set; } = @"\\.\WinRing0_1_2";
        public int MonitoringIntervalMs { get; set; } = 1000;
        public List<FanPreset> FanPresets { get; set; } = new();
        public List<PerformanceMode> PerformanceModes { get; set; } = new();
        public List<ServiceToggle> SystemToggles { get; set; } = new();
        public List<LightingProfile> LightingProfiles { get; set; } = new();
        public List<CorsairLightingPreset> CorsairLightingPresets { get; set; } = new();
        public List<CorsairDpiStage> DefaultCorsairDpi { get; set; } = new();
        public List<MacroProfile> MacroProfiles { get; set; } = new();
        public Dictionary<string, int> EcFanRegisterMap { get; set; } = new();
        public UndervoltPreferences Undervolt { get; set; } = new();
        public MonitoringPreferences Monitoring { get; set; } = new();
        public UpdatePreferences Updates { get; set; } = new();
        public bool FirstRunCompleted { get; set; } = false;
        
        /// <summary>
        /// Enable detailed diagnostics logging at startup (OGH commands, WMI status, etc.)
        /// </summary>
        public bool EnableDiagnostics { get; set; } = false;
        
        /// <summary>
        /// Last applied performance mode name (e.g., "Balanced", "Performance", "Quiet")
        /// Restored on startup.
        /// </summary>
        public string? LastPerformanceModeName { get; set; }
    }
}
