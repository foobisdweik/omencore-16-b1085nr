# OmenCore v2.6.1 - Bug Fix & UX Improvements

**Release Date:** February 2026

This release addresses bugs reported via Discord and adds quality-of-life improvements to the system tray experience.

---

## ✨ New Features

### 🎯 Enhanced System Tray

**Fan RPM in Tooltip**
- Tray tooltip now shows actual fan RPM values (e.g., "Max · 4200/4100 RPM")
- Displays both CPU and GPU fan speeds on dual-fan systems

**GPU Power Display**
- Tooltip now shows GPU power consumption in watts (e.g., "GPU: 65°C @ 80% · 125W")
- Helps monitor power draw during gaming

**Visual Mode Checkmarks**
- Fan and performance mode submenus now show ✓ next to the active mode
- Instantly see which mode is currently selected

**Mode Change Notifications**
- Toast notification appears when changing fan mode from Quick Access
- Shows icon based on mode: 🚀 Max, 🤫 Quiet, ⚖️ Auto

### 📊 Enhanced General Tab Dashboard

**New Design:** Reorganized from 2-column (Temps/Fans) to 3-column layout with comprehensive metrics:

| CPU Stats | GPU Stats | Memory Stats |
|-----------|-----------|--------------|
| Temperature + bar | Temperature + bar | RAM Usage % + bar |
| Load % + bar | Load % + bar | Used / Total GB |
| Power (W) | Power (W) ⚡ | Current Performance Mode |
| Fan RPM | Fan RPM | Current Fan Mode |

- **CPU/GPU Load %** - Real-time processor utilization
- **GPU Power (W)** - Watch your GPU wattage in real-time (highlighted in green)
- **CPU Power (W)** - Package power draw
- **RAM Usage** - Memory utilization with used/total GB

### 📦 Installer Improvements

**PawnIO Auto-Selected**
- The "Install PawnIO Driver" option is now checked by default during installation
- Previously required users to manually check the option
- PawnIO provides enhanced hardware access for fan control

### ⚡ Performance

**UI Virtualization**
- Enabled `VirtualizingStackPanel` for ListBox controls
- Uses recycling mode for better memory efficiency
- Smoother scrolling in lists with many items

### 🌡️ More Aggressive Auto Fan Curves

**Problem:** Default Auto curve was too gentle for high-power laptops (i9/RTX 4090), reaching 92°C before hitting 100% fan speed.

**Improvements:**
- **Auto Curve:** Now reaches 100% at 85°C (was 92°C)
  - 70°C → 75% (was 70%)
  - 75°C → 85% (new point)
  - 80°C → 95% (was 75%)
  - 85°C → 100% (was at 92°C)
  
- **Balanced Preset:** Reaches 100% at 85°C (was 80% at 90°C)
- **Performance Preset:** Reaches 100% at 85°C (was 95°C)

**Thermal Protection Thresholds (v2.6.1):**
| Temperature | Minimum Fan Speed |
|-------------|-------------------|
| ≥85°C | 100% (EMERGENCY) |
| ≥80°C | 90% (was 80%) |
| ≥70°C | 70% (was 60%) |
| ≥60°C | 40% |
| ≥50°C | 20% |

---

## 🐛 Bug Fixes

### �️ Temperature Yo-Yo After Thermal Protection (New in v2.6.1)

**Issue:** After thermal emergency triggered at high temps (92°C+), fans would drop back to low speeds (30%) too quickly, causing temps to immediately spike back up, creating a "yo-yo" effect.

**Root Cause:**
1. `ThermalReleaseMinFanPercent` was only 30%, too low for high-power laptops
2. When restoring `_preThermalFanPercent`, code didn't check if temps were still warm
3. `ThermalSafeReleaseTemp` was 55°C, but 60-70°C still needs active cooling on i9/4090

**Fixes Applied:**
- Raised `ThermalEmergencyThreshold` from 88°C to 85°C for earlier intervention
- Raised `ThermalSafeReleaseTemp` from 55°C to 60°C
- Raised `ThermalReleaseMinFanPercent` from 30% to 50%
- Added check: if restoring to low fan % while `stillWarm`, use minimum 50% instead
- This prevents the yo-yo cycle on high-power laptops

### 📢 Toast Notification "Max Lines" Error (New in v2.6.1)

**Issue:** Thermal protection notifications would fail with "We have reached max lines allowed (4) per toast"

**Fix:** Reduced thermal notification from 4 lines to 2 lines:
- Before: 4 separate AddText() calls
- After: `"🛡️ Thermal Protection: {level}"` + `"{temp}°C - Fans boosted to max"`
### 🔧 Fan Diagnostics Crash (New in v2.6.1)

**Issue:** Running fan diagnostics (Apply & Verify) caused repeated crashes with "TwoWay binding cannot work on read-only property 'DeviationPercent'"

**Root Cause:** The `Run.Text` bindings in `FanDiagnosticsView.xaml` defaulted to `TwoWay` mode, but `FanApplyResult.DeviationPercent` is a computed read-only property.

**Fix:** Added `Mode=OneWay` to all bindings in the History list DataTemplate:
- `FanName`, `RequestedPercent`, `ActualRpmAfter`, `DeviationPercent`, `ErrorMessage`

### 🖥️ General Tab Startup Crash (New in v2.6.1)

**Issue:** App crashed on startup with "TwoWay binding cannot work on read-only property 'RamPercent'"

**Root Cause:** ProgressBar Value bindings in `GeneralView.xaml` defaulted to TwoWay mode.

**Fix:** Added `Mode=OneWay` to all ProgressBar bindings for `CpuTemp`, `GpuTemp`, `CpuLoad`, `GpuLoad`, `RamPercent`.
### �🌀 Fan Max Mode from Quick Access (Critical)

**Issue:** When selecting "Max" from the system tray Quick Access menu, the fan mode would show as "Performance" and fans wouldn't actually run at maximum RPM.

**Root Cause:** 
1. The preset search in `SetFanModeFromTray` used `FirstOrDefault(p => p.Name.Contains("Max") || p.Name.Contains("Performance"))` which would find "Performance" first due to preset list ordering
2. The `ApplyPreset` call was missing `immediate: true` parameter
3. The OGH proxy controller mapped "Max" to `ThermalPolicy.Performance` instead of calling `SetMaxFan(true)`
4. The default "Max" preset in configuration was missing `Mode = FanMode.Max`

**Fixes Applied:**
- Changed preset lookup to prioritize exact "Max" match: `FirstOrDefault(p => p.Name.Equals("Max", ...)) ?? FirstOrDefault(p => p.Name.Contains("Max", ...))`
- Added `immediate: true` parameter to `ApplyPreset` call so max fans are applied immediately
- Added explicit `SetMaxFan(true)` call in OGH proxy's `ApplyPreset` method when preset Mode is Max or name is "Max"
- Added `Mode = FanMode.Max` to the default "Max" preset in `DefaultConfiguration.cs`

**Files Modified:**
- `ViewModels/MainViewModel.cs` - Fixed preset lookup and immediate apply
- `Hardware/FanControllerFactory.cs` - Fixed OGH ApplyPreset to call SetMaxFan
- `Services/DefaultConfiguration.cs` - Added Mode property to Max preset

---

### 🌡️ Monitoring Loop Hang Prevention (New in v2.6.1)

**Issue:** Temperature readings would sometimes stop updating completely with no error in logs, requiring app restart.

**Root Cause:** The `ReadSampleAsync` call in the monitoring loop had no timeout. If LibreHardwareMonitor or the worker IPC hung, the loop would wait indefinitely.

**Fixes Applied:**
- Added 10-second timeout to `ReadSampleAsync` calls
- On timeout, logs warning and continues to next iteration (doesn't hang)
- Tracks consecutive timeouts for diagnostics
- Added heartbeat logging every 60 seconds to confirm loop is alive
- Heartbeat shows: iteration count, error count, timeout count

**Files Modified:**
- `Services/HardwareMonitoringService.cs` - Added timeout wrapper and heartbeat

---

### 🌡️ Temperature Freezing / Stuck Readings

**Issue:** Temperature readings would sometimes freeze, requiring a full app restart to recover.

**Root Cause:** The stuck temperature detection logic waited too long (40 seconds) before attempting recovery, and didn't have a permanent fallback mechanism when LibreHardwareMonitor repeatedly failed.

**Improvements:**
- Reduced `MaxSameTempReadingsBeforeLog` from 10 to 5 readings (10s → 10s @ 2s poll)
- Reduced `MaxSameTempReadingsBeforeReinit` from 20 to 10 readings (40s → 20s @ 2s poll)
- Added `_reinitializeAttempts` counter with `MaxReinitializeAttempts = 3`
- Added `_forceWmiBiosMode` flag for permanent WMI fallback after repeated failures
- Enhanced stuck detection to try WMI BIOS immediately at warning threshold
- Added bypass for WMI-only mode in CPU temperature reading when LHM repeatedly fails

**Logic Flow:**
1. After 5 identical temp readings: Log warning, try WMI BIOS fallback immediately
2. If WMI gives different temp: Use it, reset counters
3. After 10 identical readings: Try WMI BIOS, then full reinitialize if needed
4. After 3 failed reinitialize attempts: Switch to WMI-only mode permanently
5. In WMI-only mode: Skip LibreHardwareMonitor entirely for temperature

**Files Modified:**
- `Hardware/LibreHardwareMonitorImpl.cs` - Improved stuck detection and WMI fallback

---

## 📝 Technical Details

### Version Updates
- VERSION.txt: `2.6.0` → `2.6.1`
- OmenCoreApp.csproj: AssemblyVersion/FileVersion updated
- Installer: OmenCoreInstaller.iss version updated
- TrayIconService: Tooltip version updated
- UpdateCheckService: CurrentVersion updated
- DiagnosticsExportService: Export version updated
- ProfileExportService: Export version updated

---

## ⬆️ Upgrade Notes

This is a drop-in replacement for v2.6.0. Simply install over your existing installation or replace the portable files.

### Recommended For:
- Users experiencing "Max" fan mode not working from tray
- Users experiencing frozen temperature readings
- All users on v2.6.0 (no breaking changes)

---

## 📦 Downloads

### Windows
| File | SHA256 |
|------|--------|
| `OmenCoreSetup-2.6.1.exe` | `99FE8D180F9B27B91918D84FD9F4EB493A974FC1F45EC00D365FA364F8489D9B` |
| `OmenCore-2.6.1-win-x64.zip` | `F45D04B3C98489C7281D6A11BFD14FE3EDFA10EA92EEB5273DE11E4118838380` |

### Linux
| File | SHA256 |
|------|--------|
| `OmenCore-2.6.1-linux-x64.zip` | `17FBBABC064F09DF2102AB80F9762EF5B2F9DCB7DEE2B09583B47959CDFE5405` |
Thanks to the Discord community for reporting these issues with detailed logs and steps to reproduce!
