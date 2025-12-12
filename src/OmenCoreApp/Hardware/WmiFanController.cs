using System;
using System.Collections.Generic;
using System.Linq;
using OmenCore.Models;
using OmenCore.Services;

namespace OmenCore.Hardware
{
    /// <summary>
    /// WMI-based fan controller for HP OMEN/Victus laptops.
    /// Uses HP WMI BIOS interface instead of direct EC access.
    /// This eliminates the need for the WinRing0 driver.
    /// </summary>
    public class WmiFanController : IDisposable
    {
        private readonly HpWmiBios _wmiBios;
        private readonly LibreHardwareMonitorImpl _hwMonitor;
        private readonly LoggingService? _logging;
        private bool _disposed;

        // Manual fan control state
        private HpWmiBios.FanMode _lastMode = HpWmiBios.FanMode.Default;

        public bool IsAvailable => _wmiBios.IsAvailable;
        public string Status => _wmiBios.Status;
        public int FanCount => _wmiBios.FanCount;
        
        /// <summary>
        /// Indicates if manual fan control is currently active (vs automatic BIOS control).
        /// </summary>
        public bool IsManualControlActive { get; private set; }

        public WmiFanController(LibreHardwareMonitorImpl hwMonitor, LoggingService? logging = null)
        {
            _hwMonitor = hwMonitor;
            _logging = logging;
            _wmiBios = new HpWmiBios(logging);
        }

        /// <summary>
        /// Apply a fan preset using WMI BIOS commands.
        /// </summary>
        public bool ApplyPreset(FanPreset preset)
        {
            if (!IsAvailable)
            {
                _logging?.Warn("Cannot apply preset: WMI BIOS not available");
                return false;
            }

            try
            {
                // Map preset to fan mode
                var mode = MapPresetToFanMode(preset);
                
                if (_wmiBios.SetFanMode(mode))
                {
                    _lastMode = mode;
                    IsManualControlActive = false;
                    
                    // Apply GPU power settings if needed
                    ApplyGpuPowerFromPreset(preset);
                    
                    _logging?.Info($"✓ Applied preset: {preset.Name} (Mode: {mode})");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to apply preset: {ex.Message}", ex);
            }

            return false;
        }

        /// <summary>
        /// Apply a custom fan curve by setting direct fan levels.
        /// </summary>
        public bool ApplyCustomCurve(IEnumerable<FanCurvePoint> curve)
        {
            if (!IsAvailable)
            {
                _logging?.Warn("Cannot apply custom curve: WMI BIOS not available");
                return false;
            }

            try
            {
                var curveList = curve.OrderBy(p => p.TemperatureC).ToList();
                if (!curveList.Any())
                {
                    _logging?.Warn("Empty curve provided");
                    return false;
                }

                // Get current temperature to determine fan level
                var cpuTemp = (int)_hwMonitor.GetCpuTemperature();
                var gpuTemp = (int)_hwMonitor.GetGpuTemperature();
                var maxTemp = Math.Max(cpuTemp, gpuTemp);

                // Find appropriate curve point
                var targetPoint = curveList.LastOrDefault(p => p.TemperatureC <= maxTemp) 
                                  ?? curveList.First();

                // Convert percentage to krpm (0-255 maps to ~0-5.5 krpm)
                // Typical: 0% = 0, 50% = ~2.5 krpm (25), 100% = ~5.5 krpm (55)
                byte fanLevel = (byte)(targetPoint.FanPercent * 55 / 100);

                if (_wmiBios.SetFanLevel(fanLevel, fanLevel))
                {
                    IsManualControlActive = true;
                    _logging?.Info($"✓ Custom curve applied: {targetPoint.FanPercent}% @ {maxTemp}°C (Level: {fanLevel})");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to apply custom curve: {ex.Message}", ex);
            }

            return false;
        }

        /// <summary>
        /// Set fan speed as a percentage (0-100).
        /// </summary>
        public bool SetFanSpeed(int percent)
        {
            if (!IsAvailable)
            {
                _logging?.Warn("Cannot set fan speed: WMI BIOS not available");
                return false;
            }

            percent = Math.Clamp(percent, 0, 100);

            try
            {
                // Convert percentage to krpm level
                byte fanLevel = (byte)(percent * 55 / 100);
                
                if (_wmiBios.SetFanLevel(fanLevel, fanLevel))
                {
                    IsManualControlActive = true;
                    _logging?.Info($"✓ Fan speed set to {percent}% (Level: {fanLevel})");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to set fan speed: {ex.Message}", ex);
            }

            return false;
        }

        /// <summary>
        /// Enable maximum fan speed mode.
        /// </summary>
        public bool SetMaxFanSpeed(bool enabled)
        {
            if (!IsAvailable)
            {
                _logging?.Warn("Cannot set max fan: WMI BIOS not available");
                return false;
            }

            return _wmiBios.SetFanMax(enabled);
        }

        /// <summary>
        /// Set performance mode (Default/Performance/Cool).
        /// </summary>
        public bool SetPerformanceMode(string modeName)
        {
            if (!IsAvailable)
            {
                _logging?.Warn("Cannot set performance mode: WMI BIOS not available");
                return false;
            }

            var nameLower = modeName?.ToLowerInvariant() ?? "default";
            
            HpWmiBios.FanMode fanMode;
            if (nameLower.Contains("performance") || nameLower.Contains("turbo") || nameLower.Contains("gaming"))
            {
                fanMode = HpWmiBios.FanMode.Performance;
            }
            else if (nameLower.Contains("quiet") || nameLower.Contains("silent") || nameLower.Contains("cool") || nameLower.Contains("battery"))
            {
                fanMode = HpWmiBios.FanMode.Cool;
            }
            else
            {
                fanMode = HpWmiBios.FanMode.Default;
            }

            if (_wmiBios.SetFanMode(fanMode))
            {
                _lastMode = fanMode;
                IsManualControlActive = false;
                _logging?.Info($"✓ Performance mode set: {modeName} → {fanMode}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set performance mode using PerformanceMode object.
        /// </summary>
        public bool SetPerformanceMode(PerformanceMode mode)
        {
            return SetPerformanceMode(mode?.Name ?? "Default");
        }

        /// <summary>
        /// Restore automatic fan control.
        /// </summary>
        public bool RestoreAutoControl()
        {
            if (!IsAvailable)
            {
                return false;
            }

            try
            {
                // Set default mode to restore automatic control
                if (_wmiBios.SetFanMode(HpWmiBios.FanMode.Default))
                {
                    IsManualControlActive = false;
                    _lastMode = HpWmiBios.FanMode.Default;
                    _logging?.Info("✓ Restored automatic fan control");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to restore auto control: {ex.Message}", ex);
            }

            return false;
        }

        /// <summary>
        /// Read current fan telemetry data.
        /// </summary>
        public IEnumerable<FanTelemetry> ReadFanSpeeds()
        {
            var fans = new List<FanTelemetry>();

            // Get fan speeds from hardware monitor
            var fanSpeeds = _hwMonitor.GetFanSpeeds();
            int index = 0;

            foreach (var (name, rpm) in fanSpeeds)
            {
                var fanLevel = _wmiBios.GetFanLevel();
                int levelPercent = 0;
                
                if (fanLevel.HasValue)
                {
                    levelPercent = index == 0 
                        ? fanLevel.Value.fan1 * 100 / 55 
                        : fanLevel.Value.fan2 * 100 / 55;
                }
                else
                {
                    // Estimate from RPM
                    levelPercent = EstimateDutyFromRpm((int)rpm);
                }

                fans.Add(new FanTelemetry
                {
                    Name = name,
                    SpeedRpm = (int)rpm,
                    DutyCyclePercent = Math.Clamp(levelPercent, 0, 100),
                    Temperature = index == 0 ? _hwMonitor.GetCpuTemperature() : _hwMonitor.GetGpuTemperature()
                });
                index++;
            }

            // Fallback if no fans detected
            if (fans.Count == 0)
            {
                var biosTemp = _wmiBios.GetTemperature();
                var cpuTemp = _hwMonitor.GetCpuTemperature();
                var gpuTemp = _hwMonitor.GetGpuTemperature();

                fans.Add(new FanTelemetry 
                { 
                    Name = "CPU Fan", 
                    SpeedRpm = 0, 
                    DutyCyclePercent = 0, 
                    Temperature = cpuTemp > 0 ? cpuTemp : biosTemp ?? 0
                });
                
                fans.Add(new FanTelemetry 
                { 
                    Name = "GPU Fan", 
                    SpeedRpm = 0, 
                    DutyCyclePercent = 0, 
                    Temperature = gpuTemp
                });
            }

            return fans;
        }

        /// <summary>
        /// Get current GPU power settings.
        /// </summary>
        public (bool customTgp, bool ppab, int dState)? GetGpuPowerSettings()
        {
            return _wmiBios.GetGpuPower();
        }

        /// <summary>
        /// Set GPU power level.
        /// </summary>
        public bool SetGpuPower(HpWmiBios.GpuPowerLevel level)
        {
            return _wmiBios.SetGpuPower(level);
        }

        /// <summary>
        /// Get current GPU mode.
        /// </summary>
        public HpWmiBios.GpuMode? GetGpuMode()
        {
            return _wmiBios.GetGpuMode();
        }

        private HpWmiBios.FanMode MapPresetToFanMode(FanPreset preset)
        {
            // Map based on preset characteristics
            var maxFan = preset.Curve.Any() ? preset.Curve.Max(p => p.FanPercent) : 50;
            var avgFan = preset.Curve.Any() ? preset.Curve.Average(p => p.FanPercent) : 50;

            // Check preset name for hints
            var nameLower = preset.Name.ToLowerInvariant();
            
            if (nameLower.Contains("quiet") || nameLower.Contains("silent") || nameLower.Contains("cool"))
            {
                return HpWmiBios.FanMode.Cool;
            }
            
            if (nameLower.Contains("performance") || nameLower.Contains("turbo") || nameLower.Contains("gaming"))
            {
                return HpWmiBios.FanMode.Performance;
            }

            // Use curve characteristics
            if (avgFan < 40)
            {
                return HpWmiBios.FanMode.Cool;
            }
            
            if (avgFan > 70 || maxFan > 90)
            {
                return HpWmiBios.FanMode.Performance;
            }

            return HpWmiBios.FanMode.Default;
        }

        private void ApplyGpuPowerFromPreset(FanPreset preset)
        {
            var nameLower = preset.Name.ToLowerInvariant();
            
            if (nameLower.Contains("performance") || nameLower.Contains("turbo") || nameLower.Contains("gaming"))
            {
                _wmiBios.SetGpuPower(HpWmiBios.GpuPowerLevel.Maximum);
            }
            else if (nameLower.Contains("quiet") || nameLower.Contains("silent") || nameLower.Contains("battery"))
            {
                _wmiBios.SetGpuPower(HpWmiBios.GpuPowerLevel.Minimum);
            }
            else
            {
                _wmiBios.SetGpuPower(HpWmiBios.GpuPowerLevel.Medium);
            }
        }

        private int EstimateDutyFromRpm(int rpm)
        {
            if (rpm == 0) return 0;
            
            const int minRpm = 1500;
            const int maxRpm = 6000;
            
            return Math.Clamp((rpm - minRpm) * 100 / (maxRpm - minRpm), 0, 100);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                RestoreAutoControl();
                _wmiBios.Dispose();
                _disposed = true;
            }
        }
    }
}
