# OmenCore v1.5 Roadmap

**Target Release:** Q3 2026 (July)  
**Status:** Planning  
**Last Updated:** December 17, 2025

---

## Overview

This document outlines planned improvements for OmenCore v1.5, based on a comprehensive audit of the codebase. Major focus areas:

1. **Keyboard RGB Complete Rework** - Current implementation broken on most models
2. **Undervolting System Rework** - Capability-based detection, remove WinRing0 dependency
3. **Fan Control Improvements** - Closed-loop verification, calibration, fix UI glitches
4. **UI/UX Overhaul** - Remove duplicate displays, improve clarity
5. **Code Architecture Refactoring** - Split god classes, improve DI
6. **3rd Party RGB Expansion** - Razer, SteelSeries, OpenRGB integration
7. **Installer & Auto-Update Polish** - Code signing, rollback mechanism, remove WinRing0
8. **Settings Persistence** - Fix TCC/GPU boost not surviving reboot
9. **Power Source Detection** - Fix AC/battery detection issues

---

## Known Bugs (v1.4.0-beta3)

**Must fix before v1.5 stable:**

| # | Bug | Severity | Root Cause |
|---|-----|----------|------------|
| 1 | Fan curve sliders move awkwardly without user clicking | High | UI binding/event handling issue |
| 2 | AC/Battery detection not working | High | `PowerAutomationService` not reading power state correctly |
| 3 | App doesn't start with Windows despite task creation | High | Scheduled task elevation or path issues |
| 4 | TCC Offset resets to 100Â°C on restart | Medium | Settings not persisted/restored on startup |
| 5 | GPU Power Boost resets to Minimum on restart | Medium | Same as above - startup restore failing |
| 6 | CPU Undervolt fails with MSR 0x152 write error | Expected | WinRing0 blocked by Secure Boot (by design) |
| 7 | Fan control glitchy - 80% power causing 98Â°C CPU temp | High | Requested % not matching actual fan behavior |
| 8 | Monitoring values glitchy | Medium | LibreHardwareMonitor polling/caching issues |
| 9 | Fan cleaning shows available but user says "no use" | Low | UI unclear about what fan boost actually does |
| 10 | Undervolt applies but doesn't change voltage (275HX) | High | Arrow Lake-HX likely BIOS locked; no verification |
| 11 | OMEN button doesn't capture/toggle app window | Medium | Hotkey registration or WMI event not working |
| 12 | GPU Dynamic Boost only +15W, should be +25W (175W total) | Medium | Need to verify WMI values for newer models |
| 13 | Debug log window too small/hard to read | Low | UI needs larger scrollable log viewer |

**User-requested features:**
- Battery charge limiter (requested frequently)
- Better feedback when features are unavailable
- Finer granularity power control (more detailed TDP options)
- Per-core overclocking (P-core/E-core/Cache) - **significant feature**
- "Apply settings at launch" option (explicit restore toggle)

**Tested Hardware (from user reports):**
| Model | CPU | GPU | Issues |
|-------|-----|-----|--------|
| OMEN Max 16 (ah0097nr) | Intel Core Ultra 9 275HX | RTX 5080 | Undervolt locked, Dynamic Boost limited |
| OMEN 16-xd0xxx (8BCD) | AMD Ryzen 7 7840HS | RTX 4060 | AC detection, fan glitches |

---

## Critical Priority

### 1. Complete WinRing0 Removal

**Status:** Required - WinRing0 doesn't work with Secure Boot anyway  
**Effort:** Medium  
**Impact:** Critical (simplifies codebase, removes unsigned driver issues)

WinRing0 is blocked by Secure Boot on most modern systems. Rather than asking users to disable Secure Boot, we should remove WinRing0 entirely and rely on:
- **PawnIO driver** (Secure Boot compatible, already included)
- **WMI BIOS** (no driver needed, works everywhere)

**Removal Plan:**
1. **Delete from installer:** Remove `WinRing0x64.sys`, `WinRing0x64.dll` from `drivers/WinRing0Stub/`
2. **Delete from codebase:**
   - `Hardware/WinRing0MsrAccess.cs`
   - `Hardware/WinRing0EcAccess.cs`
   - Any `WinRing0` references in detection logic
3. **Update capability detection:** Remove WinRing0 as a backend option
4. **Update UI:** Remove "WinRing0 detected" messages
5. **Update installer script:** Remove WinRing0 file copying and driver installation

**Files to modify/delete:**
- `src/OmenCoreApp/Hardware/WinRing0*.cs` - DELETE
- `drivers/WinRing0Stub/` - DELETE entire folder
- `installer/OmenCoreInstaller.iss` - Remove WinRing0 references
- `MainViewModel.cs` - Remove WinRing0 detection
- `README.md` - Update driver documentation

---

### 2. Undervolting System Rework

**Status:** Required - Current XTU detection causes false positives  
**Effort:** High  
**Impact:** High

**Current Problems:**
- False "XTU installed" detection disables undervolt even when XTU isn't present
- Even with correct detection, undervolt fails due to BIOS/firmware locks
- MSR 0x152 writes fail with Secure Boot (expected - but error handling is poor)

**New Architecture: Capability-Based Detection**

Instead of "is XTU installed?", probe *actual* undervolt capability:

```csharp
public interface IUndervoltBackend
{
    string Name { get; }
    Task<UndervoltProbeResult> ProbeAsync();
    Task<int> GetOffsetAsync();
    Task<bool> SetOffsetAsync(int millivolts);
}

public class UndervoltProbeResult
{
    public bool IsSupported { get; set; }
    public string BlockedReason { get; set; }  // "Secure Boot", "BIOS Lock", "AMD CPU", etc.
    public bool RequiresDriver { get; set; }
    public string RecommendedBackend { get; set; }
}
```

**Backend Priority:**
1. **PawnIO MSR backend** - Secure Boot compatible, primary option
2. **Direct MSR (admin only)** - If PawnIO unavailable and user has MSR access
3. **XTU SDK backend** - Optional, only if user explicitly selects "Use XTU"

**Probe Pipeline:**
1. Check CPU vendor (Intel only for now, AMD Ryzen uses different method)
2. Check IA32_OC_MAILBOX availability via MSR read
3. Check for OC lock bits / Plundervolt mitigation
4. Attempt a **harmless read** (get current offset) - only enable UI if read succeeds

**UI Improvements:**
Show clear status:
- `Supported` - Ready to use
- ` Locked by BIOS` - Feature disabled in firmware
- ` Blocked by Secure Boot` - Needs PawnIO driver
- ` Driver missing` - Install PawnIO
- ` Not supported` - AMD CPU or incompatible hardware
- ` Arrow Lake locked` - Intel Core Ultra 200 series (275HX etc.) appears BIOS locked

**Known CPU Behavior:**
| CPU Family | Undervolt Status |
|------------|------------------|
| Intel 10th-11th Gen | Usually works (check Plundervolt mitigation) |
| Intel 12th-14th Gen | Often locked by BIOS |
| Intel Core Ultra 200 (Arrow Lake) | Appears locked on OMEN Max 16 |
| AMD Ryzen 7000 | Different method needed (Curve Optimizer) |

#### AMD Ryzen Curve Optimizer Support

AMD Ryzen processors don't use MSR 0x150 like Intel. Instead, they use the **Curve Optimizer** via SMU mailbox:

```csharp
public class AmdCurveOptimizer : IUndervoltBackend
{
    // AMD SMU Mailbox MSRs
    private const uint MSR_PMU_CMD = 0xC0011020;
    private const uint MSR_PMU_ARGS = 0xC0011024;
    private const uint CMD_SET_CURVE = 0x18;
    
    public async Task<bool> SetCurveOffsetAsync(int coreIndex, int offset)
    {
        // offset range: -30 to +30 (counts, not mV)
        offset = Math.Clamp(offset, -30, 30);
        
        // Write command to mailbox
        WriteMsr(MSR_PMU_ARGS, (uint)((coreIndex << 16) | (offset & 0xFF)));
        WriteMsr(MSR_PMU_CMD, CMD_SET_CURVE);
        
        // Wait for completion (bit 31 clears when done)
        var timeout = 100;
        while (timeout-- > 0)
        {
            var status = ReadMsr(MSR_PMU_CMD);
            if ((status & 0x80000000) == 0) break;
            await Task.Delay(10);
        }
        
        return ReadMsr(MSR_PMU_CMD) == 0; // Success if cleared
    }
    
    public async Task<bool> SetAllCoreOffsetAsync(int offset)
    {
        // Apply same offset to all cores
        var coreCount = Environment.ProcessorCount / 2; // Logical / 2 for physical
        for (int i = 0; i < coreCount; i++)
        {
            if (!await SetCurveOffsetAsync(i, offset))
                return false;
        }
        return true;
    }
}
```

**AMD-Specific UI:**
- Show "Curve Optimizer" instead of "Undervolt" for Ryzen
- Range: -30 to +30 counts (not millivolts)
- Per-core vs all-core toggle
- Link to Ryzen Master for advanced control

**Reference:** Reverse-engineered from Ryzen Master behavior analysis.

**Don't block undervolt just because XTU services exist** - show warning instead:
> "Intel XTU detected. XTU may override OmenCore's voltage settings. Consider closing XTU for consistent results."

**Critical: Verify changes actually applied**
User reports: "I can apply settings but this doesn't change voltage at baseline or under load"
â†’ Must read back voltage after applying and verify it changed. If not â†’ show "BIOS locked" status.

---

### 3. GPU Dynamic Boost Investigation

**Status:** Needs investigation - User reports +25W should be possible, not just +15W  
**Effort:** Low (research) / Medium (implementation)  
**Impact:** Medium

**User Report (OMEN Max 16 with RTX 5080):**
> "GPU dynamic boost is supposed to allow for up to +25W (instead of only 15W) for a total of 175W (150W original)"

**Investigation Needed:**
1. What WMI values does HP use for different boost levels?
2. Does it vary by GPU model (4060 vs 4080 vs 5080)?
3. Is there a "Maximum" level beyond what we currently expose?
4. Check HP OMEN Gaming Hub to see what options it provides

**Current Implementation:**
```csharp
public enum GpuPowerLevel
{
    Minimum = 0,    // No boost
    Medium = 1,     // +15W?
    Maximum = 2,    // +15W or +25W?
}
```

**Action Items:**
- [ ] Test on RTX 5080 system to see actual TGP with each setting
- [ ] Decompile HP OMEN Gaming Hub to find exact WMI values
- [ ] Consider exposing raw wattage control if possible

**Reverse Engineering Method:**
```powershell
# Capture WMI traffic while using OMEN Gaming Hub
# Run in elevated PowerShell before testing GPU boost in OGH

# Method 1: Monitor WMI events
$query = "SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'hpqBIntM'"
Register-WmiEvent -Namespace root\wmi -Query $query -Action {
    $data = $Event.SourceEventArgs.NewEvent.TargetInstance
    Write-Host "WMI Call: $($data | Format-List | Out-String)"
} -SourceIdentifier HPWmiMonitor

# Method 2: Use WMI Explorer to inspect hpqBIntM class
# Look for SetGpuPower (CMD 0x22) parameter values
Get-WmiObject -Namespace root\wmi -Class hpqBIntM | Format-List *
```

**Expected Discovery:**
OGH may use values beyond 0/1/2 for newer GPUs. RTX 5080 likely has:
- `0x00` = Base TGP (150W)
- `0x01` = +15W (165W)
- `0x02` = +25W (175W)
- Possibly `0x03` or higher for future models

---

### 4. Complete Keyboard RGB Rework

**Status:** Required - Current implementation doesn't work on many models  
**Effort:** High  
**Impact:** Critical

The current keyboard RGB implementation reports WMI success but doesn't actually change colors on most HP OMEN models. A complete rewrite is needed.

**Current Issues:**
- WMI BIOS `SetColorTable` returns success but keyboard doesn't change
- ColorTable format may vary by model (not just 128-byte structure)
- EC register addresses (0xB1-0xBC) vary by keyboard model
- Per-key RGB keyboards need completely different protocol
- No reliable way to detect which method works for a given model

**Research Needed:**
1. **Reverse engineer HP's implementation** - Decompile `HP.Omen.Core.Common.dll` from OMEN Gaming Hub
2. **Study OmenMon source** - Understand their ACPI DSDT analysis approach
3. **EC register mapping** - Document which EC registers control keyboard on each model:
   - OMEN 15 (various years)
   - OMEN 16 (various years)
   - OMEN 17 (various years)
   - OMEN 25L/30L/40L/45L desktops
4. **Per-key RGB protocol** - Research HID-based per-key control for newer models

**Implementation Plan:**

#### Phase 1: Model Detection & Compatibility Database
```csharp
public class KeyboardModel
{
    public string ProductId { get; set; }      // e.g., "8A14", "8BAD"
    public KbdType Type { get; set; }          // Standard, TenKeyLess, PerKeyRgb
    public KbdBackend Backend { get; set; }    // WmiBios, EcDirect, HidPerKey
    public byte[] EcColorRegisters { get; set; } // Model-specific EC addresses
    public bool RequiresBacklightToggle { get; set; }
}
```

#### Phase 2: Multiple Backend Support
- **WMI BIOS Backend** - Current approach, works on some models
- **EC Direct Backend** - Direct EC register writes (model-specific addresses)
- **HID Per-Key Backend** - USB HID protocol for per-key RGB keyboards
- **Auto-Detection** - Try each backend and track which one actually works

#### Phase 3: User-Assisted Calibration
If auto-detection fails, allow users to:
1. Manually select their keyboard model
2. Run a "test pattern" to verify colors work
3. Report success/failure to build a compatibility database

#### Phase 4: Real Verification (Stop Trusting "WMI Returned OK")
After applying a color:
- If `GetColorTable` works â†’ read back and compare
- If it doesn't â†’ apply a known test pattern (e.g., WASD bright red, others off) and prompt user: "Did it change? Yes/No"
- Store result per model (SKU/ProductId/BIOS) locally â†’ build toward a compatibility DB

```csharp
public class RgbApplyResult
{
    public bool WmiReportedSuccess { get; set; }
    public bool VerificationPassed { get; set; }  // Did readback match?
    public bool UserConfirmed { get; set; }       // Did user say "yes it worked"?
    public string FailureReason { get; set; }
}
```

**Files to Create:**
- `Services/KeyboardLighting/IKeyboardBackend.cs` - Backend interface
- `Services/KeyboardLighting/WmiBiosBackend.cs` - WMI implementation
- `Services/KeyboardLighting/EcDirectBackend.cs` - EC implementation  
- `Services/KeyboardLighting/HidPerKeyBackend.cs` - Per-key RGB implementation
- `Services/KeyboardLighting/KeyboardModelDatabase.cs` - Model compatibility data
- `Services/KeyboardLighting/KeyboardLightingServiceV2.cs` - New unified service

**Reference Projects:**
- [OmenMon](https://github.com/OmenMon/OmenMon) - EC and WMI approach
- [OmenHubLighter](https://github.com/Joery-M/OmenHubLighter) - HP driver decompilation
- [OpenRGB](https://gitlab.com/CalcProgrammer1/OpenRGB) - Per-key RGB protocols

**OmenHubLighter Key Findings:**
Reverse engineering of `HP.Omen.Core.Common.dll` revealed HP uses different WMI methods by model year:

| Model Year | WMI Method | Notes |
|------------|------------|-------|
| 2018-2019 | `SetBacklight` only | On/off, no color control |
| 2020-2022 | `SetColorTable` (128-byte) | 4-zone RGB, current implementation |
| 2023+ | `SetKeyboardBacklight` | New interface, different parameters |
| Per-key RGB | HID protocol | USB HID, not WMI |

**Detection Strategy:**
```csharp
public class KeyboardMethodDetector
{
    public async Task<KeyboardMethod> DetectMethodAsync()
    {
        // Try methods in order of model year (newest first)
        if (await TrySetKeyboardBacklightAsync())
            return KeyboardMethod.NewWmi2023;
            
        if (await TrySetColorTableAsync())
            return KeyboardMethod.ColorTable2020;
            
        if (await TryHidPerKeyAsync())
            return KeyboardMethod.HidPerKey;
            
        return KeyboardMethod.Unsupported;
    }
}
```

---

### 5. Corsair Peripheral RGB Control

**Status:** Partially working - detection improved in v1.4.0-beta3  
**Effort:** Medium  
**Impact:** High

**Current State:**
- Device detection working (Dark Core RGB PRO, HS70 PRO, etc.)
- RGB control via direct HID not reliable
- DPI control not implemented
- No iCUE SDK integration (requires iCUE running)

**Planned Improvements:**
1. **Better HID Protocol** - Research proper Corsair HID commands per device
2. **Device-Specific Handlers** - Each device family needs different command sequences
3. **Sync with Keyboard** - Option to sync Corsair RGB with HP keyboard lighting
4. **Standalone Mode** - Work without iCUE running

---

### 6. Fan Control Rework

**Status:** Required - Current implementation is glitchy and unreliable  
**Effort:** High  
**Impact:** Critical

**Current Problems (from beta3 feedback):**
- Fan curve sliders move without user interaction (UI event/binding bug)
- Requested % doesn't match actual fan speed (80% setting â†’ CPU hits 98Â°C)
- No verification that applied settings actually took effect
- WMI BIOS fan levels report unrealistic values (e.g., 62 krpm = 6200 RPM seems wrong)

**New Architecture: Closed-Loop Fan Control**

#### 1. Explicit Control States (no ambiguity)
```csharp
public enum FanControlMode
{
    BiosAuto,           // Let BIOS handle everything
    BiosPreset,         // Default/Performance/Cool thermal policy
    SoftwareCurve,      // OmenCore-managed levels
    MaxOverride,        // Emergency full blast
}
```

UI must always show:
- `Requested %` - What user asked for
- `Applied Level` - What was sent to hardware
- `Actual RPM` - What fans are actually doing

#### 2. Per-Model Fan Calibration
HP's WMI interface varies by model. Some treat "55" as max, others use 0-100.

**Calibration Wizard:**
1. Step through levels: 0, 10, 20, 30, ... 55, 100
2. Record resulting RPM at each step
3. Build curve: `level â†’ RPM` mapping
4. Derive inverse: `target % â†’ appropriate level`

```csharp
public class FanCalibrationProfile
{
    public string ProductId { get; set; }
    public int MaxLevel { get; set; }           // e.g., 55 or 100
    public int[] LevelToRpmMapping { get; set; }
    public bool SupportsDirectRpm { get; set; }
}
```

#### 3. Closed-Loop Verification
When user changes fan curve:
1. Write level to hardware
2. Wait 2-3 seconds for fans to respond
3. Read back actual RPM
4. If RPM didn't change as expected â†’ warn user / retry / fall back

```csharp
public class FanApplyResult
{
    public int RequestedPercent { get; set; }
    public int AppliedLevel { get; set; }
    public int ActualRpmBefore { get; set; }
    public int ActualRpmAfter { get; set; }
    public bool SuccessfullyApplied => Math.Abs(ActualRpmAfter - ExpectedRpm) < 500;
}
```

**Full Verification Implementation (from nbfc-linux best practices):**
```csharp
public class FanVerificationService
{
    private readonly IHpWmiBios _wmiBios;
    private readonly LoggingService _logging;
    private readonly FanCalibrationProfile _calibration;
    
    public async Task<FanApplyResult> ApplyAndVerifyFanSpeedAsync(int fanIndex, int targetPercent)
    {
        var result = new FanApplyResult { RequestedPercent = targetPercent };
        
        // Read current state
        result.ActualRpmBefore = await _wmiBios.GetFanRpmAsync(fanIndex);
        
        // Convert percent to model-specific level
        result.AppliedLevel = _calibration.PercentToLevel(targetPercent);
        
        // Apply setting
        var success = await _wmiBios.SetFanLevelAsync(fanIndex, result.AppliedLevel);
        if (!success)
        {
            _logging.Warn($"WMI SetFanLevel failed for fan {fanIndex}");
            return result;
        }
        
        // Wait for fan to respond (fans have mechanical inertia)
        await Task.Delay(2500);
        
        // Read back actual RPM
        result.ActualRpmAfter = await _wmiBios.GetFanRpmAsync(fanIndex);
        
        // Calculate expected RPM from calibration data
        var expectedRpm = _calibration.GetExpectedRpm(targetPercent);
        var tolerance = expectedRpm * 0.15; // 15% tolerance
        
        if (Math.Abs(result.ActualRpmAfter - expectedRpm) > tolerance)
        {
            _logging.Warn($"Fan verification failed: Expected ~{expectedRpm} RPM, got {result.ActualRpmAfter} RPM");
            
            // Retry once
            await Task.Delay(2000);
            result.ActualRpmAfter = await _wmiBios.GetFanRpmAsync(fanIndex);
            
            if (Math.Abs(result.ActualRpmAfter - expectedRpm) > tolerance)
            {
                _logging.Error($"Fan control not responding as expected. Model may need calibration.");
            }
        }
        else
        {
            _logging.Info($"✓ Fan {fanIndex} verified: {result.ActualRpmAfter} RPM (expected ~{expectedRpm})");
        }
        
        return result;
    }
}
```

#### 4. Fix UI Slider Bugs
- Investigate binding mode (should be `TwoWay` with explicit user trigger)
- Add `ThrottleDispatcher` to prevent rapid-fire updates
- Only apply curve on explicit user action (button click or slider release)

---

### 7. Settings Persistence & Startup Fixes

**Status:** Required - Users report settings don't survive reboot  
**Effort:** Medium  
**Impact:** High

**Reported Issues:**
- TCC Offset resets to 100Â°C on restart
- GPU Power Boost resets to Minimum on restart
- App doesn't start with Windows despite scheduled task

**Root Causes to Investigate:**
1. **Settings saved but not restored:** Check `ConfigService.LoadAsync()` on startup
2. **Settings restored before hardware ready:** Add delay or retry logic
3. **Scheduled task not triggering:** Check task XML, elevation requirements, path

**Fixes:**
```csharp
// On startup - restore saved settings
public async Task RestoreSettingsOnStartupAsync()
{
    await WaitForHardwareReadyAsync(); // Ensure WMI BIOS is available
    
    var config = await _configService.LoadAsync();
    
    if (config.TccOffset != 0)
    {
        await ApplyTccOffsetAsync(config.TccOffset);
        _logging.Info($"Restored TCC Offset: {config.TccOffset}Â°C");
    }
    
    if (config.GpuPowerBoost != GpuPowerLevel.Default)
    {
        await ApplyGpuPowerBoostAsync(config.GpuPowerBoost);
        _logging.Info($"Restored GPU Power Boost: {config.GpuPowerBoost}");
    }
}
```

**Scheduled Task Fix:**
- Verify task runs as Admin with highest privileges
- Use absolute path to exe (not relative)
- Add 30-second delay for Windows services to stabilize
- Test with `schtasks /query /tn "OmenCore"` to verify task exists

**StartupSequencer Pattern (proven approach):**
```csharp
public class StartupSequencer
{
    private readonly IHpWmiBios _wmiBios;
    private readonly ConfigurationService _config;
    private readonly LoggingService _logging;
    
    public async Task InitializeAsync()
    {
        // Phase 1: Wait for hardware
        _logging.Info("Startup: Waiting for WMI BIOS...");
        if (!await WaitForWmiAvailableAsync(timeout: TimeSpan.FromSeconds(15)))
        {
            _logging.Error("Startup: WMI BIOS not available after 15s");
            return;
        }
        
        // Phase 2: Read current hardware state
        _logging.Info("Startup: Reading hardware state...");
        var currentState = await ReadAllHardwareStateAsync();
        
        // Phase 3: Load saved config
        var savedConfig = await _config.LoadAsync();
        
        // Phase 4: Apply settings with retry
        _logging.Info("Startup: Restoring saved settings...");
        
        await ApplyWithRetryAsync("TCC Offset", 
            () => _wmiBios.SetTccOffsetAsync(savedConfig.TccOffset),
            retries: 3, delayMs: 1000);
            
        await ApplyWithRetryAsync("GPU Power Boost",
            () => _wmiBios.SetGpuPowerAsync(savedConfig.GpuPowerBoost),
            retries: 3, delayMs: 1000);
            
        await ApplyWithRetryAsync("Fan Mode",
            () => _fanService.ApplyPresetAsync(savedConfig.FanPreset),
            retries: 3, delayMs: 1000);
            
        _logging.Info("Startup: Initialization complete");
    }
    
    private async Task<bool> WaitForWmiAvailableAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                // Try a simple WMI query
                await _wmiBios.GetFanCountAsync();
                return true;
            }
            catch
            {
                await Task.Delay(500);
            }
        }
        return false;
    }
    
    private async Task ApplyWithRetryAsync(string settingName, Func<Task<bool>> action, int retries, int delayMs)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                if (await action())
                {
                    _logging.Info($"✓ Restored {settingName}");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logging.Warn($"Attempt {i+1}/{retries} for {settingName} failed: {ex.Message}");
            }
            
            if (i < retries - 1)
                await Task.Delay(delayMs);
        }
        _logging.Error($"✗ Failed to restore {settingName} after {retries} attempts");
    }
}
```

---

### 8. Power Source Detection Fix

**Status:** Required - AC/Battery detection not working  
**Effort:** Low  
**Impact:** High (PowerAutomation feature is broken)

**From Logs:**
```
[INFO] Power state changed: On Battery
```
This suggests detection IS happening, but something else is wrong.

**Investigation Points:**
1. `PowerAutomationService` - Is it subscribing to power events correctly?
2. `SystemInformation.PowerStatus` - Is it being read correctly?
3. Event timing - Is the profile switch happening but then getting overridden?

**Fix Plan:**
```csharp
public class PowerAutomationService
{
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        var isOnAc = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        _logging.Info($"Power mode changed: {e.Mode}, On AC: {isOnAc}");
        
        // Debounce rapid changes (e.g., brief power flicker)
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }
    
    private async void ApplyPowerProfileDebounced()
    {
        var isOnAc = IsOnAcPower();
        var profile = isOnAc ? _settings.AcProfile : _settings.BatteryProfile;
        
        _logging.Info($"Applying power profile: {profile} (AC: {isOnAc})");
        await _performanceService.ApplyProfileAsync(profile);
    }
}
```

---

## ðŸŸ  High Priority

### 9. UI/UX Overhaul

**Status:** Planning  
**Effort:** High  
**Impact:** High

**Identified Issues:**

#### Duplicate Information Displays
- Performance mode shown in sidebar AND header - redundant
- Fan status duplicated across multiple views
- GPU boost state displayed in 3+ locations
- Memory usage in sidebar AND SystemView

#### Large/Complex Views
| File | Lines | Issue |
|------|-------|-------|
| `MainViewModel.cs` | 2563 | God class - handles too much |
| `SettingsView.xaml` | 1618 | Too many settings on one page |
| `PeripheralSettingsView.xaml` | 600+ | Needs better organization |

#### Sidebar Issues (User Feedback)
- **Sidebar too small** - Users report it feels cramped
- Fan/temp displays hard to read
- Status icons and text need more breathing room

**Planned Improvements:**

1. **Sidebar Redesign**
   - **Increase width** from ~200px to ~280px (configurable)
   - Larger font for temperatures and fan speeds
   - More padding/spacing between elements
   - Collapsible sections for power users
   - Option to minimize to icon-only mode

2. **Consolidate Status Displays**
   - Single "Status Bar" component showing key metrics
   - Click to expand for details rather than showing everything
   - Remove duplicate performance mode indicators

3. **Split SettingsView**
   - General Settings (theme, startup, tray)
   - Hardware Settings (fan curves, GPU, CPU)
   - Peripheral Settings (Corsair, Logitech)
   - Advanced Settings (logging, WMI, EC access)

4. **Responsive Layouts**
   - Improve layouts for different window sizes
   - Better tablet/touch support
   - Consider compact mode for small screens

5. **Add Light Theme**
   - Currently only dark theme available
   - Many users prefer light themes
   - Auto-switch based on Windows theme

6. **Improve Keyboard View**
   - Visual keyboard layout with clickable zones
   - Better color picker integration
   - Live preview of lighting effects

---

### 10. Code Architecture Refactoring

**Status:** Planning  
**Effort:** High  
**Impact:** High (maintainability)

**God Classes to Split:**

#### MainViewModel.cs (2563 lines)
Split into:
- `FanViewModel.cs` - Fan monitoring and control
- `ThermalViewModel.cs` - Temperature monitoring
- `PerformanceViewModel.cs` - Performance mode management
- `SystemInfoViewModel.cs` - Hardware info display
- `MainViewModel.cs` - Orchestration only (~500 lines)

#### HpWmiBios.cs (1173 lines)
Split into:
- `WmiConnectionManager.cs` - CIM session management
- `FanWmiCommands.cs` - Fan-related WMI calls
- `KeyboardWmiCommands.cs` - Keyboard RGB WMI calls
- `SystemWmiCommands.cs` - System info queries
- `HpWmiBios.cs` - Facade/factory (~300 lines)

#### SettingsView.xaml (1618 lines)
Split into separate pages:
- `GeneralSettingsView.xaml`
- `HardwareSettingsView.xaml`
- `PeripheralSettingsView.xaml`
- `AdvancedSettingsView.xaml`
- Settings navigation via TabControl or TreeView

**Dependency Injection Improvements:**
```csharp
// Current: Static singletons
var fan = FanService.Instance;
var wmi = HpWmiBios.Instance;

// Target: DI container
services.AddSingleton<IFanService, FanService>();
services.AddSingleton<IHpWmiBios, HpWmiBios>();
services.AddSingleton<IKeyboardLightingService, KeyboardLightingServiceV2>();
```

**Thread Safety Fixes:**
- `ObservableCollection` modifications need UI thread marshaling
- `FanService` polling needs proper synchronization
- CIM session management needs thread-safe patterns

---

### 11. 3rd Party RGB Expansion

**Status:** Planning  
**Effort:** High  
**Impact:** High

#### Razer Support
**Dependencies:** Razer Chroma SDK or OpenRGB
**Scope:**
- Razer keyboards (Huntsman, BlackWidow series)
- Razer mice (DeathAdder, Viper, Basilisk)
- Razer headsets (Kraken series)
- Razer mousepads (Goliathus, Firefly)

**Implementation Options:**
1. **Razer Chroma SDK** - Official, requires Synapse
2. **Direct HID** - Standalone but complex per-device
3. **OpenRGB Integration** - Already supports Razer

#### SteelSeries Support
**Dependencies:** SteelSeries GameSense SDK or OpenRGB
**Scope:**
- SteelSeries keyboards (Apex Pro, Apex 3)
- SteelSeries mice (Rival, Sensei, Aerox)
- SteelSeries headsets (Arctis series)

#### OpenRGB Integration (Recommended)
Rather than implementing each vendor separately:
```csharp
public interface IPeripheralRgbProvider
{
    string VendorName { get; }
    Task<IEnumerable<RgbDevice>> ScanDevicesAsync();
    Task SetColorAsync(RgbDevice device, Color color);
    Task SetEffectAsync(RgbDevice device, LightingEffect effect);
}

// Implementations:
class CorsairRgbProvider : IPeripheralRgbProvider { }
class LogitechRgbProvider : IPeripheralRgbProvider { }
class RazerRgbProvider : IPeripheralRgbProvider { }      // NEW
class SteelSeriesRgbProvider : IPeripheralRgbProvider { } // NEW
class OpenRgbProvider : IPeripheralRgbProvider { }        // Bridge to OpenRGB
```

**OpenRGB Benefits:**
- Already supports 100+ RGB devices
- Unified protocol via SDK
- Community-maintained device database
- OmenCore just needs to implement OpenRGB SDK client

---

### 12. Installer & Auto-Update Polish

**Status:** Planning  
**Effort:** Medium  
**Impact:** High (trust/UX)

#### Code Signing
- **Current:** Unsigned installer triggers SmartScreen warnings
- **Required:** EV code signing certificate ($400-600/year)
- **Benefits:** No more "Unknown publisher" warnings, automatic trust

#### Rollback Mechanism
```
Current: Uninstall â†’ Reinstall old version
Target:  Settings â†’ Updates â†’ "Roll back to previous version"
```
- Keep 1-2 previous versions in backup folder
- One-click rollback if update causes issues
- Preserve user settings during rollback

#### Uninstaller Improvements
- Remove ALL files including drivers
- Clean up registry entries
- Remove startup task
- Option to keep settings vs full cleanup

#### Auto-Update Enhancements
- **Delta Updates** - Download only changed files (smaller downloads)
- **Background Download** - Download in background, prompt when ready
- **Release Channel Selection** - Stable / Beta / Nightly
- **Changelog Preview** - Show what's new before updating

#### Silent Install Support
```powershell
# For enterprise/power users
OmenCoreSetup.exe /SILENT /SUPPRESSMSGBOXES /NORESTART
```

---

## ðŸŸ¡ Medium Priority

### 13. Lighting Profiles & Presets

**Effort:** Medium  
**Impact:** Medium

**Features:**
- Save/Load lighting presets (keyboard + peripherals combined)
- Link presets to game profiles
- Quick-switch presets via hotkeys or tray menu
- Import/Export preset files

---

### 14. Per-Zone Keyboard Control UI

**Effort:** Medium  
**Impact:** Medium

**Features:**
- Visual keyboard diagram showing zones
- Click zone to set color
- Drag to select multiple zones
- Preview before applying

**UI Mockup:**
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Keyboard Zones                                             â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚   â”Œâ”€â”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”€â”   â”‚   â”‚
â”‚  â”‚   â”‚ WASD â”‚ â”‚    Left    â”‚ â”‚   Middle   â”‚ â”‚ Rightâ”‚   â”‚   â”‚
â”‚  â”‚   â”‚  ðŸ”´  â”‚ â”‚     ðŸŸ¢     â”‚ â”‚     ðŸ”µ     â”‚ â”‚  ðŸŸ¡  â”‚   â”‚   â”‚
â”‚  â”‚   â””â”€â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”€â”˜   â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  Selected: WASD Zone    Color: #FF0000                     â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

### 15. Lighting Effects Engine

**Effort:** High  
**Impact:** Medium

**Effects to Implement:**
- Static (single color per zone)
- Breathing (fade in/out)
- Color Cycle (rainbow across zones)
- Wave (color moves across keyboard)
- Reactive (flash on keypress - requires per-key support)
- Audio Visualizer (react to system audio)

---

### 16. Debug Log Viewer Improvements

**Status:** User feedback - current log window too small/hard to read  
**Effort:** Low  
**Impact:** Medium (UX for debugging)

**Current Issues:**
- Log window is small and embedded
- Hard to scroll through long logs
- No search/filter capability

**Improvements:**
1. **Larger dedicated window** - Open logs in separate resizable window
2. **Log levels filter** - Show only INFO/WARN/ERROR
3. **Search** - Find specific text in logs
4. **Export** - One-click "Copy all" or "Save to file"
5. **Auto-scroll toggle** - Follow new entries or stay in place
6. **Syntax highlighting** - Color code by log level

```xaml
<!-- New LogViewerWindow.xaml -->
<Window Title="OmenCore Debug Log" Width="900" Height="600">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <!-- Filter bar -->
        <StackPanel Orientation="Horizontal" Grid.Row="0">
            <CheckBox Content="INFO" IsChecked="True"/>
            <CheckBox Content="WARN" IsChecked="True"/>
            <CheckBox Content="ERROR" IsChecked="True"/>
            <TextBox PlaceholderText="Search..." Width="200"/>
            <Button Content="ðŸ“‹ Copy All"/>
            <Button Content="ðŸ’¾ Save"/>
        </StackPanel>
        <!-- Log viewer -->
        <ListBox Grid.Row="1" ItemsSource="{Binding LogEntries}"/>
    </Grid>
</Window>
```

---

### 17. Performance Optimizations

**Status:** Planning  
**Effort:** Medium  
**Impact:** Medium (stability)

#### Async/Await Best Practices
```csharp
// Current: Missing ConfigureAwait
await Task.Delay(100);
var result = await wmiTask;

// Target: Proper ConfigureAwait for library code
await Task.Delay(100).ConfigureAwait(false);
var result = await wmiTask.ConfigureAwait(false);
```

#### Thread Safety Issues
- `ObservableCollection` updates from background threads crash UI
- Fan polling timer needs proper synchronization
- CIM session access needs locking

**Fix Pattern:**
```csharp
// UI thread marshal for ObservableCollection
Application.Current.Dispatcher.Invoke(() =>
{
    Devices.Add(newDevice);
});

// Thread-safe session access
private readonly object _sessionLock = new();
private CimSession GetSession()
{
    lock (_sessionLock)
    {
        return _session ??= CimSession.Create(".");
    }
}
```

#### CIM Session Leak
- `HpWmiBios` creates CIM sessions that may not be properly disposed
- Sessions should be pooled/reused
- Implement `IDisposable` properly

#### Memory Optimization
- Profile memory usage during extended operation
- Check for event handler leaks
- Optimize hardware polling intervals

---

## ðŸŸ¢ Nice to Have

### 18. BIOS Update System Improvements

**Status:** Current implementation works but limited  
**Effort:** Medium  
**Impact:** Medium (user convenience)

#### Current State
The `BiosUpdateService` provides basic BIOS update checking via HP's APIs, but has limitations:
- HP's public APIs don't reliably return BIOS version data
- Serial number lookup often returns HTML instead of JSON
- Falls back to constructing HP Support page URL
- No automatic download/install capability

#### Planned Improvements

**API Improvements:**
```csharp
// Current: Basic serial lookup
var url = $"https://support.hp.com/wcc-services/drivers/bySerial?sn={serial}";

// Target: Multiple API endpoints with fallbacks
public class BiosUpdateServiceV2
{
    private readonly string[] _apiEndpoints = {
        "https://support.hp.com/wcc-services/drivers/bySerial",
        "https://ftp.hp.com/pub/softpaq/catalog/", // HP FTP catalog
        "https://support.hp.com/wcc-services/softwarecatalog"
    };
    
    public async Task<BiosInfo?> CheckMultipleSourcesAsync(SystemInfo info)
    {
        foreach (var endpoint in _apiEndpoints)
        {
            var result = await TryEndpointAsync(endpoint, info);
            if (result != null) return result;
        }
        return null;
    }
}
```

**HP FTP Catalog Parsing:**
- HP publishes softpaq catalogs at `ftp.hp.com/pub/softpaq/`
- Parse CSV/XML catalogs for BIOS softpaqs matching the product ID
- Cache catalog data locally for faster lookups

**Enhanced Version Comparison:**
```csharp
// Better version parsing for HP's various formats
public class BiosVersionParser
{
    // F.xx format (e.g., F.20, F.21)
    public static Version ParseFVersion(string version);
    
    // Date-based format (e.g., 01/15/2024)
    public static DateTime ParseDateVersion(string version);
    
    // AMI format (e.g., 1.15.0)
    public static Version ParseNumericVersion(string version);
}
```

**UI Enhancements:**
1. **Release Notes Preview** - Show what's changed before downloading
2. **Version History** - Display last 3-5 BIOS versions
3. **One-Click Download** - Download directly to Downloads folder
4. **Installation Guide** - Step-by-step for safe BIOS update
5. **Backup Reminder** - Warn user to back up before BIOS update

**Caching:**
- Cache BIOS check results for 24 hours
- Store last-known-good BIOS version
- Track user's BIOS update history

**Safety Features:**
- Verify downloaded file hash against HP's published checksums
- Check for AC power before suggesting BIOS update
- Warn about battery level requirements
- Link to HP's official BIOS update instructions

---

### 19. Community Model Database

**Effort:** Low  
**Impact:** High (long-term)

Create an online database where users can report:
- Their HP model + keyboard type
- Which RGB backend works for them
- EC register addresses if discovered

This data can be aggregated to improve auto-detection over time.

---

### 20. OpenRGB Integration

**Effort:** Medium  
**Impact:** Medium

Instead of reimplementing per-key RGB, integrate with OpenRGB:
- OpenRGB already supports many HP OMEN keyboards
- Can control Corsair, Logitech, Razer peripherals too
- OmenCore becomes the "HP hardware" specialist, OpenRGB handles RGB

---

## 🔮 Future Consideration (v2.0+)

### Per-Core Overclocking

**Status:** Feature request from user with 275HX  
**Effort:** Very High  
**Impact:** High (for enthusiasts)  
**Risk:** High (can damage hardware)

**User Request:**
> "An overclocking option for P core/ E core/ Cache would be sick"

**Considerations:**
1. **Safety** - Overclocking can cause instability, crashes, or hardware damage
2. **Warranty** - May void manufacturer warranty
3. **BIOS locks** - Many systems have OC locked in firmware
4. **Complexity** - Per-core OC requires extensive MSR knowledge

**If implemented:**
- P-Core ratio adjustment
- E-Core ratio adjustment  
- Cache/Ring ratio adjustment
- Per-core voltage offset
- Load-line calibration (LLC)
- Stress test integration
- Temperature/power limits

**Prerequisites:**
- Must have working MSR access (PawnIO)
- Must verify BIOS allows OC
- Must have extensive safety limits
- Must have stress test validation

**Recommendation:** This is significant scope creep. Consider:
1. First perfect undervolting (which is safer)
2. Partner with/integrate existing OC tools rather than reimplementing
3. Only attempt if there's strong community demand

---

### Battery Charge Limiter

**Status:** Frequently requested  
**Effort:** Medium  
**Impact:** High (battery longevity)

HP OMEN laptops may support battery charge limits via WMI or EC. Need to research:
- Does HP expose this via WMI?
- What EC registers control charging?
- Can we limit to 80% or custom threshold?

**Reference:** Some HP models support this in BIOS settings.

---

### Linux Support Foundation

**Status:** Planning foundation in v1.5, full support in v2.0+  
**Effort:** Very High  
**Impact:** High (expands user base significantly)

**Current Stack (Windows-only):**
- WPF (.NET 8) - Windows-only UI framework
- WMI/CIM - Windows Management Instrumentation
- WinRing0/PawnIO - Windows kernel drivers
- LibreHardwareMonitor - Has some Linux support

**Linux Challenges:**
1. **No WMI** - Need alternative hardware access
2. **No WPF** - Need cross-platform UI framework
3. **Different drivers** - Need Linux kernel module or /sys access
4. **Permissions** - Root access for hardware control

**Foundation Work for v1.5:**

#### 1. Abstract Hardware Access Layer
Separate hardware access from Windows-specific code:

```csharp
// Platform-agnostic interface
public interface IHardwareBackend
{
    Task<FanInfo[]> GetFanInfoAsync();
    Task SetFanLevelAsync(int fanIndex, int level);
    Task<ThermalInfo> GetThermalInfoAsync();
    Task<SystemInfo> GetSystemInfoAsync();
}

// Windows implementation (current code)
public class WindowsWmiBackend : IHardwareBackend { }

// Future Linux implementation
public class LinuxSysfsBackend : IHardwareBackend { }
public class LinuxEcBackend : IHardwareBackend { }
```

#### 2. Research Linux HP OMEN Support
Existing Linux projects to study:
- **hp-omen-linux-module** - Kernel module for OMEN fan control
- **omen-fan-ctl** - CLI tool for OMEN fans
- **nbfc-linux** - Notebook Fan Control for Linux
- **OpenRGB** - Already cross-platform

**Linux Hardware Access Methods:**
| Method | Use Case | Permissions |
|--------|----------|-------------|
| `/sys/class/hwmon/` | Temperature sensors | Read: user, Write: root |
| `/sys/devices/platform/hp-wmi/` | HP WMI-like interface | Root |
| `/dev/port` or `ioperm()` | Direct EC access | Root + capabilities |
| ACPI calls via `/proc/acpi/` | BIOS features | Root |

#### 3. Consider Avalonia UI
Avalonia is a cross-platform .NET UI framework:
- Similar to WPF (XAML-based)
- Runs on Windows, Linux, macOS
- Can reuse much of our MVVM architecture
- Active community, good documentation

**Migration Path:**
```
v1.5: Abstract hardware layer, keep WPF
v2.0: Port UI to Avalonia, add Linux backend
v2.1: Full Linux release
```

#### 4. CLI/Daemon Architecture
For Linux, consider splitting:
- **omencore-daemon** - Background service (root) for hardware control
- **omencore-cli** - Command-line interface
- **omencore-gui** - Optional GUI (Avalonia or GTK)

This matches Linux conventions and allows headless server use.

**v1.5 Deliverables:**
- [ ] Create `IHardwareBackend` interface
- [ ] Refactor Windows code to implement interface
- [ ] Document all WMI commands used
- [ ] Research Linux HP WMI support
- [ ] Prototype Linux EC access

---

##  Technical Notes

### HP Keyboard WMI Commands (from OmenMon)

```csharp
// Keyboard WMI commands via BiosCmd.Keyboard (0x20009)
CMD_KBD_TYPE_GET = 0x01;     // GetKbdType - returns keyboard layout
CMD_COLOR_GET = 0x02;        // GetColorTable - 128-byte color table
CMD_COLOR_SET = 0x03;        // SetColorTable - set zone colors
CMD_BACKLIGHT_GET = 0x04;    // GetBacklight - on/off status
CMD_BACKLIGHT_SET = 0x05;    // SetBacklight - toggle on/off
CMD_HAS_BACKLIGHT = 0x06;    // HasBacklight - check support
```

### ColorTable Structure (per OmenMon)

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 128)]
public struct ColorTable {
    public byte ZoneCount;           // Byte 0: Number of zones (4)
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
    byte[] Padding;                  // Bytes 1-24: Padding
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public RgbColor[] Zone;          // Bytes 25+: RGB per zone
}

public struct RgbColor {
    public byte Red, Green, Blue;
}
```

### Known EC Keyboard Registers (model-specific!)

```
Model 8A14 (Ralph 21C2):
- May use different registers than documented

Common pattern (unverified):
- 0xB0: Backlight control
- 0xB1-0xB3: Zone 1 RGB
- 0xB4-0xB6: Zone 2 RGB
- 0xB7-0xB9: Zone 3 RGB
- 0xBA-0xBC: Zone 4 RGB
- 0xBD: Brightness
- 0xBE: Effect mode
```

---

## Compatibility Tester Tool

**Status:** New - Recommended for v1.5  
**Effort:** Medium  
**Impact:** High (builds community database)

Create a standalone diagnostic tool that users can run to collect hardware compatibility data.

**Output:** JSON file that can be:
1. Saved locally for troubleshooting
2. Optionally uploaded (with user consent) to build database
3. Shared on Discord/GitHub for support

**UI:**
- "Run Compatibility Test" button in Settings
- Progress bar during tests
- Color-coded results
- "Copy Report" button for easy sharing

---

## ðŸŽ¯ Priority Implementation Phases

Based on user impact and dependencies:

### Phase 0: Quick Wins (Week 1-2)
| Task | Effort | Impact |
|------|--------|--------|
| Settings persistence (StartupSequencer) | Low | High |
| AC/Battery detection debug | Low | High |
| Fan slider UI binding fix | Low | High |

### Phase 1: Verification Layer (Week 3-4)
| Task | Effort | Impact |
|------|--------|--------|
| Fan verification with readback | Medium | High |
| Undervolt readback verification | Medium | High |

### Phase 2: Keyboard RGB (Week 5-8)
| Task | Effort | Impact |
|------|--------|--------|
| Model detection database | Medium | High |
| WMI method probing (2020 vs 2023) | Medium | High |
| Compatibility tester tool | Medium | High |

### Phase 3: AMD Support (Week 9-10)
| Task | Effort | Impact |
|------|--------|--------|
| Curve Optimizer backend | High | High |
| Ryzen detection & UI | Medium | Medium |

### Phase 4: Architecture (Week 11-14)
| Task | Effort | Impact |
|------|--------|--------|
| Split MainViewModel | High | Medium |
| WinRing0 removal | Low | Low |

### Phase 5: Polish (Week 15-18)
| Task | Effort | Impact |
|------|--------|--------|
| Code signing | Low | High |
| Auto-update improvements | Medium | Medium |


##  Compatibility Tester Tool

**Status:** New - Recommended for v1.5  
**Effort:** Medium  
**Impact:** High (builds community database)

Create a standalone diagnostic tool that users can run to collect hardware compatibility data:

```csharp
public class CompatibilityTester
{
    public CompatibilityReport GenerateReport()
    {
        return new CompatibilityReport
        {
            // System Info
            ProductId = GetProductId(),
            BiosVersion = GetBiosVersion(),
            SystemSku = GetSystemSku(),
            
            // CPU Detection
            CpuModel = GetCpuModel(),
            CpuVendor = GetCpuVendor(),
            SecureBootEnabled = IsSecureBootEnabled(),
            
            // WMI Capabilities
            WmiMethodsAvailable = ProbeWmiMethods(),
            ThermalPolicyVersion = GetThermalPolicyVersion(),
            FanCount = GetFanCount(),
            
            // Keyboard Detection
            KeyboardType = DetectKeyboardType(),
            SetColorTableWorks = TestSetColorTable(),
            SetKeyboardBacklightWorks = TestSetKeyboardBacklight(),
            
            // Test Results
            UndervoltReadable = CanReadUndervolt(),
            FanControlWorks = TestFanControl(),
            GpuPowerControlWorks = TestGpuPower()
        };
    }
}
```

**Output:** JSON file that can be:
1. Saved locally for troubleshooting
2. Optionally uploaded (with user consent) to build database
3. Shared on Discord/GitHub for support

**UI:**
- "Run Compatibility Test" button in Settings
- Progress bar during tests
- Color-coded results ( Working,  Partial,  Not working)
- "Copy Report" button for easy sharing

---

##  Priority Implementation Phases

Based on user impact and dependencies:

### Phase 0: Quick Wins (Week 1-2)
| Task | Effort | Impact |
|------|--------|--------|
| Settings persistence (StartupSequencer) | Low | High |
| AC/Battery detection debug | Low | High |
| Fan slider UI binding fix | Low | High |

### Phase 1: Verification Layer (Week 3-4)
| Task | Effort | Impact |
|------|--------|--------|
| Fan verification with readback | Medium | High |
| Undervolt readback verification | Medium | High |
| GPU power verification | Low | Medium |

### Phase 2: Keyboard RGB (Week 5-8)
| Task | Effort | Impact |
|------|--------|--------|
| Model detection database | Medium | High |
| WMI method probing (2020 vs 2023) | Medium | High |
| Compatibility tester tool | Medium | High |

### Phase 3: AMD Support (Week 9-10)
| Task | Effort | Impact |
|------|--------|--------|
| Curve Optimizer backend | High | High |
| Ryzen detection & UI | Medium | Medium |

### Phase 4: Architecture (Week 11-14)
| Task | Effort | Impact |
|------|--------|--------|
| Split MainViewModel | High | Medium |
| Split SettingsView | Medium | Medium |
| WinRing0 removal | Low | Low |

### Phase 5: Polish (Week 15-18)
| Task | Effort | Impact |
|------|--------|--------|
| Code signing | Low | High |
| Auto-update improvements | Medium | Medium |


## Timeline

| Phase | Target | Scope |
|-------|--------|-------|
| **Phase 1** | Jan 2026 | Research: Decompile HP DLLs, study OmenMon, document models |
| **Phase 2** | Feb 2026 | Code Refactoring: Split MainViewModel, HpWmiBios, SettingsView |
| **Phase 3** | Mar 2026 | Keyboard RGB: Multiple backend support, model database |
| **Phase 4** | Apr 2026 | UI/UX Overhaul: Remove duplicates, improve layouts |
| **Phase 5** | May 2026 | 3rd Party RGB: Razer, SteelSeries, OpenRGB integration |
| **Phase 6** | Jun 2026 | Polish: Installer signing, auto-update improvements |
| **Release** | Jul 2026 | v1.5.0 stable |

---

##  Audit Summary

This roadmap is based on a comprehensive codebase audit conducted December 2025.

### Files Requiring Attention

| File | Lines | Priority | Action |
|------|-------|----------|--------|
| `MainViewModel.cs` | 2563 | High | Split into 5+ ViewModels |
| `SettingsView.xaml` | 1618 | High | Split into tabbed pages |
| `HpWmiBios.cs` | 1173 | Medium | Extract command groups |
| `PeripheralSettingsView.xaml` | 600+ | Medium | Better organization |
| `BiosUpdateService.cs` | 445 | Low | Improve HP API integration |

### Technical Debt

- [ ] Add `ConfigureAwait(false)` throughout async code
- [ ] Implement `IDisposable` properly for CIM sessions
- [ ] Thread-safe `ObservableCollection` updates
- [ ] Remove static singletons, use DI
- [ ] Enable nullable reference types
- [ ] Add unit test coverage (currently minimal)
- [ ] BIOS update: Parse HP FTP softpaq catalogs
- [ ] BIOS update: Cache results, show release notes

---

##  References

- [OmenMon GitHub](https://github.com/OmenMon/OmenMon)
- [OmenMon Documentation](https://omenmon.github.io/)
- [OmenHubLighter](https://github.com/Joery-M/OmenHubLighter)
- [OpenRGB](https://gitlab.com/CalcProgrammer1/OpenRGB)
- [HP OMEN ACPI/DSDT Analysis](https://omenmon.github.io/cli#ec)


## Systems Required

| Feature             | Backend                   |
| ------------------- | ------------------------- |
| Fans / Thermal      | WMI                       |
| Performance modes   | WMI                       |
| GPU power           | WMI                       |
| Intel undervolt     | PawnIO (capability-gated) |
| AMD Curve Optimizer | PawnIO                    |
| Zone RGB            | WMI                       |
| Per-key RGB         | HID / OpenRGB             |
| EC fallback         | PawnIO (opt-in, warned)   |


