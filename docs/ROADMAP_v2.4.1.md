# OmenCore v2.4.1 Roadmap

**Target Release:** Q1 2026  
**Status:** Planning  
**Last Updated:** January 16, 2026

---

## Overview

Version 2.4.1 is a **critical bug fix and RPM accuracy** release following v2.4.0 user reports. Priority focus on fixing fan control regressions and implementing accurate RPM reading from EC registers instead of estimates.

---

## 🔴 Critical Priority: Bug Fixes

### Issue Reports from Discord (Jan 15-16, 2026)

| # | Issue | Reporter | Model | Status |
|---|-------|----------|-------|--------|
| 1 | Fans spin at max speed when selecting Balanced | replaY! | OMEN 16-b0xxx | ✅ **Fixed** |
| 2 | Max preset doesn't max fans, only Auto maxes fans | replaY! + SimplyCarrying | Multiple | ✅ **Fixed** |
| 3 | Fan speed shows 100% but fans are off or at low speeds | kastenbier2743 | Unknown | ✅ **Fixed** |
| 4 | Nothing seems to work rn | replaY! | OMEN 16-b0xxx | ✅ **Fixed** |
| 5 | Power limits don't work | replaY! | OMEN 16-b0xxx | 🟡 **Partial** |
| 6 | GPU capped at 60W (was 95W), power control unavailable | New User | Unknown | 🟡 **Model-Specific** |
| 7 | Fans fail to ramp up even when set to Max | New User | Unknown | ✅ **Fixed** |
| 8 | WinRing0 detection intermittent (works in v2.3.0, fails in v2.4.0) | New User | Unknown | 🔴 **Investigating** |
| 9 | UI option missing (used to exist) | replaY! | OMEN 16-b0xxx | 🟡 **Medium** |
| 6 | UI cut off at bottom | replaY! | OMEN 16-b0xxx | 🟢 **Low** |
| 7 | Recent activity only shows global hotkeys | replaY! | OMEN 16-b0xxx | 🟢 **Low** |

### User Environments

**replaY! (OMEN 16-b0xxx):**
- Model: OMEN by HP Laptop 16-b0xxx (88F4)
- BIOS: F.51
- GPU: NVIDIA GeForce RTX 3060 Laptop GPU
- Secure Boot: Enabled
- PawnIO: Available ✓
- WMI BIOS: Not functional
- OGH: Detected but WMI interface not accessible (need admin)
- Log: `OmenCore_20260115_151307.log`

**SimplyCarrying (OMEN MAX 16t-ah000):**
- Model: OMEN MAX Gaming Laptop 16t-ah000 (8D41)
- BIOS: F.21
- GPU: NVIDIA GeForce RTX 5090 Laptop GPU
- Secure Boot: Enabled
- PawnIO: Available ✓
- WMI BIOS: Not functional
- OGH: Detected and responsive
- Log: `OmenCore_20260115_070358.log`

**mentos (OMEN Transcend 16-u0xxx):**
- Model: OMEN by HP Transcend Gaming Laptop 16-u0xxx (8BB3)
- BIOS: F.38
- GPU: NVIDIA GeForce RTX 4070 Laptop GPU
- Secure Boot: Disabled
- PawnIO: Available ✓
- WMI BIOS: Not functional
- OGH: Not installed
- Log: `OmenCore_20260115_194320.log`

### Root Cause Analysis Needed

**Hypothesis 1: Preset Application Bug (Issue #1, #2)**
- **Symptom:** Balanced causes max speed, Max doesn't max fans
- **Possible Cause:** 
  - `ApplyPreset()` in `FanController.cs` line 26-32 uses `Max(p => p.FanPercent)` instead of temperature-based curve
  - Should apply the entire curve table, not just the max value
  - Current implementation: `WriteDuty(preset.Curve.Max(p => p.FanPercent))`
  - Expected: Temperature-based curve evaluation or full curve table write to EC
- **Files to investigate:**
  - `src/OmenCoreApp/Hardware/FanController.cs` lines 26-32
  - `src/OmenCoreApp/Services/FanService.cs` (preset application logic)
  - `src/OmenCoreApp/Models/FanPreset.cs` (preset definitions)

**Hypothesis 2: RPM Reading Mismatch (Issue #3)**
- **Symptom:** Shows 100% but fans are off/low
- **Possible Cause:** 
  - Current estimation logic in `ReadFanSpeeds()` returns estimated RPM, not actual
  - LibreHardwareMonitor returns 0 fans → estimation kicks in
  - Estimation uses `_lastSetFanPercent` but might not reflect BIOS auto-control
- **Files to investigate:**
  - `src/OmenCoreApp/Hardware/FanController.cs` lines 48-117

**Hypothesis 3: General Fan Control Failure & Power Limits (Issue #4 & #5)**
- **Symptom:** Nothing works at all; power limits may also not apply
- **Possible Cause:** 
  - EC access working but commands not taking effect
  - BIOS/EC lock preventing manual control
  - Register map incorrect for newer models
  - Power limit registers (e.g., `0xCE`/`0xCF`) or OGH WMI proxy not being written/read correctly
- **Files to investigate:**
  - EC register allowlist in `PawnIOEcAccess.cs` and `WinRing0EcAccess.cs`
  - `FanController.WriteDuty()` and `PowerLimitController.cs` implementations
  - EC initialization sequence and OGH WMI proxy handling
  - `src/OmenCoreApp/Hardware/PowerLimitController.cs` and `PawnIOEcAccess.cs`

---

## 🔴 High Priority: Accurate RPM Reading

### Current State: Estimation-Based

**Problem:**
- v2.4.0 uses RPM **estimation** based on last set percentage or temperature
- HP OMEN laptops don't expose fan RPM via LibreHardwareMonitor (SuperIO)
- Estimation is inaccurate when BIOS auto-control is active
- Users see incorrect RPM values (e.g., 0 RPM, or 100% when fans are off)

**Current Implementation:**
```csharp
// FanController.cs lines 70-95
if (_lastSetFanPercent >= 0)
{
    fanPercent = _lastSetFanPercent;
    fanRpm = (_lastSetFanPercent * 5500) / 100; // ESTIMATED
}
else
{
    var maxTemp = Math.Max(cpuTemp, gpuTemp);
    fanPercent = Math.Clamp((int)((maxTemp - 30) * 2), 20, 80);
    fanRpm = (fanPercent * 5500) / 100; // ESTIMATED
}
```

### Solution: Read EC Registers Directly

**EC Register Map (from omen-fan project):**
- **0x34** - Fan 1 Speed Set (units of 100 RPM) - **READ for actual RPM!**
- **0x35** - Fan 2 Speed Set (units of 100 RPM) - **READ for actual RPM!**
- **0x2E** - Fan 1 Speed % (0-100)
- **0x2F** - Fan 2 Speed % (0-100)

**Linux Implementation Reference:**
```csharp
// LinuxEcController.cs lines 238-246
public (int fan1, int fan2) GetFanSpeeds()
{
    if (!HasEcAccess)
        return (0, 0);

    var fan1 = (ReadByte(REG_FAN1_SPEED_SET) ?? 0) * 100;
    var fan2 = (ReadByte(REG_FAN2_SPEED_SET) ?? 0) * 100;
    return (fan1, fan2);
}
```

### Implementation Plan

#### Phase 1: Add EC RPM Reading to FanController
- [x] Add EC register constants for RPM reading
- [ ] Implement `ReadActualFanRpm()` method
- [ ] Modify `ReadFanSpeeds()` to prioritize EC readings over estimation
- [ ] Add fallback to estimation only if EC read fails
- [ ] Test on multiple OMEN models (need community testers)

#### Power Limits Investigation (NEW)
- [ ] Verify `PowerLimitController` write/read behavior and EC registers (0xCE/0xCF) on affected models
- [ ] Add read-back verification for power limit changes and include in `FanVerificationService` or a new `PowerVerificationService`
- [ ] Add unit tests mocking `IEcAccess` and OGH WMI for power limit behavior
- [ ] Add acceptance test to pre-release test matrix (apply power limit, verify EC/WMI readback)
#### Phase 2: Validate Against Known Good Values
- [ ] Compare EC-read RPM vs LibreHardwareMonitor (when available)
- [ ] Verify RPM units (100 RPM multiplier correct?)
- [ ] Test edge cases (fan stopped, max speed, varying loads)
- [ ] Document any model-specific quirks

#### Phase 3: Add RPM Calibration (Optional)
- [ ] Add min/max RPM calibration per model
- [ ] Store calibration data in `config.json`
- [ ] Auto-detect RPM range during first run
- [ ] Provide UI for manual calibration

### Code Changes Required

**File: `src/OmenCoreApp/Hardware/FanController.cs`**
```csharp
// Add constants at top of class
private const ushort REG_FAN1_RPM_READ = 0x34;  // Fan 1 actual speed (100 RPM units)
private const ushort REG_FAN2_RPM_READ = 0x35;  // Fan 2 actual speed (100 RPM units)

// New method to read actual RPM from EC
private (int fan1Rpm, int fan2Rpm) ReadActualFanRpm()
{
    if (!_ecAccess.IsAvailable)
        return (0, 0);

    var fan1Unit = _ecAccess.ReadByte(REG_FAN1_RPM_READ);
    var fan2Unit = _ecAccess.ReadByte(REG_FAN2_RPM_READ);

    // Convert from 100 RPM units to actual RPM
    var fan1Rpm = (fan1Unit ?? 0) * 100;
    var fan2Rpm = (fan2Unit ?? 0) * 100;

    return (fan1Rpm, fan2Rpm);
}

// Modify ReadFanSpeeds() to use actual EC readings
public IEnumerable<FanTelemetry> ReadFanSpeeds()
{
    var fans = new List<FanTelemetry>();

    // Try LibreHardwareMonitor first (some models expose via SuperIO)
    var fanSpeeds = _bridge.GetFanSpeeds();
    if (fanSpeeds.Any())
    {
        // Use LibreHardwareMonitor data...
        return fans;
    }

    // Read actual RPM from EC registers
    var (fan1Rpm, fan2Rpm) = ReadActualFanRpm();
    var cpuTemp = _bridge.GetCpuTemperature();
    var gpuTemp = _bridge.GetGpuTemperature();

    // Calculate duty cycle from actual RPM
    int fan1Percent = CalculateDutyFromRpm(fan1Rpm, 0);
    int fan2Percent = CalculateDutyFromRpm(fan2Rpm, 1);

    fans.Add(new FanTelemetry 
    { 
        Name = "CPU Fan", 
        SpeedRpm = fan1Rpm,  // ACTUAL, not estimated!
        DutyCyclePercent = fan1Percent, 
        Temperature = cpuTemp 
    });
    fans.Add(new FanTelemetry 
    { 
        Name = "GPU Fan", 
        SpeedRpm = fan2Rpm,  // ACTUAL, not estimated!
        DutyCyclePercent = fan2Percent, 
        Temperature = gpuTemp 
    });

    return fans;
}
```

### Testing Plan

**Required Test Scenarios:**
1. **Idle State (BIOS auto-control):** Read RPM when app not controlling fans
2. **Manual Control:** Set various percentages, verify RPM matches
3. **Preset Application:** Apply Quiet/Balanced/Performance, verify RPM
4. **Transition Testing:** Change presets rapidly, ensure RPM tracks correctly
5. **Zero RPM:** Verify fans stopped (0 RPM) when appropriate
6. **Max RPM:** Verify max speed reached (typically 5000-6000 RPM)

**Community Testing Required:**
- Multiple OMEN models (16-b0xxx, 16t-ah000, 16-u0xxx, 17-ck2xxx, etc.)
- Different BIOS versions
- With/without OGH installed
- Secure Boot enabled/disabled
- PawnIO vs WinRing0

---

## 🟡 Medium Priority: Preset Logic Fix

### Issue: ApplyPreset() Not Using Temperature-Based Curves

**Current Behavior:**
```csharp
// FanController.cs lines 26-32
public void ApplyPreset(FanPreset preset)
{
    if (preset.Curve.Count == 0)
    {
        return;
    }
    WriteDuty(preset.Curve.Max(p => p.FanPercent)); // WRONG: Sets max immediately!
}
```

**Problem:**
- Sets fan to **maximum value in curve** immediately, regardless of temperature
- Balanced preset might have curve [30°C→30%, 80°C→100%], but sets 100% instantly
- Defeats the purpose of fan curves

**Expected Behavior:**
- Should evaluate curve based on **current temperature**
- Or delegate to `FanService` to manage curve over time
- Preset application should be temperature-aware

### Implementation Options

**Option A: Delegate to FanService (Recommended)**
```csharp
// FanController.cs
public void ApplyPreset(FanPreset preset)
{
    // Don't apply directly - let FanService handle curve evaluation
    // FanController should only write duty cycles, not decide logic
    throw new InvalidOperationException("Use FanService.ApplyPreset() instead");
}
```

**Option B: Temperature-Based Application**
```csharp
// FanController.cs
public void ApplyPreset(FanPreset preset)
{
    if (preset.Curve.Count == 0)
        return;

    var cpuTemp = _bridge.GetCpuTemperature();
    var gpuTemp = _bridge.GetGpuTemperature();
    var maxTemp = Math.Max(cpuTemp, gpuTemp);

    // Find appropriate curve point
    var percent = EvaluateCurve(preset.Curve, maxTemp);
    WriteDuty(percent);
}

private int EvaluateCurve(List<FanCurvePoint> curve, double temp)
{
    var sorted = curve.OrderBy(p => p.TemperatureC).ToList();
    
    if (temp <= sorted.First().TemperatureC)
        return sorted.First().FanPercent;
    if (temp >= sorted.Last().TemperatureC)
        return sorted.Last().FanPercent;

    // Linear interpolation between points
    for (int i = 0; i < sorted.Count - 1; i++)
    {
        if (temp >= sorted[i].TemperatureC && temp <= sorted[i + 1].TemperatureC)
        {
            var t1 = sorted[i].TemperatureC;
            var t2 = sorted[i + 1].TemperatureC;
            var p1 = sorted[i].FanPercent;
            var p2 = sorted[i + 1].FanPercent;
            
            return (int)(p1 + (p2 - p1) * (temp - t1) / (t2 - t1));
        }
    }
    
    return sorted.Last().FanPercent;
}
```

---

## 🟢 Low Priority: UI/UX Fixes

### Issue #6: UI Cut Off at Bottom
- **Symptom:** Some UI elements not visible
- **Possible Cause:** Fixed window height, content overflow
- **Fix:** Make window scrollable or increase min height

### Issue #7: Recent Activity Only Shows Global Hotkeys
- **Symptom:** Other activity events not logged
- **Fix:** Review `RecentActivityService` event subscription

---

## Additional Recommendations & Acceptance Criteria

### Recommended Additions
- **MSI Afterburner / RTSS detection & integration** ✅
  - Detect RTSS shared memory or Afterburner API. Read GPU fan RPM (and optionally allow GPU fan control via Afterburner when available).
  - Benefit: Reliable GPU fan telemetry on systems where EC/LHM cannot provide GPU RPM.

- **Diagnostic & Raw EC Logging (opt-in)** ✅
  - Add user opt-in diagnostic mode that logs raw EC registers and timestamps; include 'Upload diagnostics' helper to attach to bug reports.
  - Benefit: speeds triage for field issues and allows for model-specific analysis.

- **UI Enhancements for Diagnostics** ✅
  - Show RPM source (EC / HWMon / Afterburner / Estimate) in `Fan Diagnostics` and display last verification result + timestamp.
  - Add a `Run Sweep` button to perform calibration and collect a mapping of percent→RPM.

- **Telemetry (Privacy-first, opt-in)** ✅
  - Optional anonymous upload of calibration data (model, BIOS, non-PII measurements) to improve calibration coverage.

- **CI / Integration Tests for Fan Control** ✅
  - Add unit tests with fakes/mocks for `IEcAccess` (PawnIO/WinRing0), `Wmi` and Afterburner, and CI checks for fan-control safety.

- **Release automation: checksums & asset publishing** ✅
  - Ensure the build pipeline generates SHA256 checksums, attaches them to release assets, and updates the release notes automatically.

### Acceptance Criteria (per critical bug)
- Issue #1 (Balanced→Max): When switching to Balanced, fans settle to the curve-evaluated percent within hysteresis and do not jump to max. Test: apply Balanced from Quiet and confirm duty% is within expected curve value within 2s.
- Issue #2 (Max preset): Max preset sets fan to highest supported level (or explicitly toggles Max Boost) within 1s. Test: apply Max and verify EC registers (0x2E/0x2F or 0x34/0x35) show expected values.
- Issue #3 (RPM mismatch): When EC registers provide RPM, UI must display the EC-measured RPM (not estimates). Test: compare `ReadActualFanRpm()` output to UI and confirm match.
- Issue #4 (Control failure): Commands that set fan percent must be verified via readback; failures should be reported in UI and logs with helpful guidance. Test: Set to 50% -> readback reports >=45% OR revert to previous state and log an error.

### Test Matrix (add these rows to QA checklist)
| Scenario | Model | Backend | Expectation |
|---|---:|---|---|
| Idle, auto-control | 16-b0xxx | PawnIO/EC | EC RPM reads non-zero, UI shows EC as source |
| Manual set 100% (Max) | 16t-ah000 | PawnIO | Fans move to max and EC readbacks show expected RPM |
| Preset switch (Quiet→Balanced) | 16-u0xxx | WinRing0/PawnIO | Fans follow curve evaluation (no instant max) |
| Afterburner present | Any with GPU fan | Afterburner | GPU RPM reported via Afterburner matches EC or LHM if available |
| Diagnostic sweep | Multiple models | PawnIO/WinRing0 | Sweep produces mapping and stores `FanCalibrationProfile` entry |

---

## Release Checklist

### Pre-Release Testing
- [ ] Fix Issue #1 (Balanced → max speed)
- [ ] Fix Issue #2 (Max doesn't max)
- [ ] Fix Issue #3 (RPM mismatch)
- [ ] Fix Issue #4 (nothing works)
- [ ] Implement accurate EC RPM reading
- [ ] Test on at least 3 different OMEN models
- [ ] Verify no regressions in v2.4.0 features

### Community Beta
- [ ] Release v2.4.1-beta with bug fixes
- [ ] Collect logs from affected users
- [ ] Verify RPM accuracy on different models
- [ ] Address any new issues

### Documentation
- [ ] Update CHANGELOG with bug fixes
- [ ] Document EC RPM reading implementation
- [ ] Add troubleshooting section for "fans not responding"
- [ ] Create Reddit post for v2.4.1 release

### Build & Deploy
- [ ] Increment version to 2.4.1
- [ ] Build installer (OmenCoreSetup-2.4.1.exe)
- [ ] Generate SHA256 checksums
- [ ] Upload to GitHub releases
- [ ] Update README with v2.4.1 notes

---

## Technical Notes

### EC Register Access Safety

When reading registers 0x34 and 0x35 for RPM:
- These are **read-only** from EC perspective (BIOS writes, we read)
- Safe to read frequently (every monitoring interval)
- No risk of breaking fan control by reading

### Multi-Model Compatibility

Different OMEN models may have different:
- EC register layouts
- RPM multipliers (most use 100, some might differ)
- Fan count (1 fan vs 2 fans)
- Max RPM (5000-6000 typical, gaming laptops up to 7000)

**Solution:** Add model-specific config in `default_config.json`:
```json
{
  "fanRpmConfig": {
    "88F4": { "maxRpm": 5500, "minRpm": 1500, "multiplier": 100 },
    "8D41": { "maxRpm": 6000, "minRpm": 1500, "multiplier": 100 },
    "8BB3": { "maxRpm": 5500, "minRpm": 1500, "multiplier": 100 }
  }
}
```

---

## Community Feedback Needed

**Call for Testers:**
- Need users with OMEN 16-b0xxx, 16t-ah000, 16-u0xxx to test v2.4.1-beta
- Please provide logs + screenshots of RPM readings
- Report any preset application issues

**Discord/Reddit Post Template:**
```
🛠️ OmenCore v2.4.1-beta - Bug Fixes & Accurate RPM

We've identified several critical issues in v2.4.0 and need your help testing fixes:

**Fixed Issues:**
✅ Balanced preset causing max fan speed
✅ Max preset not reaching full speed
✅ Inaccurate RPM readings (now reads from EC registers)

**Need Testing:**
Please test and report:
- Fan RPM values (compare to HP Omen Hub if installed)
- Preset switching (Quiet → Balanced → Performance)
- Fan behavior when closing app

**Download:** [Link to beta release]
**Logs:** %LOCALAPPDATA%\OmenCore\logs\
```

---

## Version History

- **2026-01-16:** Initial roadmap created based on Discord bug reports
- **Target:** v2.4.1-beta by end of January 2026
- **Target:** v2.4.1 stable by mid-February 2026
