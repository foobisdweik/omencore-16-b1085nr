using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OmenCore.Services;

namespace OmenCore.Hardware
{
    /// <summary>
    /// Service for controlling RGB lighting on OMEN desktop PCs.
    /// Supports case fans, LED strips, logo lighting, and front panel accents.
    /// Compatible with OMEN 45L, 40L, 35L, 30L, 25L, and Obelisk series.
    /// </summary>
    public class OmenDesktopRgbService : IDisposable
    {
        private readonly LoggingService _logging;
        private bool _initialized;
        private bool _disposed;
        private List<DesktopRgbZone> _zones = new();
        private string _desktopModel = string.Empty;

        #region WMI Constants

        private const string HP_WMI_BIOS_NAMESPACE = @"root\HP\InstrumentedBIOS";
        private const string HP_WMI_HARDWARE_NAMESPACE = @"root\WMI";
        private const string OMEN_RGB_CLASS = "HPBIOS_BIOSInteger";
        private const string OMEN_RGB_METHOD_CLASS = "HPWMI_RGBControl";

        // Known BIOS setting names for desktop RGB (varies by model)
        private const string SETTING_RGB_ENABLED = "RGB Lighting";
        private const string SETTING_RGB_MODE = "RGB Mode";
        private const string SETTING_RGB_COLOR = "RGB Color";
        private const string SETTING_RGB_SPEED = "RGB Animation Speed";

        #endregion

        #region USB HID Constants (for direct control)

        // OMEN Desktop RGB Controller USB identifiers
        private const int OMEN_RGB_VID = 0x103C; // HP Vendor ID
        private static readonly int[] OMEN_RGB_PIDs = { 0x84FD, 0x84FE, 0x8602, 0x8603 }; // Known RGB controller PIDs

        #endregion

        /// <summary>
        /// Gets whether the desktop RGB service is initialized and available.
        /// </summary>
        public bool IsAvailable => _initialized && _zones.Count > 0;

        /// <summary>
        /// Gets the detected desktop model name.
        /// </summary>
        public string DesktopModel => _desktopModel;

        /// <summary>
        /// Gets the list of detected RGB zones.
        /// </summary>
        public IReadOnlyList<DesktopRgbZone> Zones => _zones.AsReadOnly();

        /// <summary>
        /// Gets the total number of RGB zones.
        /// </summary>
        public int ZoneCount => _zones.Count;

        public OmenDesktopRgbService(LoggingService logging)
        {
            _logging = logging;
        }

        /// <summary>
        /// Initialize the desktop RGB service and detect available zones.
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_initialized) return IsAvailable;

            try
            {
                _logging.Info("OmenDesktopRGB: Initializing...");

                // Check if this is an OMEN desktop
                if (!await DetectOmenDesktopAsync())
                {
                    _logging.Info("OmenDesktopRGB: Not an OMEN desktop or RGB not detected");
                    return false;
                }

                // Try WMI-based detection first
                if (await DetectZonesViaWmiAsync())
                {
                    _logging.Info($"OmenDesktopRGB: Detected {_zones.Count} zones via WMI");
                }
                // Fall back to USB HID detection
                else if (await DetectZonesViaUsbAsync())
                {
                    _logging.Info($"OmenDesktopRGB: Detected {_zones.Count} zones via USB HID");
                }
                else
                {
                    _logging.Info("OmenDesktopRGB: No RGB zones detected");
                    return false;
                }

                _initialized = true;
                _logging.Info($"OmenDesktopRGB: Initialized with {_zones.Count} zones on {_desktopModel}");
                return true;
            }
            catch (Exception ex)
            {
                _logging.Error($"OmenDesktopRGB: Initialization failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Detect if this is an OMEN desktop system.
        /// </summary>
        private async Task<bool> DetectOmenDesktopAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                        var model = obj["Model"]?.ToString() ?? "";

                        if (manufacturer.Contains("HP", StringComparison.OrdinalIgnoreCase) &&
                            model.Contains("OMEN", StringComparison.OrdinalIgnoreCase))
                        {
                            _desktopModel = model;
                            
                            // Check if it's a desktop (not laptop)
                            var systemType = obj["SystemType"]?.ToString() ?? "";
                            var pcType = obj["PCSystemType"]?.ToString() ?? "1";
                            
                            // PCSystemType: 1 = Desktop, 2 = Mobile
                            if (pcType == "1" || model.Contains("45L") || model.Contains("40L") || 
                                model.Contains("35L") || model.Contains("30L") || model.Contains("25L") ||
                                model.Contains("Obelisk"))
                            {
                                _logging.Info($"OmenDesktopRGB: Detected OMEN desktop: {model}");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logging.Warn($"OmenDesktopRGB: WMI detection failed: {ex.Message}");
                }

                return false;
            });
        }

        /// <summary>
        /// Detect RGB zones via HP WMI interface.
        /// </summary>
        private async Task<bool> DetectZonesViaWmiAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(HP_WMI_BIOS_NAMESPACE, "SELECT * FROM HPBIOS_BIOSSetting");
                    
                    bool hasRgb = false;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var name = obj["Name"]?.ToString() ?? "";
                        
                        // Look for RGB-related BIOS settings
                        if (name.Contains("RGB", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("LED", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Light", StringComparison.OrdinalIgnoreCase))
                        {
                            hasRgb = true;
                            _logging.Debug($"OmenDesktopRGB: Found BIOS setting: {name}");
                        }
                    }

                    if (hasRgb)
                    {
                        // Create default zone configuration for OMEN desktops
                        CreateDefaultZones();
                        return _zones.Count > 0;
                    }
                }
                catch (ManagementException ex)
                {
                    _logging.Debug($"OmenDesktopRGB: WMI namespace not available: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logging.Warn($"OmenDesktopRGB: WMI zone detection failed: {ex.Message}");
                }

                return false;
            });
        }

        /// <summary>
        /// Detect RGB zones via USB HID interface.
        /// </summary>
        private async Task<bool> DetectZonesViaUsbAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Search for HP RGB controller USB devices
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_103C%'");
                    
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var deviceId = obj["DeviceID"]?.ToString() ?? "";
                        var name = obj["Name"]?.ToString() ?? "";

                        foreach (var pid in OMEN_RGB_PIDs)
                        {
                            if (deviceId.Contains($"PID_{pid:X4}", StringComparison.OrdinalIgnoreCase))
                            {
                                _logging.Info($"OmenDesktopRGB: Found RGB controller: {name} ({deviceId})");
                                CreateDefaultZones();
                                return _zones.Count > 0;
                            }
                        }

                        // Generic RGB controller detection
                        if (name.Contains("RGB", StringComparison.OrdinalIgnoreCase) && 
                            name.Contains("OMEN", StringComparison.OrdinalIgnoreCase))
                        {
                            _logging.Info($"OmenDesktopRGB: Found generic OMEN RGB: {name}");
                            CreateDefaultZones();
                            return _zones.Count > 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logging.Warn($"OmenDesktopRGB: USB detection failed: {ex.Message}");
                }

                return false;
            });
        }

        /// <summary>
        /// Create default zone configuration based on detected desktop model.
        /// </summary>
        private void CreateDefaultZones()
        {
            _zones.Clear();

            // OMEN 45L has the most zones
            if (_desktopModel.Contains("45L", StringComparison.OrdinalIgnoreCase))
            {
                _zones.Add(new DesktopRgbZone(0, "Top Fan 1", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(1, "Top Fan 2", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(2, "Top Fan 3", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(3, "Front Fan 1", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(4, "Front Fan 2", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(5, "Front Fan 3", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(6, "Interior Strip", RgbZoneType.LedStrip, true));
                _zones.Add(new DesktopRgbZone(7, "OMEN Logo", RgbZoneType.Logo, true));
                _zones.Add(new DesktopRgbZone(8, "Front Panel", RgbZoneType.Accent, true));
            }
            // OMEN 25L/30L - fewer zones
            else if (_desktopModel.Contains("25L", StringComparison.OrdinalIgnoreCase) ||
                     _desktopModel.Contains("30L", StringComparison.OrdinalIgnoreCase))
            {
                _zones.Add(new DesktopRgbZone(0, "Front Fan", RgbZoneType.Fan, true));
                _zones.Add(new DesktopRgbZone(1, "OMEN Logo", RgbZoneType.Logo, true));
                _zones.Add(new DesktopRgbZone(2, "Front Accent", RgbZoneType.Accent, true));
            }
            // Default configuration for unknown models
            else
            {
                _zones.Add(new DesktopRgbZone(0, "RGB Zone 1", RgbZoneType.Generic, true));
                _zones.Add(new DesktopRgbZone(1, "RGB Zone 2", RgbZoneType.Generic, false));
                _zones.Add(new DesktopRgbZone(2, "OMEN Logo", RgbZoneType.Logo, true));
            }

            _logging.Info($"OmenDesktopRGB: Created {_zones.Count} default zones for {_desktopModel}");
        }

        /// <summary>
        /// Set the color for a specific zone.
        /// </summary>
        /// <param name="zoneId">Zone ID</param>
        /// <param name="r">Red (0-255)</param>
        /// <param name="g">Green (0-255)</param>
        /// <param name="b">Blue (0-255)</param>
        public async Task<bool> SetZoneColorAsync(int zoneId, byte r, byte g, byte b)
        {
            if (!IsAvailable) return false;

            var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
            if (zone == null)
            {
                _logging.Warn($"OmenDesktopRGB: Zone {zoneId} not found");
                return false;
            }

            try
            {
                _logging.Debug($"OmenDesktopRGB: Setting zone {zoneId} ({zone.Name}) to RGB({r},{g},{b})");

                // Try WMI first
                if (await SetZoneColorViaWmiAsync(zoneId, r, g, b))
                {
                    zone.CurrentColor = (r, g, b);
                    return true;
                }

                // TODO: Implement USB HID fallback
                _logging.Warn($"OmenDesktopRGB: Failed to set color for zone {zoneId} - no working control method");
                return false;
            }
            catch (Exception ex)
            {
                _logging.Error($"OmenDesktopRGB: Error setting zone color: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Set color for all zones.
        /// </summary>
        public async Task<bool> SetAllZonesColorAsync(byte r, byte g, byte b)
        {
            if (!IsAvailable) return false;

            var success = true;
            foreach (var zone in _zones.Where(z => z.IsControllable))
            {
                if (!await SetZoneColorAsync(zone.Id, r, g, b))
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Set the effect mode for a zone.
        /// </summary>
        /// <param name="zoneId">Zone ID</param>
        /// <param name="mode">Effect mode</param>
        /// <param name="speed">Animation speed (1-10)</param>
        public async Task<bool> SetZoneModeAsync(int zoneId, RgbEffectMode mode, int speed = 5)
        {
            if (!IsAvailable) return false;

            var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
            if (zone == null) return false;

            try
            {
                _logging.Debug($"OmenDesktopRGB: Setting zone {zoneId} mode to {mode}, speed {speed}");

                // Try WMI method
                if (await SetZoneModeViaWmiAsync(zoneId, mode, speed))
                {
                    zone.CurrentMode = mode;
                    zone.AnimationSpeed = speed;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logging.Error($"OmenDesktopRGB: Error setting zone mode: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Set the effect mode for all zones.
        /// </summary>
        public async Task<bool> SetAllZonesModeAsync(RgbEffectMode mode, int speed = 5)
        {
            if (!IsAvailable) return false;

            var success = true;
            foreach (var zone in _zones.Where(z => z.IsControllable))
            {
                if (!await SetZoneModeAsync(zone.Id, mode, speed))
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Set zone color via WMI.
        /// </summary>
        private async Task<bool> SetZoneColorViaWmiAsync(int zoneId, byte r, byte g, byte b)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Try HP WMI BIOS method
                    using var classInstance = new ManagementClass(HP_WMI_BIOS_NAMESPACE, "HPBIOS_BIOSSettingInterface", null);
                    
                    // Pack RGB into 32-bit value (0x00RRGGBB)
                    uint rgbValue = (uint)((r << 16) | (g << 8) | b);
                    
                    var inParams = classInstance.GetMethodParameters("SetBIOSSetting");
                    inParams["Name"] = $"RGB Zone {zoneId} Color";
                    inParams["Value"] = rgbValue.ToString();
                    inParams["Password"] = "";

                    var outParams = classInstance.InvokeMethod("SetBIOSSetting", inParams, null);
                    var returnValue = outParams?["Return"]?.ToString() ?? "1";

                    if (returnValue == "0")
                    {
                        _logging.Debug($"OmenDesktopRGB: WMI SetBIOSSetting succeeded for zone {zoneId}");
                        return true;
                    }
                }
                catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
                {
                    _logging.Debug("OmenDesktopRGB: HP WMI namespace not available");
                }
                catch (Exception ex)
                {
                    _logging.Debug($"OmenDesktopRGB: WMI color set failed: {ex.Message}");
                }

                return false;
            });
        }

        /// <summary>
        /// Set zone effect mode via WMI.
        /// </summary>
        private async Task<bool> SetZoneModeViaWmiAsync(int zoneId, RgbEffectMode mode, int speed)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var classInstance = new ManagementClass(HP_WMI_BIOS_NAMESPACE, "HPBIOS_BIOSSettingInterface", null);
                    
                    var inParams = classInstance.GetMethodParameters("SetBIOSSetting");
                    inParams["Name"] = $"RGB Zone {zoneId} Mode";
                    inParams["Value"] = ((int)mode).ToString();
                    inParams["Password"] = "";

                    var outParams = classInstance.InvokeMethod("SetBIOSSetting", inParams, null);
                    var returnValue = outParams?["Return"]?.ToString() ?? "1";

                    if (returnValue == "0")
                    {
                        // Also set speed if supported
                        inParams["Name"] = $"RGB Zone {zoneId} Speed";
                        inParams["Value"] = speed.ToString();
                        classInstance.InvokeMethod("SetBIOSSetting", inParams, null);
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logging.Debug($"OmenDesktopRGB: WMI mode set failed: {ex.Message}");
                }

                return false;
            });
        }

        /// <summary>
        /// Sync all desktop RGB zones with a unified effect.
        /// </summary>
        public async Task SyncAllZonesAsync(byte r, byte g, byte b, RgbEffectMode mode = RgbEffectMode.Static, int speed = 5)
        {
            await SetAllZonesModeAsync(mode, speed);
            await SetAllZonesColorAsync(r, g, b);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _zones.Clear();
            _initialized = false;
            _disposed = true;

            _logging.Info("OmenDesktopRGB: Disposed");
        }
    }

    /// <summary>
    /// Represents an RGB zone on an OMEN desktop.
    /// </summary>
    public class DesktopRgbZone
    {
        public int Id { get; }
        public string Name { get; }
        public RgbZoneType Type { get; }
        public bool IsControllable { get; }
        public (byte R, byte G, byte B) CurrentColor { get; set; } = (255, 0, 0);
        public RgbEffectMode CurrentMode { get; set; } = RgbEffectMode.Static;
        public int AnimationSpeed { get; set; } = 5;

        public DesktopRgbZone(int id, string name, RgbZoneType type, bool controllable)
        {
            Id = id;
            Name = name;
            Type = type;
            IsControllable = controllable;
        }
    }

    /// <summary>
    /// Types of RGB zones on OMEN desktops.
    /// </summary>
    public enum RgbZoneType
    {
        Generic,
        Fan,
        LedStrip,
        Logo,
        Accent
    }

    /// <summary>
    /// RGB effect modes.
    /// </summary>
    public enum RgbEffectMode
    {
        Off = 0,
        Static = 1,
        Breathing = 2,
        ColorCycle = 3,
        Rainbow = 4,
        Wave = 5,
        Reactive = 6
    }
}
