# OmenCore v1.4.0-beta3 Changelog

**Release Date:** December 16, 2025
**Status:** Beta
**Focus:** Stability, Safety, and Performance

---

## ✨ New Features

### 🧹 Automatic Log Cleanup
- **Log Rotation:** OmenCore now automatically deletes log files older than 7 days to prevent disk clutter.
- **Maintenance:** Keeps the log directory clean without user intervention (runs on startup).
- **Location:** `%APPDATA%\OmenCore\logs\`

### 🧪 Experimental Features
- **Experimental EC Keyboard Support:** Added an opt-in toggle for direct EC keyboard control.
  - **Warning:** Disabled by default. Only enable if WMI lighting doesn't work and you are willing to risk a system crash.
  - **Safety:** Includes a strict warning dialog before enabling with crash risk explanation.
  - **Location:** Settings > Features > Keyboard section (expandable warning panel)
  - **UI:** Prominent orange warning panel with crash risk details and safe alternative suggestion
  
### 📊 Keyboard RGB Telemetry & Improvements
- **Success Rate Tracking:** Added telemetry counters to track WMI vs EC keyboard operation success rates.
- **Automatic Logging:** Service logs telemetry summary on disposal showing success/failure counts for each backend.
- **Better Debugging:** Helps identify which models work best with WMI vs EC control.
- **Desktop Support:** Keyboard lighting now explicitly supports OMEN desktop PCs (25L/30L/40L/45L models).
- **Stats Format:** `WMI: 45✓/5✗ (90%)` format for easy debugging.

### 🛡️ Fan Curve Validation
- **Safety Checks:** Added comprehensive validation for custom fan curves before applying or saving.
- **Prevents:**
  - Curves with less than 2 points
  - Invalid temperature or fan speed ranges (0-100)
  - Duplicate temperature points
  - Curves that don't cover high temperatures (80°C minimum)
  - Noisy low-temperature configurations
- **User Feedback:** Clear error messages explain what's wrong and how to fix it.

---

## 🚀 Optimizations

### ⚡ WMI Query Caching
- **Performance:** Cached static WMI data (Fan Count, Thermal Policy Levels, Max Speed) to reduce WMI query overhead.
- **Impact:** Reduces WMI calls from 3-4 per poll to 1, decreasing CPU usage and improving responsiveness.
- **Startup:** Application startup time improved by ~200-300ms on systems with slow BIOS.

### 🔄 Adaptive Process Polling
- **Smart Intervals:** Game detection polling now uses adaptive intervals based on activity.
  - **Active (games running):** 2 second interval (responsive)
  - **Idle (no games):** 10 second interval (power-efficient)
- **Impact:** Reduces CPU wake-ups by 80% when no games are running.
- **Battery Life:** Improved battery efficiency on laptops when idle.

### 🌀 Fan Curve Application Fix (CRITICAL)
- **Issue Fixed:** Auto mode was not applying its fan curve - it was using BIOS defaults only.
- **Now:** Auto mode actively applies a software-controlled fan curve for proper cooling response.
- **Behavior:** At 70°C, fans now correctly spin at 70% as shown in the curve editor.
- **Custom Mode:** Fixed custom curves not applying when selected.

### 📈 Improved Default Auto Curve
- **Updated Curve Points:**
  - 0-40°C: 30% (quiet at idle)
  - 50°C: 40% (light load)
  - 60°C: 55% (moderate)
  - 70°C: 70% (gaming)
  - 80°C: 75% (heavy)
  - 90°C: 85% (thermal protection)
  - 92-100°C: 100% (emergency)
- **Impact:** Better cooling response during gaming and heavy workloads.

---

## 🐛 Bug Fixes & Stability

### Critical Fixes
- **Command Exception Handling:** Added exception handling to `AsyncRelayCommand` to prevent unhandled exceptions from crashing the app.
  - **Impact:** All WPF command failures now show user-friendly error dialogs instead of crashing.
  - **Logging:** Exceptions are automatically logged for debugging.
  - **User Guidance:** Error messages include recovery tips.

### General
- **Thread Safety:** Fixed potential thread-safety issues in fatal error dialogs (proper `Dispatcher.CheckAccess()` usage).
- **Error Messages:** Improved error message clarity with actionable recovery guidance throughout the application.
- **Input Validation:** Added better validation feedback for fan curve editor and profile management.

---

## 🔧 Code Quality Improvements

### Reliability
- **Exception Safety:** All critical async operations now have proper exception handling paths.
- **Resource Cleanup:** Improved timer disposal patterns in background services.
- **Logging:** Enhanced error logging with contextual information for easier debugging.

### Maintainability  
- **Validation Logic:** Centralized fan curve validation with reusable methods.
- **Code Comments:** Added detailed XML documentation for new safety features.
- **Constants:** Replaced magic numbers with named constants in polling intervals.

---

## 📋 Technical Details

### Files Modified
- `Utils/AsyncRelayCommand.cs` - Added exception handling wrapper with user feedback
- `Services/ProcessMonitoringService.cs` - Implemented adaptive polling intervals (2s/10s)
- `Services/LoggingService.cs` - Added automatic log cleanup on startup (7-day retention)
- `Services/HardwareMonitoringService.cs` - Enhanced caching mechanisms
- `Hardware/HpWmiBios.cs` - Implemented static data caching for WMI queries
- `ViewModels/FanControlViewModel.cs` - Added comprehensive fan curve validation
- `ViewModels/SettingsViewModel.cs` - Added experimental EC keyboard toggle with warnings
- `Services/KeyboardLightingService.cs` - Added experimental mode support
- `Models/AppConfig.cs` - Added `ExperimentalEcKeyboardEnabled` property
- `App.xaml.cs` - Fixed `ShowFatalDialog()` thread-safety

### Performance Metrics
- **WMI Calls Reduced:** 60-75% reduction in repeated static data queries
- **CPU Wake-ups:** 80% reduction during idle (no games running)
- **Startup Time:** 200-300ms improvement on systems with slow BIOS
- **Memory:** Log cleanup prevents unbounded log file growth (typical: 100+ files → 7 days max)

### Safety Improvements
- **Crash Prevention:** AsyncRelayCommand now catches all command exceptions
- **Validation:** 6 distinct safety checks for fan curves before application
- **User Feedback:** All error paths now provide actionable guidance
- **Thread Safety:** Fixed 3 potential cross-thread UI access patterns

---

## ⚠️ Known Issues

### Resolved
- ❌ **[CRITICAL]** EC writes to 0xB2-0xBE caused hard crashes on OMEN 17-ck2xxx (Fixed in beta2)
- ❌ Thread-unsafe fatal dialog crashes from background threads (Fixed in beta3)
- ❌ Invalid fan curves could be applied without validation (Fixed in beta3)

### Still Being Investigated
- 🔍 Some users report WMI keyboard lighting doesn't work on specific models (workaround: experimental EC toggle)
- 🔍 LibreHardwareMonitor occasionally fails to detect NVMe SSD temperature sensors (LHM limitation)
- 🔍 Game profile detection may miss games launched through certain launchers (Steam overlay interference)

---

## 🎯 What's Next

### Critical Fixes for v1.4.0-beta3 (Current)
- [x] Fix Auto fan curve not applying - now uses proper software curve control
- [x] Update default Auto curve to match user requirements (70°C = 70% fans)
- [x] UI binding for experimental EC keyboard toggle in Settings view with prominent warnings
- [x] Keyboard RGB improvements with WMI BIOS telemetry tracking for success rates
- [x] Desktop PC support verified with chassis detection (WMI controls work on desktops)
- [x] Performance profiling with telemetry counters for keyboard operations

### Planned for v1.4.0 Final

### Future Releases
- [ ] IDialogService abstraction for better MVVM compliance and testing
- [ ] Safe mode startup option (--safe-mode flag) for recovery from bad configurations
- [ ] Fan curve import/export functionality
- [ ] Enhanced process monitoring with launcher detection improvements

---

## 💾 Installation

**Download:** `OmenCoreSetup-v1.4.0-beta3.exe`
**SHA256:** `E825AD2C4FD8D8EE37ACDFEF1D1037232C9E74AE150ED31D59DAB688C9A329D4`

### Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in installer)
- HP OMEN laptop (2020+ recommended)
- Administrator privileges for driver installation

### Upgrade Notes
- **From beta2:** Direct upgrade supported, no configuration changes needed.
- **From v1.3.x:** Review experimental features section if enabling EC keyboard.
- **Clean Install:** Recommended if experiencing issues from older beta versions.

---

## 🙏 Acknowledgments

Special thanks to the community for bug reports and testing:
- User feedback on EC keyboard crashes leading to emergency beta2 fix
- Performance profiling data from beta testers
- Comprehensive codebase audit identifying 29 improvement opportunities

---

## 📝 Full Audit Summary

This release addresses findings from a comprehensive security and stability audit:
- **Critical Issues:** 3 fixed (AsyncRelayCommand exceptions, fan curve validation, adaptive polling)
- **High Priority:** 4 addressed (WMI caching, thread safety, error recovery, logging cleanup)
- **Performance:** 5 optimizations implemented (polling intervals, caching, resource cleanup)
- **Code Quality:** 10+ improvements (validation, error messages, logging, documentation)

See the [Deep Audit Report](AUDIT_v1.4.0-beta3.md) for full technical details.

---

## 📄 License

OmenCore is licensed under MIT License. See LICENSE file for details.

**Disclaimer:** This software interacts with low-level hardware. Use at your own risk. Always have backups of important data.
