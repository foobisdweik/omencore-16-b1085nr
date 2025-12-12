using System;
using System.Management;
using System.Runtime.InteropServices;
using OmenCore.Services;

namespace OmenCore.Hardware
{
    /// <summary>
    /// HP OMEN WMI BIOS interface for fan control and system management.
    /// Communicates with HP BIOS via WMI instead of direct EC access,
    /// eliminating the need for the WinRing0 driver.
    /// 
    /// Based on HP ACPI\PNP0C14 driver interface documented by OmenMon project.
    /// </summary>
    public class HpWmiBios : IDisposable
    {
        private readonly LoggingService? _logging;
        private bool _isAvailable;
        private bool _disposed;
        private ManagementObject? _biosInterface;
        
        // Error throttling to reduce log spam
        private DateTime _lastErrorLog = DateTime.MinValue;
        private int _errorCount = 0;
        private const int ErrorLogIntervalSeconds = 30;
        
        // Track WMI command failures - disable WMI if it consistently fails
        private int _consecutiveFailures = 0;
        private const int MaxConsecutiveFailures = 3;
        private bool _wmiCommandsDisabled = false;

        // HP WMI namespaces
        private const string WmiNamespace = @"root\WMI";
        private const string HpBiosClass = "hpqBIntM";

        // BIOS command identifiers (from HP.Omen.Core.Common.PowerControl)
        private const int CMD_FAN_GET_COUNT = 0x10;
        private const int CMD_FAN_GET_LEVEL = 0x11;
        private const int CMD_FAN_SET_LEVEL = 0x12;
        private const int CMD_FAN_GET_TYPE = 0x13;
        private const int CMD_FAN_GET_TABLE = 0x17;
        private const int CMD_FAN_SET_TABLE = 0x18;
        private const int CMD_FAN_MAX_GET = 0x1A;
        private const int CMD_FAN_MAX_SET = 0x1B;
        private const int CMD_FAN_MODE_SET = 0x1C;
        private const int CMD_SYSTEM_GET_DATA = 0x28;
        private const int CMD_GPU_GET_POWER = 0x2F;
        private const int CMD_GPU_SET_POWER = 0x30;
        private const int CMD_GPU_GET_MODE = 0x32;
        private const int CMD_GPU_SET_MODE = 0x33;
        private const int CMD_TEMP_GET = 0x3C;
        private const int CMD_BACKLIGHT_GET = 0x25;
        private const int CMD_BACKLIGHT_SET = 0x26;
        private const int CMD_COLOR_GET = 0x21;
        private const int CMD_COLOR_SET = 0x22;
        private const int CMD_THROTTLE_GET = 0x3D;
        private const int CMD_IDLE_SET = 0x3E;

        // BIOS signature bytes
        private const uint BIOS_SIGNATURE = 0x4F4D4E48; // "HNMO" (OMEN Hardware)
        
        /// <summary>
        /// Fan performance mode enumeration.
        /// On Thermal Policy Version 1 systems, only Default, Performance, and Cool are used.
        /// </summary>
        public enum FanMode : byte
        {
            LegacyDefault = 0x00,
            LegacyPerformance = 0x01,
            LegacyCool = 0x02,
            LegacyQuiet = 0x03,
            Default = 0x30,        // Balanced
            Performance = 0x31,   // High performance
            Cool = 0x50           // Quiet/Cool mode
        }

        /// <summary>
        /// GPU power preset levels.
        /// </summary>
        public enum GpuPowerLevel : byte
        {
            Minimum = 0x00,  // Base TGP only
            Medium = 0x01,   // Custom TGP
            Maximum = 0x02   // Custom TGP + PPAB
        }

        /// <summary>
        /// GPU mode (not Advanced Optimus, requires reboot).
        /// </summary>
        public enum GpuMode : byte
        {
            Hybrid = 0x00,
            Discrete = 0x01,
            Optimus = 0x02
        }

        /// <summary>
        /// Thermal policy version - determines which fan modes are available.
        /// </summary>
        public enum ThermalPolicyVersion : byte
        {
            V0 = 0x00,  // Legacy devices
            V1 = 0x01   // Current devices (Default/Performance/Cool)
        }

        public bool IsAvailable => _isAvailable;
        public string Status { get; private set; } = "Not initialized";
        public ThermalPolicyVersion ThermalPolicy { get; private set; } = ThermalPolicyVersion.V1;
        public int FanCount { get; private set; } = 2;

        public HpWmiBios(LoggingService? logging = null)
        {
            _logging = logging;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // Check for HP OMEN WMI BIOS interface
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {HpBiosClass}");
                var results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    _biosInterface = obj;
                    break;
                }

                if (_biosInterface != null)
                {
                    _isAvailable = true;
                    Status = "HP WMI BIOS interface available";
                    _logging?.Info($"✓ {Status}");

                    // Query system data to get thermal policy version and fan count
                    // This also serves as a validation that WMI commands actually work
                    if (!QuerySystemData())
                    {
                        // WMI class exists but commands don't work - this system may need WinRing0
                        _isAvailable = false;
                        Status = "HP WMI BIOS found but commands not functional";
                        _logging?.Warn($"⚠️ {Status} - fan control requires WinRing0 driver on this system");
                    }
                }
                else
                {
                    // Try alternate class name
                    TryAlternateInterface();
                }
            }
            catch (ManagementException ex)
            {
                _isAvailable = false;
                Status = $"WMI query failed: {ex.Message}";
                _logging?.Info($"HP WMI BIOS: {Status}");
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                Status = $"Initialization failed: {ex.Message}";
                _logging?.Error($"HP WMI BIOS: {Status}", ex);
            }
        }

        private void TryAlternateInterface()
        {
            try
            {
                // Some HP systems use a different WMI class
                using var searcher = new ManagementObjectSearcher(WmiNamespace, "SELECT * FROM HP_BIOSMethod");
                var results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    _biosInterface = obj;
                    _isAvailable = true;
                    Status = "HP WMI BIOS interface available (alternate)";
                    _logging?.Info($"✓ {Status}");
                    break;
                }
            }
            catch
            {
                _isAvailable = false;
                Status = "HP WMI BIOS interface not found";
                _logging?.Info($"HP WMI BIOS: {Status}");
            }
        }

        /// <summary>
        /// Query system data and validate WMI commands work.
        /// Returns true if commands work, false otherwise.
        /// </summary>
        private bool QuerySystemData()
        {
            try
            {
                var result = ExecuteBiosCommand(CMD_SYSTEM_GET_DATA, new byte[4], 128);
                if (result != null && result.Length >= 9)
                {
                    ThermalPolicy = (ThermalPolicyVersion)result[3];
                    _logging?.Info($"  Thermal Policy: V{(int)ThermalPolicy}");

                    // Query fan count
                    var fanResult = ExecuteBiosCommand(CMD_FAN_GET_COUNT, new byte[4], 4);
                    if (fanResult != null && fanResult.Length >= 1)
                    {
                        FanCount = fanResult[0];
                        _logging?.Info($"  Fan Count: {FanCount}");
                    }
                    
                    return true; // Commands are working
                }
                
                _logging?.Warn("WMI BIOS: System data query returned empty result");
                return false;
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to query system data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set fan performance mode via WMI BIOS.
        /// </summary>
        public bool SetFanMode(FanMode mode)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set fan mode: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                data[0] = (byte)mode;

                var result = ExecuteBiosCommand(CMD_FAN_MODE_SET, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ Fan mode set to: {mode}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set fan mode: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Set fan speed levels directly (0-255, in krpm units).
        /// </summary>
        public bool SetFanLevel(byte fan1Level, byte fan2Level)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set fan level: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                data[0] = fan1Level;
                data[1] = fan2Level;

                var result = ExecuteBiosCommand(CMD_FAN_SET_LEVEL, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ Fan levels set: CPU={fan1Level}, GPU={fan2Level}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set fan level: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Get current fan speed levels.
        /// </summary>
        public (byte fan1, byte fan2)? GetFanLevel()
        {
            if (!_isAvailable) return null;

            try
            {
                var result = ExecuteBiosCommand(CMD_FAN_GET_LEVEL, new byte[4], 4);
                if (result != null && result.Length >= 2)
                {
                    return (result[0], result[1]);
                }
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to get fan level: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Enable or disable maximum fan speed mode.
        /// </summary>
        public bool SetFanMax(bool enabled)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set fan max: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                data[0] = (byte)(enabled ? 1 : 0);

                var result = ExecuteBiosCommand(CMD_FAN_MAX_SET, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ Fan max mode: {(enabled ? "enabled" : "disabled")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set fan max: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Get current fan max mode status.
        /// </summary>
        public bool? GetFanMax()
        {
            if (!_isAvailable) return null;

            try
            {
                var result = ExecuteBiosCommand(CMD_FAN_MAX_GET, new byte[4], 4);
                if (result != null && result.Length >= 1)
                {
                    return result[0] != 0;
                }
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to get fan max status: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Get BIOS temperature sensor reading.
        /// Note: This tends to read lower than EC sensors.
        /// </summary>
        public int? GetTemperature()
        {
            if (!_isAvailable) return null;

            try
            {
                var result = ExecuteBiosCommand(CMD_TEMP_GET, new byte[4], 4);
                if (result != null && result.Length >= 1)
                {
                    return result[0];
                }
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to get temperature: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Set GPU power preset.
        /// </summary>
        public bool SetGpuPower(GpuPowerLevel level)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set GPU power: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                // GpuCustomTgp, GpuPpab, GpuDState, PeakTemperature
                switch (level)
                {
                    case GpuPowerLevel.Minimum:
                        data[0] = 0; // CustomTgp off
                        data[1] = 0; // PPAB off
                        break;
                    case GpuPowerLevel.Medium:
                        data[0] = 1; // CustomTgp on
                        data[1] = 0; // PPAB off
                        break;
                    case GpuPowerLevel.Maximum:
                        data[0] = 1; // CustomTgp on
                        data[1] = 1; // PPAB on
                        break;
                }

                var result = ExecuteBiosCommand(CMD_GPU_SET_POWER, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ GPU power set to: {level}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set GPU power: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Get current GPU power settings.
        /// </summary>
        public (bool customTgp, bool ppab, int dState)? GetGpuPower()
        {
            if (!_isAvailable) return null;

            try
            {
                var result = ExecuteBiosCommand(CMD_GPU_GET_POWER, new byte[4], 4);
                if (result != null && result.Length >= 3)
                {
                    return (result[0] != 0, result[1] != 0, result[2]);
                }
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to get GPU power: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Get GPU mode (Hybrid/Discrete/Optimus).
        /// </summary>
        public GpuMode? GetGpuMode()
        {
            if (!_isAvailable) return null;

            try
            {
                var result = ExecuteBiosCommand(CMD_GPU_GET_MODE, new byte[4], 4);
                if (result != null && result.Length >= 1)
                {
                    return (GpuMode)result[0];
                }
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to get GPU mode: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Set GPU mode (requires reboot to take effect).
        /// </summary>
        public bool SetGpuMode(GpuMode mode)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set GPU mode: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                data[0] = (byte)mode;

                var result = ExecuteBiosCommand(CMD_GPU_SET_MODE, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ GPU mode set to: {mode} (reboot required)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set GPU mode: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Set keyboard backlight on/off.
        /// </summary>
        public bool SetBacklight(bool enabled)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set backlight: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                data[0] = (byte)(enabled ? 0xE4 : 0x64);

                var result = ExecuteBiosCommand(CMD_BACKLIGHT_SET, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ Keyboard backlight: {(enabled ? "on" : "off")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set backlight: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Set idle mode (affects power management).
        /// </summary>
        public bool SetIdleMode(bool enabled)
        {
            if (!_isAvailable)
            {
                _logging?.Warn("Cannot set idle mode: WMI BIOS not available");
                return false;
            }

            try
            {
                var data = new byte[4];
                data[0] = (byte)(enabled ? 1 : 0);

                var result = ExecuteBiosCommand(CMD_IDLE_SET, data, 4);
                if (result != null)
                {
                    _logging?.Info($"✓ Idle mode: {(enabled ? "enabled" : "disabled")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set idle mode: {ex.Message}", ex);
            }
            return false;
        }

        /// <summary>
        /// Execute a BIOS command via WMI using embedded WMI object format.
        /// HP WMI expects hpqBDataIn embedded object, not a raw byte array.
        /// </summary>
        private byte[]? ExecuteBiosCommand(int command, byte[] inputData, int outputSize)
        {
            if (_biosInterface == null || _wmiCommandsDisabled)
                return null;

            // Try embedded object format first, fall back to legacy if needed
            var result = ExecuteBiosCommandEmbedded(command, inputData, outputSize);
            if (result != null)
            {
                _consecutiveFailures = 0; // Reset on success
                return result;
            }
                
            // Fallback to legacy raw byte array format
            result = ExecuteBiosCommandLegacy(command, inputData, outputSize);
            if (result != null)
            {
                _consecutiveFailures = 0;
                return result;
            }
            
            // Track failures and disable if too many
            _consecutiveFailures++;
            if (_consecutiveFailures >= MaxConsecutiveFailures && !_wmiCommandsDisabled)
            {
                _wmiCommandsDisabled = true;
                _logging?.Warn($"WMI BIOS commands disabled after {MaxConsecutiveFailures} consecutive failures. " +
                    "This HP system may require different command format or WinRing0 driver for fan control.");
            }
            
            return null;
        }
        
        /// <summary>
        /// Execute BIOS command using embedded WMI object (newer HP systems).
        /// </summary>
        private byte[]? ExecuteBiosCommandEmbedded(int command, byte[] inputData, int outputSize)
        {
            if (_biosInterface == null)
                return null;

            try
            {
                // Create the embedded hpqBDataIn WMI object using the proper path
                using var inputClass = new ManagementClass(WmiNamespace, "hpqBDataIn", null);
                var inputInstance = inputClass.CreateInstance();
                
                // Build data buffer (128 bytes for hpqBIOSInt128)
                var dataBuffer = new byte[128];
                if (inputData != null && inputData.Length > 0)
                {
                    Array.Copy(inputData, 0, dataBuffer, 0, Math.Min(inputData.Length, 128));
                }

                // Set properties according to HP WMI schema
                inputInstance["Command"] = (uint)command;
                inputInstance["CommandType"] = (uint)0;  // Standard command
                inputInstance["Size"] = (uint)(inputData?.Length ?? 0);
                inputInstance["hpqBData"] = dataBuffer;
                inputInstance["Sign"] = new byte[256];  // Signature (not used but required)

                // Get method parameters
                var inParams = _biosInterface.GetMethodParameters("hpqBIOSInt128");
                if (inParams == null)
                {
                    return null;
                }
                
                // The InData parameter expects the embedded object
                // ManagementObject.InvokeMethod with embedded objects requires setting the embedded instance
                inParams["InData"] = inputInstance;
                
                var outParams = _biosInterface.InvokeMethod("hpqBIOSInt128", inParams, null);
                if (outParams != null)
                {
                    // Try to get output as embedded object first
                    var outDataObj = outParams["OutData"] as ManagementBaseObject;
                    if (outDataObj != null)
                    {
                        // hpqBDataOut128 has 'Data' property (not 'hpqBData')
                        var outputBuffer = outDataObj["Data"] as byte[];
                        var returnCode = Convert.ToInt32(outDataObj["rwReturnCode"] ?? -1);
                        
                        if (returnCode == 0 && outputBuffer != null && outputBuffer.Length > 0)
                        {
                            _errorCount = 0; // Reset error count on success
                            var result = new byte[Math.Min(outputSize, outputBuffer.Length)];
                            Array.Copy(outputBuffer, 0, result, 0, result.Length);
                            return result;
                        }
                    }
                }
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidParameter ||
                                                  ex.ErrorCode == ManagementStatus.TypeMismatch)
            {
                // Expected for systems that don't support embedded object format
                // Will fall through to legacy format - don't log this
            }
            catch (ManagementException ex)
            {
                LogThrottledError($"WMI embedded method failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogThrottledError($"BIOS embedded command failed: {ex.Message}");
            }

            return null;
        }
        
        /// <summary>
        /// Log errors with throttling to prevent log spam during repeated failures.
        /// </summary>
        private void LogThrottledError(string message)
        {
            _errorCount++;
            var now = DateTime.Now;
            if ((now - _lastErrorLog).TotalSeconds >= ErrorLogIntervalSeconds)
            {
                if (_errorCount > 1)
                {
                    _logging?.Warn($"{message} (repeated {_errorCount}x in last {ErrorLogIntervalSeconds}s)");
                }
                else
                {
                    _logging?.Warn(message);
                }
                _lastErrorLog = now;
                _errorCount = 0;
            }
        }
        
        /// <summary>
        /// Legacy BIOS command format for older HP systems that use raw byte arrays.
        /// </summary>
        private byte[]? ExecuteBiosCommandLegacy(int command, byte[] inputData, int outputSize)
        {
            if (_biosInterface == null)
                return null;

            try
            {
                // Build input buffer: 4-byte signature + 4-byte command + data
                var inputBuffer = new byte[128];
                
                // HP BIOS signature "HNMO"
                inputBuffer[0] = 0x48; // H
                inputBuffer[1] = 0x4E; // N
                inputBuffer[2] = 0x4D; // M
                inputBuffer[3] = 0x4F; // O
                
                // Command ID
                inputBuffer[4] = (byte)(command & 0xFF);
                inputBuffer[5] = (byte)((command >> 8) & 0xFF);
                inputBuffer[6] = (byte)((command >> 16) & 0xFF);
                inputBuffer[7] = (byte)((command >> 24) & 0xFF);

                // Copy input data
                if (inputData != null && inputData.Length > 0)
                {
                    Array.Copy(inputData, 0, inputBuffer, 8, Math.Min(inputData.Length, 120));
                }

                var inParams = _biosInterface.GetMethodParameters("hpqBIOSInt128");
                if (inParams != null)
                {
                    inParams["InData"] = inputBuffer;
                    
                    var outParams = _biosInterface.InvokeMethod("hpqBIOSInt128", inParams, null);
                    if (outParams != null)
                    {
                        var outputBuffer = outParams["OutData"] as byte[];
                        if (outputBuffer != null && outputBuffer.Length >= 8)
                        {
                            var returnCode = BitConverter.ToInt32(outputBuffer, 0);
                            if (returnCode == 0)
                            {
                                var result = new byte[outputSize];
                                Array.Copy(outputBuffer, 4, result, 0, Math.Min(outputSize, outputBuffer.Length - 4));
                                return result;
                            }
                        }
                    }
                }
            }
            catch (ManagementException)
            {
                // Silently fail - will be tracked by ExecuteBiosCommand
            }
            catch (Exception)
            {
                // Silently fail - will be tracked by ExecuteBiosCommand
            }

            return null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _biosInterface?.Dispose();
                _disposed = true;
            }
        }
    }
}
