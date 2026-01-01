using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HidSharp;
using OmenCore.Logitech;

namespace OmenCore.Services.Logitech
{
    /// <summary>
    /// Direct HID communication with Logitech devices - no G HUB required.
    /// Communicates directly with USB HID interface for RGB control.
    /// </summary>
    public class LogitechHidDirect : ILogitechSdkProvider
    {
        private readonly LoggingService _logging;
        private bool _initialized;
        private readonly List<LogitechHidDevice> _devices = new();
        
        // Logitech USB Vendor ID
        private const int LOGITECH_VID = 0x046D;
        
        // Known Logitech G product IDs and their device types
        // Comprehensive list including 2024/2025 devices
        private static readonly Dictionary<int, (string Name, LogitechDeviceType Type)> KnownProducts = new()
        {
            // ============= KEYBOARDS =============
            // G213 Series
            { 0xC336, ("G213 Prodigy", LogitechDeviceType.Keyboard) },
            { 0xC33F, ("G213 Prodigy (Alt)", LogitechDeviceType.Keyboard) },
            
            // G4xx Series
            { 0xC330, ("G410 Atlas Spectrum", LogitechDeviceType.Keyboard) },
            { 0xC33A, ("G413 Carbon", LogitechDeviceType.Keyboard) },
            { 0xC33B, ("G413 Silver", LogitechDeviceType.Keyboard) },
            
            // G5xx Series  
            { 0xC342, ("G512 Carbon", LogitechDeviceType.Keyboard) },
            { 0xC33C, ("G512 SE", LogitechDeviceType.Keyboard) },
            { 0xC346, ("G512 RGB", LogitechDeviceType.Keyboard) },
            
            // G6xx Series
            { 0xC333, ("G610 Orion", LogitechDeviceType.Keyboard) },
            { 0xC338, ("G610 Orion Brown", LogitechDeviceType.Keyboard) },
            
            // G7xx Series (Wireless)
            { 0xC547, ("G715 Wireless", LogitechDeviceType.Keyboard) },
            { 0xC548, ("G713 Wired", LogitechDeviceType.Keyboard) },
            
            // G8xx Series
            { 0xC331, ("G810 Orion Spectrum", LogitechDeviceType.Keyboard) },
            { 0xC337, ("G810 Orion Spectrum (2)", LogitechDeviceType.Keyboard) },
            { 0xC33E, ("G815 Lightsync", LogitechDeviceType.Keyboard) },
            { 0xC33D, ("G815 Lightsync (Alt)", LogitechDeviceType.Keyboard) },
            
            // G9xx Series
            { 0xC32B, ("G910 Orion Spark", LogitechDeviceType.Keyboard) },
            { 0xC335, ("G910 Orion Spectrum", LogitechDeviceType.Keyboard) },
            { 0xC343, ("G915 Lightspeed", LogitechDeviceType.Keyboard) },
            { 0xC545, ("G915 TKL Lightspeed", LogitechDeviceType.Keyboard) },
            { 0xC541, ("G915 Wireless", LogitechDeviceType.Keyboard) },
            
            // G PRO Keyboards
            { 0xC341, ("G PRO Keyboard", LogitechDeviceType.Keyboard) },
            { 0xC339, ("G PRO X Keyboard", LogitechDeviceType.Keyboard) },
            { 0xC34A, ("G PRO X TKL", LogitechDeviceType.Keyboard) },
            { 0xC549, ("G PRO X 60", LogitechDeviceType.Keyboard) },
            
            // ============= MICE =============
            // G1xx Series (Budget)
            { 0xC084, ("G102/G203 Prodigy", LogitechDeviceType.Mouse) },
            { 0xC082, ("G203 Lightsync", LogitechDeviceType.Mouse) },
            { 0xC092, ("G203 LIGHTSYNC (2)", LogitechDeviceType.Mouse) },
            
            // G3xx Series  
            { 0xC07D, ("G302 Daedalus Prime", LogitechDeviceType.Mouse) },
            { 0xC093, ("G305 Lightspeed", LogitechDeviceType.Mouse) },
            { 0xC099, ("G309 Lightspeed", LogitechDeviceType.Mouse) },
            
            // G4xx Series
            { 0xC07E, ("G402 Hyperion Fury", LogitechDeviceType.Mouse) },
            { 0xC083, ("G403 Prodigy", LogitechDeviceType.Mouse) },
            { 0xC08F, ("G403 Hero", LogitechDeviceType.Mouse) },
            { 0xC096, ("G403 Lightspeed", LogitechDeviceType.Mouse) },
            
            // G5xx Series
            { 0xC332, ("G502 Proteus Spectrum", LogitechDeviceType.Mouse) },
            { 0xC08B, ("G502 Hero", LogitechDeviceType.Mouse) },
            { 0xC08D, ("G502 Lightspeed", LogitechDeviceType.Mouse) },
            { 0xC094, ("G502 X Lightspeed", LogitechDeviceType.Mouse) },
            { 0xC095, ("G502 X Plus", LogitechDeviceType.Mouse) },
            { 0xC098, ("G502 X", LogitechDeviceType.Mouse) },
            
            // G6xx Series (Wireless)
            { 0xC088, ("G603 Lightspeed", LogitechDeviceType.Mouse) },
            { 0xC53A, ("G604 Lightspeed", LogitechDeviceType.Mouse) },
            
            // G7xx Series
            { 0xC537, ("G703 Lightspeed", LogitechDeviceType.Mouse) },
            { 0xC090, ("G703 Hero", LogitechDeviceType.Mouse) },
            
            // G9xx Series
            { 0xC085, ("G903 Lightspeed", LogitechDeviceType.Mouse) },
            { 0xC091, ("G903 Hero", LogitechDeviceType.Mouse) },
            
            // G PRO Mice
            { 0xC08A, ("G PRO Gaming Mouse", LogitechDeviceType.Mouse) },
            { 0xC08C, ("G PRO Wireless", LogitechDeviceType.Mouse) },
            { 0xC09C, ("G PRO X Superlight", LogitechDeviceType.Mouse) },
            { 0xC09D, ("G PRO X Superlight 2", LogitechDeviceType.Mouse) },
            { 0xC09F, ("G PRO X Superlight 2 DEX", LogitechDeviceType.Mouse) },
            
            // ============= HEADSETS =============
            // G4xx Series
            { 0x0A66, ("G430 Gaming Headset", LogitechDeviceType.Headset) },
            { 0x0ABE, ("G435 Lightspeed", LogitechDeviceType.Headset) },
            
            // G5xx Series
            { 0x0A89, ("G533 Wireless", LogitechDeviceType.Headset) },
            { 0x0A5B, ("G535 Lightspeed", LogitechDeviceType.Headset) },
            { 0x0A78, ("G560 Lightsync Speakers", LogitechDeviceType.Headset) },
            
            // G6xx Series
            { 0x0A6D, ("G633 Artemis Spectrum", LogitechDeviceType.Headset) },
            { 0x0A88, ("G635 Gaming Headset", LogitechDeviceType.Headset) },
            
            // G7xx Series
            { 0x0AAA, ("G733 Lightspeed", LogitechDeviceType.Headset) },
            { 0x0AB0, ("G733 Lightspeed (Alt)", LogitechDeviceType.Headset) },
            
            // G9xx Series
            { 0x0A87, ("G933 Artemis Spectrum", LogitechDeviceType.Headset) },
            { 0x0AB5, ("G935 Wireless", LogitechDeviceType.Headset) },
            
            // PRO Series Headsets
            { 0x0AAC, ("PRO X Wireless", LogitechDeviceType.Headset) },
            { 0x0AAE, ("PRO X 2 Lightspeed", LogitechDeviceType.Headset) },
            { 0x0AB7, ("PRO X 2 Lightspeed (Alt)", LogitechDeviceType.Headset) },
            
            // ASTRO (Logitech-owned)
            { 0x0AAD, ("ASTRO A50 Gen 4", LogitechDeviceType.Headset) },
            { 0x0AB8, ("ASTRO A30", LogitechDeviceType.Headset) },
            
            // ============= ACCESSORIES =============
            // Mouse Pads
            { 0xC53B, ("G Powerplay", LogitechDeviceType.Mouse) },
            { 0xC53F, ("G840 XL", LogitechDeviceType.Mouse) },
            
            // Receivers (useful for detecting wireless devices)
            { 0xC539, ("Lightspeed Receiver", LogitechDeviceType.Mouse) },
            { 0xC547, ("Lightspeed Receiver (2)", LogitechDeviceType.Mouse) },
        };

        public LogitechHidDirect(LoggingService logging)
        {
            _logging = logging;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logging.Info("Initializing Logitech direct HID access (no G HUB required)...");
                
                var deviceList = DeviceList.Local;
                var hidDevices = deviceList.GetHidDevices();
                
                int foundCount = 0;
                var seenProducts = new HashSet<int>();
                
                foreach (var hidDevice in hidDevices)
                {
                    if (hidDevice.VendorID == LOGITECH_VID)
                    {
                        // Only process known G-series devices and avoid duplicates
                        if (!seenProducts.Contains(hidDevice.ProductID) && IsGSeriesDevice(hidDevice.ProductID))
                        {
                            try
                            {
                                var productName = GetProductName(hidDevice);
                                var deviceType = GetDeviceType(hidDevice.ProductID);
                                
                                // Select the best interface for RGB control
                                if (IsRgbInterface(hidDevice))
                                {
                                    var logitechDevice = new LogitechHidDevice
                                    {
                                        HidDevice = hidDevice,
                                        ProductId = hidDevice.ProductID,
                                        DeviceInfo = new LogitechDevice
                                        {
                                            DeviceId = hidDevice.DevicePath,
                                            Name = productName,
                                            DeviceType = deviceType,
                                            Status = new LogitechDeviceStatus
                                            {
                                                BatteryPercent = 100,
                                                Dpi = 800,
                                                MaxDpi = 25600,
                                                FirmwareVersion = "N/A (Direct HID)",
                                                ConnectionType = "USB"
                                            }
                                        }
                                    };
                                    
                                    _devices.Add(logitechDevice);
                                    seenProducts.Add(hidDevice.ProductID);
                                    foundCount++;
                                    _logging.Info($"  Found Logitech device: {productName} (PID: 0x{hidDevice.ProductID:X4})");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logging.Warn($"  Skipping Logitech device 0x{hidDevice.ProductID:X4}: {ex.Message}");
                            }
                        }
                    }
                }
                
                _initialized = foundCount > 0;
                
                if (_initialized)
                {
                    _logging.Info($"Logitech direct HID initialized - {foundCount} device(s) found");
                }
                else
                {
                    _logging.Info("No Logitech G devices found via direct HID");
                }
                
                return await Task.FromResult(_initialized);
            }
            catch (Exception ex)
            {
                _logging.Error($"Failed to initialize Logitech direct HID: {ex.Message}");
                return false;
            }
        }

        private bool IsGSeriesDevice(int productId)
        {
            // Check if it's a known G-series device
            if (KnownProducts.ContainsKey(productId))
                return true;
            
            // Heuristics for G-series PIDs
            // G-series keyboards: 0xC3xx range
            // G-series mice: 0xC0xx range  
            // G-series headsets: 0x0Axx range
            return (productId >= 0xC080 && productId <= 0xC0FF) ||  // Mice
                   (productId >= 0xC300 && productId <= 0xC3FF) ||  // Keyboards
                   (productId >= 0xC530 && productId <= 0xC5FF) ||  // Wireless devices
                   (productId >= 0x0A60 && productId <= 0x0AFF);    // Headsets/Audio
        }

        private string GetProductName(HidDevice device)
        {
            if (KnownProducts.TryGetValue(device.ProductID, out var info))
            {
                return info.Name;
            }
            
            try
            {
                var name = device.GetProductName();
                if (!string.IsNullOrEmpty(name))
                    return name;
            }
            catch { }
            
            return $"Logitech G Device (0x{device.ProductID:X4})";
        }

        private LogitechDeviceType GetDeviceType(int productId)
        {
            if (KnownProducts.TryGetValue(productId, out var info))
            {
                return info.Type;
            }
            
            // Heuristics based on PID ranges
            if (productId >= 0xC080 && productId <= 0xC0FF)
                return LogitechDeviceType.Mouse;
            if (productId >= 0xC300 && productId <= 0xC3FF)
                return LogitechDeviceType.Keyboard;
            if (productId >= 0x0A60 && productId <= 0x0AFF)
                return LogitechDeviceType.Headset;
            if (productId >= 0xC530 && productId <= 0xC5FF)
                return LogitechDeviceType.Mouse; // Wireless mice/receivers
                
            return LogitechDeviceType.Unknown;
        }

        private bool IsRgbInterface(HidDevice device)
        {
            // Logitech G devices expose multiple HID interfaces
            // Accept first valid interface per product
            _ = device;
            return true;
        }

        public Task<IEnumerable<LogitechDevice>> DiscoverDevicesAsync()
        {
            var devices = _devices.Select(d => d.DeviceInfo).ToList();
            return Task.FromResult<IEnumerable<LogitechDevice>>(devices);
        }

        public async Task ApplyStaticColorAsync(LogitechDevice device, string hexColor, int brightness)
        {
            var hidDevice = _devices.FirstOrDefault(d => d.DeviceInfo.DeviceId == device.DeviceId);
            if (hidDevice == null)
            {
                _logging.Warn($"Device not found: {device.Name}");
                return;
            }

            try
            {
                var (r, g, b) = ParseHexColor(hexColor);
                // Apply brightness
                r = (byte)(r * brightness / 100);
                g = (byte)(g * brightness / 100);
                b = (byte)(b * brightness / 100);
                
                await SendColorCommand(hidDevice, r, g, b);
                _logging.Info($"Applied lighting {hexColor} (brightness {brightness}%) to {device.Name} via direct HID");
            }
            catch (Exception ex)
            {
                _logging.Error($"Failed to apply lighting to {device.Name}: {ex.Message}");
            }
        }

        public async Task ApplyBreathingEffectAsync(LogitechDevice device, string hexColor, int speed)
        {
            // Breathing effect - for now just apply static color
            // Full effect support would need device-specific protocol
            _logging.Info($"[Direct HID] Breathing effect requested on {device.Name} - applying static color");
            await ApplyStaticColorAsync(device, hexColor, 100);
        }

        public async Task ApplySpectrumEffectAsync(LogitechDevice device, int speed)
        {
            // Spectrum effect via HID++ would require device-specific commands
            // For now, log and apply a representative color
            _logging.Info($"[Direct HID] Spectrum effect requested on {device.Name} - not fully supported");
            await ApplyStaticColorAsync(device, "#FF00FF", 100); // Purple to indicate spectrum mode
        }

        public async Task ApplyFlashEffectAsync(LogitechDevice device, string hexColor, int durationMs, int intervalMs)
        {
            // Flash effect via HID++ would require device-specific timing commands
            _logging.Info($"[Direct HID] Flash effect requested on {device.Name} - applying static color");
            await ApplyStaticColorAsync(device, hexColor, 100);
        }

        public Task<int> GetDpiAsync(LogitechDevice device)
        {
            var hidDevice = _devices.FirstOrDefault(d => d.DeviceInfo.DeviceId == device.DeviceId);
            if (hidDevice != null)
            {
                return Task.FromResult(hidDevice.DeviceInfo.Status?.Dpi ?? 800);
            }
            return Task.FromResult(800);
        }

        public Task SetDpiAsync(LogitechDevice device, int dpi)
        {
            _logging.Info($"[Direct HID] DPI control not yet implemented for {device.Name}");
            return Task.CompletedTask;
        }

        private static (byte r, byte g, byte b) ParseHexColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                return (
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16)
                );
            }
            return (255, 255, 255);
        }

        private async Task SendColorCommand(LogitechHidDevice device, byte r, byte g, byte b)
        {
            // Logitech HID++ protocol for RGB control
            // Protocol varies between HID++ 1.0 and 2.0 devices
            
            try
            {
                using var stream = device.HidDevice.Open();
                
                // HID++ 2.0 color control (for newer G devices)
                // Feature: RGB Effect (0x8071)
                // This is a simplified solid color command
                
                var report = new byte[20]; // Short HID++ report
                report[0] = 0x11; // Short report ID for HID++ 2.0
                report[1] = 0xFF; // Device index (0xFF = all)
                report[2] = 0x04; // Feature index (RGB effects, varies by device)
                report[3] = 0x3E; // Function: Set RGB zone color
                report[4] = 0x00; // Zone (0 = all)
                report[5] = r;    // Red
                report[6] = g;    // Green
                report[7] = b;    // Blue
                report[8] = 0x02; // Effect type (solid)
                
                await stream.WriteAsync(report, 0, report.Length);
                
                // Alternative: Try HID++ 1.0 style for older devices
                var report10 = new byte[7];
                report10[0] = 0x10; // Short report for HID++ 1.0
                report10[1] = 0xFF; // Device
                report10[2] = 0x81; // Backlight command
                report10[3] = r;
                report10[4] = g;
                report10[5] = b;
                
                try
                {
                    await stream.WriteAsync(report10, 0, report10.Length);
                }
                catch { } // Ignore if device doesn't support 1.0
            }
            catch (Exception ex)
            {
                _logging.Warn($"HID write failed: {ex.Message} - Device may require specific HID++ version");
            }
        }

        public Task<LogitechDeviceStatus> GetDeviceStatusAsync(LogitechDevice device)
        {
            var hidDevice = _devices.FirstOrDefault(d => d.DeviceInfo.DeviceId == device.DeviceId);
            if (hidDevice != null)
            {
                // Return status with defaults - battery query via HID++ could be added later
                return Task.FromResult(new LogitechDeviceStatus
                {
                    BatteryPercent = 100,
                    Dpi = 800,
                    MaxDpi = 25600,
                    FirmwareVersion = "N/A (Direct HID)",
                    ConnectionType = "USB",
                    BrightnessPercent = 100
                });
            }
            
            return Task.FromResult(new LogitechDeviceStatus());
        }

        public void Shutdown()
        {
            _logging.Info("Logitech direct HID shut down");
            _devices.Clear();
            _initialized = false;
        }
        
        /// <summary>
        /// Internal class to track HID device with its info
        /// </summary>
        private class LogitechHidDevice
        {
            public HidDevice HidDevice { get; set; } = null!;
            public int ProductId { get; set; }
            public LogitechDevice DeviceInfo { get; set; } = null!;
        }
    }
}
