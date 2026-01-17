using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmenCore.Hardware;
using OmenCore.Models;

namespace OmenCore.Services
{
    /// <summary>
    /// Results from applying and verifying a fan speed change.
    /// </summary>
    public class FanApplyResult
    {
        public int FanIndex { get; set; }
        public string FanName { get; set; } = "";
        public int RequestedPercent { get; set; }
        public int AppliedLevel { get; set; }
        public int ActualRpmBefore { get; set; }
        public int ActualRpmAfter { get; set; }
        public int ExpectedRpm { get; set; }
        public bool WmiCallSucceeded { get; set; }
        public bool VerificationPassed { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// True if the fan speed change was successfully applied and verified.
        /// </summary>
        public bool Success => WmiCallSucceeded && VerificationPassed;
        
        /// <summary>
        /// Percentage difference from expected RPM (for diagnostics).
        /// </summary>
        public double DeviationPercent => ExpectedRpm > 0 
            ? Math.Abs(ActualRpmAfter - ExpectedRpm) / (double)ExpectedRpm * 100 
            : 0;
    }

    /// <summary>
    /// Provides closed-loop verification for fan control commands.
    /// After setting a fan speed, reads back the actual RPM to verify it was applied.
    /// 
    /// Enhanced with:
    /// - Multi-sample verification (reads RPM multiple times to ensure stability)
    /// - Auto-revert on failure (restores previous state if verification fails)
    /// - Detailed diagnostics (logs suggestion to switch backend if commands ineffective)
    /// - Retry logic for transient failures
    /// 
    /// This addresses the issue where "Requested % doesn't match actual fan speed".
    /// </summary>
    public class FanVerificationService : IFanVerificationService
    {
        private readonly HpWmiBios? _wmiBios;
        private readonly FanService? _fanService;
        private readonly LoggingService _logging;
        
        // Verification parameters
        private const int VerificationRetries = 2;          // Retry verification up to 2 times
        private const int VerificationSamples = 3;          // Take 3 RPM samples and average
        private const int SampleDelayMs = 300;              // Wait 300ms between samples
        private const int MaxLevel = 55;  // HP uses 55 as max on most models
        private const int MinRpm = 0;
        private const int MaxRpm = 5500;  // Typical max RPM
        
        // Verification timing
        private const int FanResponseDelayMs = 2500;  // Fans have mechanical inertia
        private const int RetryDelayMs = 2000;
        private const double RpmTolerance = 0.20;  // 20% tolerance for RPM verification
        
        public FanVerificationService(HpWmiBios? wmiBios, FanService? fanService, LoggingService logging)
        {
            _wmiBios = wmiBios;
            _fanService = fanService;
            _logging = logging;
        }
        
        /// <summary>
        /// Check if verification is available.
        /// </summary>
        public bool IsAvailable => (_wmiBios?.IsAvailable ?? false) || (_fanService != null);
        
        /// <summary>
        /// Get current RPM from fan telemetry.
        /// </summary>
        private int GetCurrentRpm(int fanIndex)
        {
            if (_fanService?.FanTelemetry != null && _fanService.FanTelemetry.Count > fanIndex)
            {
                return _fanService.FanTelemetry[fanIndex].SpeedRpm;
            }
            return 0;
        }
        
        /// <summary>
        /// Get fan name from telemetry.
        /// </summary>
        private string GetFanName(int fanIndex)
        {
            if (_fanService?.FanTelemetry != null && _fanService.FanTelemetry.Count > fanIndex)
            {
                return _fanService.FanTelemetry[fanIndex].Name;
            }
            return fanIndex == 0 ? "CPU Fan" : "GPU Fan";
        }
        
        /// <summary>
        /// Apply a fan speed and verify it was actually applied by reading back RPM.
        /// </summary>
        /// <param name="fanIndex">Fan index (0 for CPU, 1 for GPU typically)</param>
        /// <param name="targetPercent">Target fan speed percentage (0-100)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Result containing applied values and verification status</returns>
        public async Task<FanApplyResult> ApplyAndVerifyFanSpeedAsync(
            int fanIndex, 
            int targetPercent,
            CancellationToken ct = default)
        {
            var startTime = DateTime.Now;
            var result = new FanApplyResult
            {
                FanIndex = fanIndex,
                FanName = GetFanName(fanIndex),
                RequestedPercent = targetPercent,
                ExpectedRpm = PercentToExpectedRpm(targetPercent)
            };
            
            if (_wmiBios == null || !_wmiBios.IsAvailable)
            {
                result.ErrorMessage = "WMI BIOS not available";
                result.Duration = DateTime.Now - startTime;
                return result;
            }
            
            try
            {
                // Read current state before change (from telemetry)
                result.ActualRpmBefore = GetCurrentRpm(fanIndex);
                _logging.Info($"Fan {fanIndex} ({result.FanName}) before: {result.ActualRpmBefore} RPM");
                
                // Convert percent to level
                result.AppliedLevel = PercentToLevel(targetPercent);
                
                // For 100%, use SetFanMax which achieves true maximum RPM
                // SetFanLevel(55) may be capped by BIOS on some models
                if (targetPercent >= 100)
                {
                    result.WmiCallSucceeded = _wmiBios.SetFanMax(true);
                    if (result.WmiCallSucceeded)
                    {
                        _logging.Info($"Fan {fanIndex} set to MAX (100%) via SetFanMax");
                    }
                    else
                    {
                        // Fallback to SetFanLevel
                        result.WmiCallSucceeded = _wmiBios.SetFanLevel(55, 55);
                        _logging.Info($"Fan {fanIndex} set to 100% via SetFanLevel(55) fallback");
                    }
                }
                else
                {
                    // For <100%, disable max mode first (in case it was enabled)
                    _wmiBios.SetFanMax(false);
                    
                    // Apply the fan level (need both fans)
                    byte fan1Level = (byte)(fanIndex == 0 ? result.AppliedLevel : 0);
                    byte fan2Level = (byte)(fanIndex == 1 ? result.AppliedLevel : 0);
                    
                    // Get current levels for the other fan to preserve it
                    var currentLevels = _wmiBios.GetFanLevel();
                    if (currentLevels.HasValue)
                    {
                        if (fanIndex == 0)
                            fan2Level = currentLevels.Value.fan2;
                        else
                            fan1Level = currentLevels.Value.fan1;
                    }
                    
                    result.WmiCallSucceeded = _wmiBios.SetFanLevel(fan1Level, fan2Level);
                    
                    if (result.WmiCallSucceeded)
                    {
                        _logging.Info($"Fan {fanIndex} set to level {result.AppliedLevel} ({targetPercent}%)");
                    }
                }
                
                // Check if WMI call failed
                if (!result.WmiCallSucceeded)
                {
                    _logging.Warn($"WMI fan control failed for fan {fanIndex}, level {result.AppliedLevel}");
                    result.ErrorMessage = "WMI call returned false";
                    result.Duration = DateTime.Now - startTime;
                    return result;
                }
                
                // Wait for fan to respond (fans have mechanical inertia)
                await Task.Delay(FanResponseDelayMs, ct);
                
                // Multi-sample verification: take several RPM readings and average them
                int totalAttempts = 0;
                for (int attempt = 0; attempt <= VerificationRetries; attempt++)
                {
                    totalAttempts = attempt + 1;
                    
                    // Take multiple samples and average them for stability
                    var rpmSamples = new int[VerificationSamples];
                    for (int i = 0; i < VerificationSamples; i++)
                    {
                        rpmSamples[i] = GetCurrentRpm(fanIndex);
                        if (i < VerificationSamples - 1)
                            await Task.Delay(SampleDelayMs, ct);
                    }
                    
                    // Use average RPM for verification
                    result.ActualRpmAfter = (int)rpmSamples.Average();
                    
                    _logging.Info($"Fan {fanIndex} RPM samples: [{string.Join(", ", rpmSamples)}], Average: {result.ActualRpmAfter} RPM");
                    
                    // Verify the change
                    result.VerificationPassed = VerifyRpm(result);
                    
                    if (result.VerificationPassed)
                    {
                        _logging.Info($"✓ Fan {fanIndex} verified: {result.ActualRpmAfter} RPM (expected ~{result.ExpectedRpm}, deviation {result.DeviationPercent:F1}%)");
                        break;
                    }
                    else if (attempt < VerificationRetries)
                    {
                        _logging.Warn($"Fan {fanIndex} verification attempt {attempt + 1} failed: Expected ~{result.ExpectedRpm} RPM, got {result.ActualRpmAfter} RPM ({result.DeviationPercent:F1}% deviation). Retrying...");
                        await Task.Delay(RetryDelayMs, ct);
                    }
                }
                
                // Final diagnostic message if verification still failed
                if (!result.VerificationPassed)
                {
                    _logging.Error($"Fan {fanIndex} verification failed after {totalAttempts} attempts: expected ~{result.ExpectedRpm} RPM, got {result.ActualRpmAfter} RPM");
                    result.ErrorMessage = $"RPM verification failed after {totalAttempts} attempts: expected ~{result.ExpectedRpm}, got {result.ActualRpmAfter}";
                    
                    // Track failure for diagnostics
                    _logging.Warn($"⚠️ Fan {fanIndex} commands appear ineffective. This model may not support WMI-based control. Consider using OGH proxy backend if available, or verify EC register mapping.");
                    result.ErrorMessage += " [TIP: Consider switching to OGH proxy backend for this model]";
                    
                    // Auto-revert attempt: set fans back to auto mode
                    _logging.Warn($"Attempting to restore auto control due to verification failure...");
                    try
                    {
                        _wmiBios?.SetFanMode(HpWmiBios.FanMode.Default);
                    }
                    catch { /* Ignore revert errors */ }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logging.Error($"Fan verification exception: {ex.Message}", ex);
            }
            
            result.Duration = DateTime.Now - startTime;
            return result;
        }
        
        /// <summary>
        /// Read current fan speed without making changes (diagnostic tool).
        /// </summary>
        public (int rpm, int level) GetCurrentFanState(int fanIndex)
        {
            var rpm = GetCurrentRpm(fanIndex);
            
            // Get level from WMI BIOS if available
            int level = 0;
            if (_wmiBios?.IsAvailable == true)
            {
                var levels = _wmiBios.GetFanLevel();
                if (levels.HasValue)
                {
                    level = fanIndex == 0 ? levels.Value.fan1 : levels.Value.fan2;
                }
            }
            else
            {
                // Estimate from RPM
                level = RpmToLevel(rpm);
            }
            
            return (rpm, level);
        }
        
        /// <summary>
        /// Read current fan speed with RPM source information.
        /// </summary>
        public (int rpm, int level, RpmSource source) GetCurrentFanStateWithSource(int fanIndex)
        {
            var (rpm, level) = GetCurrentFanState(fanIndex);
            
            // Get source from telemetry
            RpmSource source = RpmSource.Unknown;
            if (_fanService?.FanTelemetry != null && _fanService.FanTelemetry.Count > fanIndex)
            {
                source = _fanService.FanTelemetry[fanIndex].RpmSource;
            }
            
            return (rpm, level, source);
        }
        
        /// <summary>
        /// Verify fan reading by checking it multiple times.
        /// </summary>
        public async Task<(int avg, int min, int max)> GetStableFanRpmAsync(int fanIndex, int samples = 3, CancellationToken ct = default)
        {
            int sum = 0;
            int min = int.MaxValue;
            int max = int.MinValue;
            
            for (int i = 0; i < samples; i++)
            {
                var rpm = GetCurrentRpm(fanIndex);
                sum += rpm;
                min = Math.Min(min, rpm);
                max = Math.Max(max, rpm);
                
                if (i < samples - 1)
                    await Task.Delay(500, ct);
            }
            
            return (sum / samples, min == int.MaxValue ? 0 : min, max == int.MinValue ? 0 : max);
        }
        
        #region Conversion Helpers
        
        /// <summary>
        /// Convert percentage (0-100) to HP fan level.
        /// HP typically uses levels 0-55, not 0-100.
        /// </summary>
        private int PercentToLevel(int percent)
        {
            percent = Math.Clamp(percent, 0, 100);
            // Linear mapping: 0% -> 0, 100% -> MaxLevel
            return (int)Math.Round(percent / 100.0 * MaxLevel);
        }
        
        /// <summary>
        /// Convert expected percentage to expected RPM.
        /// This is an approximation - real calibration data would be better.
        /// </summary>
        private int PercentToExpectedRpm(int percent)
        {
            percent = Math.Clamp(percent, 0, 100);
            // Assume linear relationship (could be calibrated per-model)
            // Most laptops: 0% = 0 RPM, 100% = ~5000-6000 RPM
            if (percent == 0) return 0;
            return (int)(MinRpm + (MaxRpm - MinRpm) * (percent / 100.0));
        }
        
        /// <summary>
        /// Estimate level from RPM (inverse of apply).
        /// </summary>
        private int RpmToLevel(int rpm)
        {
            if (rpm <= 0) return 0;
            var percent = Math.Min(100, (rpm / (double)MaxRpm) * 100);
            return (int)Math.Round(percent / 100.0 * MaxLevel);
        }
        
        /// <summary>
        /// Check if the actual RPM is within tolerance of expected.
        /// </summary>
        private bool VerifyRpm(FanApplyResult result)
        {
            // If requesting 0%, fan should be off or very low
            if (result.RequestedPercent == 0)
            {
                return result.ActualRpmAfter < 1000;  // Should be nearly stopped
            }
            
            // For non-zero, check within tolerance
            var tolerance = result.ExpectedRpm * RpmTolerance;
            return Math.Abs(result.ActualRpmAfter - result.ExpectedRpm) <= tolerance;
        }
        
        #endregion
    }
}
