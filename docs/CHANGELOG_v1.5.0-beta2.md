# OmenCore v1.5.0-beta2 Release Notes

**Release Date:** December 2025  
**Type:** Beta Release (Bug Fixes + Improvements)

## Overview

Beta2 addresses all bugs reported by testers during the v1.5.0-beta testing period. This release focuses on stability, reliability, and better user communication about hardware capabilities.

---

## Tester Feedback

> "Fan hysteresis seems to be improved, it is much more smoother than 1.4" ✨

---

## Bug Fixes

### 🔧 Auto-Update File Locking (Issue from tester feedback)
**Problem:** Auto-update would fail with "The process cannot access the file because it is being used by another process."

**Fix:** 
- Added explicit scope for file stream disposal in hash computation
- Added 100ms delay after download to allow file handles to release
- Implemented retry logic (3 attempts with increasing delays) for hash verification

**Files Changed:** `Services/AutoUpdateService.cs`

---

### 💥 AC/Battery Crash Fix (Issue from tester feedback)
**Problem:** Application crashed when unplugging from AC power.

**Fix:**
- Wrapped `OnPowerModeChanged` handler in try-catch to prevent unhandled exceptions
- Added dispatcher marshalling for `PowerStateChanged` event to ensure UI thread safety

**Files Changed:** `Services/PowerAutomationService.cs`

---

### ⚡ AC Status Indicator Not Updating (Issue from tester feedback)
**Problem:** The power status in Settings didn't update when plugging/unplugging AC.

**Fix:**
- Converted `CurrentPowerStatus` from read-only computed property to property with backing field
- Added `RefreshPowerStatus()` method
- Subscribed to `PowerStateChanged` event in MainViewModel to trigger UI refresh

**Files Changed:** `ViewModels/SettingsViewModel.cs`, `ViewModels/MainViewModel.cs`

---

### 📊 CPU Overhead Option Not Disabling Charts (Issue from tester feedback)
**Problem:** Enabling "Low CPU Overhead Mode" in Settings didn't disable the monitoring graphs.

**Fix:**
- Added `HardwareMonitoringService` dependency to `SettingsViewModel`
- Added `LowOverheadModeChanged` event in SettingsViewModel
- MainViewModel now subscribes to this event and updates `MonitoringGraphsVisible`
- Low overhead mode now properly hides charts and reduces polling frequency

**Files Changed:** `ViewModels/SettingsViewModel.cs`, `ViewModels/MainViewModel.cs`

---

### 😴 S0 Modern Standby Fan Revving (Issue from tester feedback)
**Problem:** Fans would rev up when entering S0 Modern Standby mode.

**Fix:**
- Added `Pause()` and `Resume()` methods to `HardwareMonitoringService`
- Added `SystemSuspending` and `SystemResuming` events to `PowerAutomationService`
- Handle `PowerModes.Suspend` and `PowerModes.Resume` events
- Monitor loop now pauses during standby to prevent WMI/EC queries that trigger fan activity

**Files Changed:** `Services/HardwareMonitoringService.cs`, `Services/PowerAutomationService.cs`, `ViewModels/MainViewModel.cs`

---

### ⏱️ Preset Swap Delay (Issue from tester feedback)
**Problem:** Switching between fan presets was slow, causing temperature spikes during transition.

**Fix:**
- Skip `ResetFromMaxMode()` sequence if not actually in max mode (300ms → 0ms for non-max transitions)
- Reduced delays in `ResetFromMaxMode()` from 300ms to 150ms total when exiting max mode
- Transition time reduced by 50-100% depending on the preset change

**Files Changed:** `Hardware/WmiFanController.cs`

---

### 🖼️ Installer Image/Text Truncation (Issue from tester feedback)
**Problem:** Welcome screen text was truncated and images didn't display correctly.

**Fix:**
- Simplified `WelcomeLabel2` message to prevent overflow
- Removed ASCII art box characters that didn't render consistently
- Used concise bullet-point format for feature list

**Files Changed:** `installer/OmenCoreInstaller.iss`

---

### 🪟 Window Focus Issues (Issue from tester feedback)
**Problem:** Window doesn't get focus when restored from tray unless manually clicked.

**Fix:**
- Added P/Invoke declarations for Win32 window focus APIs
- Implemented `AttachThreadInput` pattern to bypass Windows focus-stealing prevention
- Use `SetForegroundWindow` for reliable foreground activation
- Removed `Topmost` toggle workaround in favor of proper API usage

**Files Changed:** `ViewModels/MainViewModel.cs`

---

## Improvements

### 💻 HP Spectre Power Limit Guidance (Tester feedback)
**Issue:** HP Spectre user reported CPU only hitting 45-50W instead of rated 55W.

**Context:** OmenCore's EC-based power limit control uses OMEN-specific register addresses. HP Spectre laptops have different/unknown EC layouts, so direct CPU/GPU wattage control isn't available.

**Improvement:**
- Added Spectre-specific status message in System Control view
- Message now suggests Intel XTU or ThrottleStop for CPU power limit control
- Provides clear expectations about what OmenCore can control on Spectre systems

**What works on Spectre:**
- ✅ Fan monitoring and control (via WMI)
- ✅ Temperature monitoring
- ✅ Windows power plan switching
- ✅ Fan presets (Auto/Quiet/Performance)
- ❌ Direct CPU/GPU power limits (EC registers differ from OMEN)

**Files Changed:** `ViewModels/SystemControlViewModel.cs`, `ViewModels/MainViewModel.cs`

---

## Technical Details

### Files Modified
- `Services/AutoUpdateService.cs` - Retry logic for file hashing
- `Services/PowerAutomationService.cs` - Exception handling, suspend/resume events
- `Services/HardwareMonitoringService.cs` - Pause/Resume for standby
- `ViewModels/SettingsViewModel.cs` - Low overhead mode sync, power status refresh
- `ViewModels/MainViewModel.cs` - Event subscriptions, P/Invoke for focus, SystemInfoService pass-through
- `ViewModels/SystemControlViewModel.cs` - Spectre-specific power limit messaging
- `Hardware/WmiFanController.cs` - Optimized preset transitions
- `installer/OmenCoreInstaller.iss` - Simplified welcome text

### New Events/Methods
```csharp
// PowerAutomationService
public event EventHandler? SystemSuspending;
public event EventHandler? SystemResuming;

// HardwareMonitoringService
public void Pause();
public void Resume();
public bool IsPaused { get; }

// SettingsViewModel
public event EventHandler<bool>? LowOverheadModeChanged;
public void RefreshPowerStatus();
```

---

## Testing Notes

### Recommended Test Cases
1. **Auto-Update:** Download and verify update (ensure no file locking errors)
2. **Power Events:** Plug/unplug AC adapter, verify no crash and status updates
3. **Low Overhead Mode:** Toggle in Settings, verify charts disappear
4. **Modern Standby:** Close lid, verify fans don't rev
5. **Preset Switching:** Switch between Auto/Max/Quiet, verify fast transitions
6. **Window Focus:** Click tray icon, verify window gains focus

---

## Upgrade Notes

- Direct upgrade from v1.5.0-beta or v1.1.x is supported
- Settings and profiles are preserved during upgrade
- No configuration changes required

---

## Known Issues

- None reported for beta2 (awaiting tester feedback)

---

## Contributors

Thanks to all beta testers who reported these issues!
