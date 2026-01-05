# Changelog v2.1.1

All notable changes to OmenCore v2.1.1 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.1.1] - 2026-01-05

### Fixed

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

### Changed

#### 🐧 Linux Release Workflow
- **Improvement:** GitHub release workflow now builds and includes Linux CLI
- **New artifact:** `omencore-linux.tar.gz` in releases
- **Workflow:** Split into separate Windows and Linux build jobs, then combined for release
- **Files changed:** `.github/workflows/release.yml`

### Known Issues

#### ⚠️ System Stuttering on OMEN Max 16 (RTX 5080)
- **Reported:** Some users with OMEN Max 16 (Ryzen AI 9 370 + RTX 5080) experience system-wide stuttering (~1.2 second intervals) when OmenCore is running
- **Suspected Cause:** LibreHardwareMonitor polling issues with new RTX 50-series GPUs
- **Workaround:** Enable "Low Overhead Mode" in Settings to reduce polling frequency
- **Status:** Under investigation

#### ⚠️ Desktop OMEN 30L Limited Support
- **Reported:** OMEN Desktop 30L GT13-1xxx has limited feature support
- **Status:** Performance modes may not function on desktop models; OmenCore is primarily designed for OMEN laptops
- **Workaround:** Continue using OMEN Gaming Hub for desktop systems

---

## Download

- **Windows Installer:** `OmenCoreSetup-2.1.1.exe` (recommended)
- **Windows Portable:** `OmenCore-2.1.1-win-x64.zip`
- **Linux CLI:** `omencore-linux.tar.gz`

## Checksums (SHA256)

*To be filled after build*
