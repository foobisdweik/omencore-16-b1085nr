# Changelog v2.1.1

All notable changes to OmenCore v2.1.1 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.1.1] - 2026-01-05

### Fixed

#### 🆕 OMEN Max 2025 Fan RPM Showing 0% (ThermalPolicy V2)
- **Issue:** OMEN Max 16/17 (2025) with RTX 50-series showed 0% / 0 RPM for fans even though fans were spinning
- **Root Cause:** OMEN Max 2025 uses ThermalPolicy V2 which requires different WMI commands for fan level reading. The existing V1 commands (0x2D) return 0 on V2 devices.
- **Fix:** 
  - Added `ThermalPolicyVersion.V2` enum for OMEN Max 2025+ detection
  - Added new fan commands: `CMD_FAN_GET_LEVEL_V2` (0x37) and `CMD_FAN_GET_RPM` (0x38)
  - Enhanced `GetFanLevel()` to try V2 commands first on V2 devices, fallback to V1
  - Added logging for V2 device detection
- **Files changed:** `HpWmiBios.cs`

#### 🔧 Tray Icon: Minimize to Tray Not Working
- **Issue:** Clicking the X button (or Alt+F4) with "Minimize to tray on close" enabled would hide the window but make the app unresponsive. Clicking the tray icon wouldn't bring the window back, requiring Task Manager to kill the process.
- **Root Cause:** `MainWindow_Closing` event was disposing the ViewModel without checking the `MinimizeToTrayOnClose` setting. The window was disposed but the process kept running without a usable UI.
- **Fix:** 
  - Added `MinimizeToTrayOnClose` check in `MainWindow_Closing` event
  - Cancel close event and hide to tray when setting is enabled
  - Added `ForceClose()` method for actual app shutdown (tray menu Exit)
  - Restore `ShowInTaskbar = true` when showing window from tray
- **Files changed:** `MainWindow.xaml.cs`, `App.xaml.cs`

#### ⚡ Performance Mode Resets After 5-10 Minutes
- **Issue:** Performance mode setting would reset after 5-10 minutes, causing TDP to drop even though the UI still showed "Performance" as active.
- **Root Cause:** `SetPerformanceMode()` wasn't starting the countdown extension timer. HP BIOS automatically reverts fan/performance settings after ~120 seconds, but only fan presets were maintaining the countdown extension.
- **Fix:** Added countdown extension start/stop in `SetPerformanceMode()`:
  - Performance and Cool modes now start countdown extension to maintain TDP
  - Default mode stops countdown extension and lets BIOS take control
- **Files changed:** `WmiFanController.cs`

#### 🌡️ Stale CPU Temperature in Fan Curves
- **Issue:** FanService reads CPU temperature via `GetCpuTemperature()` which returned cached values without checking freshness. Fan curves could respond to stale (old) temperatures when polling loops were out of sync.
- **Root Cause:** `GetCpuTemperature()` and `GetGpuTemperature()` directly returned cached values without verifying the cache was fresh (within `_cacheLifetime`).
- **Fix:** Added `EnsureCacheFresh()` method that checks `_lastUpdate` against `_cacheLifetime`:
  - **In-process mode:** Calls `UpdateHardwareReadings()` synchronously
  - **Worker mode:** Requests fresh sample from HardwareWorker with 500ms timeout
- **Files changed:** `LibreHardwareMonitorImpl.cs`

#### 🚫 OMEN Desktop Detection and Blocking (CRITICAL)
- **Issue:** OmenCore was causing severe issues on OMEN Desktop PCs (30L, 35L, 40L, 45L):
  - Only one fan detected on startup
  - All fans stop spinning
  - Fans disappear from BIOS (listed as 'inactive' in OGH)
  - Requires CMOS reset to restore functionality
- **Root Cause:** Desktop OMEN systems use completely different thermal management hardware than laptops. WMI commands designed for laptop EC/BIOS write to inappropriate addresses on desktop motherboards, corrupting fan controller configuration.
- **Fix:** Added critical desktop detection at app startup:
  - Detects OMEN Desktop models by name (25L, 30L, 35L, 40L, 45L, Obelisk)
  - Detects desktop chassis types via Win32_SystemEnclosure
  - Shows error dialog explaining incompatibility
  - Prevents app startup to protect hardware
- **Files changed:** `App.xaml.cs`
- **Note:** OmenCore is designed for **OMEN LAPTOPS ONLY**. Desktop support may come in future for RGB-only control.

### Changed

#### ⚡ Reduced Default Polling Interval (Performance)
- **Change:** Lowered default hardware polling from 1500ms to 2000ms
- **Benefit:** Reduces CPU overhead and UI sluggishness, especially on newer hardware (RTX 50-series)
- **Impact:** Slightly less frequent monitoring updates, but significantly smoother UI
- **Files changed:** `DefaultConfiguration.cs`, `SettingsViewModel.cs`
- **Note:** Users can still customize interval in Settings > Monitoring

#### 🎯 Quick Popup as Default Tray Action
- **Change:** Left-click on tray icon now shows quick popup instead of full window
- **Behavior:** 
  - **Left-click:** Quick popup with temps, fan speeds, and mode buttons (G-Helper style)
  - **Double-click:** Open full dashboard window
  - **Right-click:** Context menu
- **Benefit:** Faster access to status and controls without opening full UI
- **Files changed:** `App.xaml.cs`

#### 🐧 Linux Release Workflow
- **Improvement:** GitHub release workflow now builds and includes Linux CLI
- **New artifact:** `omencore-linux.tar.gz` in releases
- **Workflow:** Split into separate Windows and Linux build jobs, then combined for release
- **Files changed:** `.github/workflows/release.yml`

### Known Issues

#### ⚠️ Keyboard RGB Not Working on Some Models
- **Reported:** Model xd0015ax shows no keyboard RGB functionality
- **Suspected Cause:** Model-specific SDK support missing or different RGB API
- **Status:** Under investigation
- **Workaround:** Use OMEN Gaming Hub for keyboard RGB control

#### ⚠️ System Stuttering on OMEN Max 16 (RTX 5080)
- **Reported:** Some users with OMEN Max 16 (Ryzen AI 9 370 + RTX 5080) experience system-wide stuttering (~1.2 second intervals) when OmenCore is running
- **Suspected Cause:** LibreHardwareMonitor polling issues with new RTX 50-series GPUs
- **Workaround:** Enable "Low Overhead Mode" in Settings or increase polling interval to 3000-5000ms
- **Status:** Under investigation

#### ⚠️ OMEN Desktop Systems NOT SUPPORTED
- **Critical:** OMEN Desktop PCs (25L, 30L, 35L, 40L, 45L) are **NOT compatible** with OmenCore
- **Risk:** Running on desktops can corrupt BIOS fan controller settings, requiring CMOS reset
- **Protection:** App now blocks startup on detected desktop systems with error message
- **Status:** Desktop support may be added in future for RGB-only control (no fan control)

---

## Download

- **Windows Installer:** `OmenCoreSetup-2.1.1.exe` (recommended)
- **Windows Portable:** `OmenCore-2.1.1-win-x64.zip`
- **Linux CLI:** `omencore-linux.tar.gz`

## Checksums (SHA256)

```
OmenCoreSetup-2.1.1.exe:        4AC115DF7BEF7EB8503C79C3B9C456FF844BC18BA76D92102BCA05FBC7F355B5
OmenCore-2.1.1-win-x64.zip:     DC2114D3E62C9EF16F9607D5FBA78F1CE986AEBAE7F162284E3C663774922C24
omencore-linux-2.1.1.tar.gz:    8D269CBAE840C7B0D3BDB615637BE535141D8AC11CCC2097548EE2AB6581C238
```
