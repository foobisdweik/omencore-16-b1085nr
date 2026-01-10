# OmenCore v2.2.2 - Temperature Monitoring Fixes

**Release Date:** January 2026  
**Type:** Patch Release

## Summary

This release addresses critical temperature monitoring issues reported after v2.2.1, including temperatures getting stuck/frozen and causing fans to stay at high RPM or not respond to heat changes.

---

## 🐛 Bug Fixes

### Quick Access White Line Visual Bug
- **Fixed**: Removed errant white line appearing in the Quick Access popup
- **Cause**: Grid row conflict - two elements assigned to same row, plus unnecessary separator border
- **Solution**: Fixed Grid row definitions and removed the 1px white separator element

### Critical: Temperature Monitoring Freezes (#39, #40)
**Issue:** CPU/GPU temperatures would freeze at a fixed value (e.g., 55°C, 93°C) causing:
- Fans stuck at high RPM because the fan curve sees a constant high temperature
- Fans not responding to actual temperature increases, leading to thermal throttling
- Temperature display showing incorrect values (e.g., shows 93°C when actual is 80°C)

**Affected Models:** OMEN 16, OMEN Max 16, OMEN 17 (multiple users)

**Root Cause:** The HardwareWorker would copy previous sample values when starting a new reading cycle. If CPU sensor readings failed silently or returned identical values due to LibreHardwareMonitor caching issues, the stale temperature persisted indefinitely. The client side had no way to detect this staleness.

**Fix:**
- Added `IsFresh` and `StaleCount` fields to HardwareSample for staleness detection
- HardwareWorker now tracks if CPU temperature actually changed between readings
- After 20+ consecutive identical readings (~30 seconds), sample is marked as stale
- Client-side LibreHardwareMonitorImpl detects stale data and auto-restarts the worker
- Worker logs warnings when staleness is detected and attempts sensor reinitialization
- Added stuck temperature detection in the main app that tries alternative sensors

**Technical Details:**
```csharp
// Worker now tracks staleness
if (!cpuTempUpdated && sample.CpuTemperature > 0)
{
    sample.StaleCount = _lastSample.StaleCount + 1;
    if (sample.StaleCount >= 20)
    {
        sample.IsFresh = false;
        // Log and attempt sensor reinit
    }
}

// Client detects stale worker data
if (!workerSample.IsFresh || workerSample.StaleCount > 30)
{
    // Restart worker to get fresh sensors
    await _workerClient.StopAsync();
    await _workerClient.StartAsync();
}
```

---

## 📋 Known Issues from Community Reports

### OMEN 14 Transcend Compatibility
- Power mode changes may not work properly
- Fan behavior can be erratic
- Some users report OmenCore interfering with OGH
- **Status:** Under investigation - need more logs from affected users

### 2023 XF Model - Keyboard Lights Require OGH
- Keyboard lighting only works with OMEN Gaming Hub installed
- **Status:** This may be a WMI BIOS limitation on this model

### RDP Window Pop-up (#37)
- OmenCore window randomly appears when opening Remote Desktop
- **Status:** Previously fixed but may still occur in edge cases

### OMEN Key Quick Access
- OMEN key opens main app instead of quick access toolbar regardless of setting
- **Status:** Under investigation

### Windows Defender False Positive
- Some users see `Win32/Sonbokli.A!cl` (ML heuristic detection)
- **Status:** This is a common false positive for GitHub projects. The `!ml` suffix indicates machine-learning flagged it due to behaviors that "could be concerning if malicious" (e.g., hardware access, driver loading). OmenCore is open source and safe.

### Visual: White Line in Quick Access
- ✅ **FIXED in this release**

---

## 🔧 Technical Details

### Files Changed
- `OmenCoreApp/Views/QuickPopupWindow.xaml`
  - Fixed Grid row definitions (added 7th row)
  - Moved Quick Actions to correct row
  - Removed white separator border causing visual artifact

- `OmenCore.HardwareWorker/Program.cs`
  - Added `IsFresh` and `StaleCount` to HardwareSample class
  - Track consecutive identical CPU temperature readings
  - Log and attempt recovery when staleness detected
  - Always update CpuTemperature (even if 0) to prevent stale caching

- `OmenCoreApp/Hardware/HardwareWorkerClient.cs`
  - Added `IsFresh` and `StaleCount` properties to match worker

- `OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs`
  - Detect stale data from worker
  - Auto-restart worker when staleness threshold exceeded
  - Added stuck temperature detection with alternative sensor fallback

### Staleness Detection Thresholds
- **Worker-side:** 20 cycles (~30 seconds) of identical readings = mark as not fresh
- **Client-side:** StaleCount > 30 or IsFresh=false triggers worker restart
- **In-process:** 10 identical readings triggers alternative sensor search

---

## 📥 Downloads

| File | SHA256 |
|------|--------|
| OmenCoreSetup-2.2.2.exe | `D804CAC35026B2706393296FE74DA522735CC1329A0C8EE2415CFFDD0347CE97` |
| OmenCore-2.2.2-win-x64.zip | `778B4C58D19E9DEBC9492991F2CC1801C7825CD2CB8F4A6CB3101D8DCC63E4A3` |
| OmenCore-2.2.2-linux-x64.zip | `68ADE210855D044C797C79AC252E89334722FF31CAE417EFD597C12CDBB8671A` |

---

## 🙏 Acknowledgments

Thanks to the community members who reported these issues:
- @its-urbi - Fans stuck at 4600RPM issue (#39)
- @xenon205 - Temperature stuck display, false positive report (#40)
- @h4Zzzy - Temperature freeze on OMEN Max 16
- Discord community - OMEN 14 Transcend reports, keyboard lighting, RDP issues

---

**Full Changelog:** [v2.2.1...v2.2.2](https://github.com/theantipopau/omencore/compare/v2.2.1...v2.2.2)
