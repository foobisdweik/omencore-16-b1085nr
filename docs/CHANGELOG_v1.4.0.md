# OmenCore v1.4.0-beta3 Changelog

**Release Date:** December 16, 2025
**Status:** Beta 3
**Focus:** System Maintenance, RGB Improvements, Corsair Detection, and Bloatware Removal

---

## ✨ New Features

### 🗑️ HP Bloatware Removal Tool
- **One-Click Scanner:** Detects HP pre-installed bloatware apps (AD2F1837.HP* packages) using PowerShell
- **Safe Removal:** Uninstalls bloatware with confirmation dialog and safety warnings
- **Smart Protection:** Automatically preserves HP Support Assistant for driver updates
- **Detailed Results:** Shows count and list of detected packages in scrollable view
- **Warning System:** Clear warnings about irreversibility and restart recommendations
- **Progress Indicator:** Step-by-step progress during removal (Scanning → Removing X/Y → Complete)
- **Location:** Settings tab → HP Bloatware Removal section

### 🧹 Automatic Log Cleanup
- **Log Rotation:** OmenCore now automatically deletes log files older than 7 days to prevent disk clutter
- **Maintenance:** Keeps the log directory clean without user intervention (runs on startup)
- **Location:** `%APPDATA%\OmenCore\logs\`

### 🧪 Experimental EC Keyboard Support
- **Opt-in Toggle:** Direct EC keyboard control for models where WMI doesn't work
  - **Warning:** Disabled by default - only enable if WMI lighting doesn't work
  - **Safety:** Includes strict warning dialog before enabling with crash risk explanation
  - **Location:** Settings > Features > Keyboard section (expandable warning panel)
  - **UI:** Prominent orange warning panel with crash risk details and safe alternative suggestion
  
### 📊 Keyboard RGB Telemetry & Improvements
- **Success Rate Tracking:** Added telemetry counters to track WMI vs EC keyboard operation success rates
- **Automatic Logging:** Service logs telemetry summary showing success/failure counts for each backend
- **Better Debugging:** Helps identify which models work best with WMI vs EC control
- **Desktop Support:** Keyboard lighting now explicitly supports OMEN desktop PCs (25L/30L/40L/45L models)
- **Stats Format:** `WMI: 45✓/5✗ (90%)` format for easy debugging
- **User Hints:** Suggests enabling experimental EC mode if WMI has 0% success rate

### 🛡️ Fan Curve Validation
- **Safety Checks:** Added comprehensive validation for custom fan curves before applying or saving
- **Prevents:**
  - Curves with less than 2 points
  - Invalid temperature or fan speed ranges (0-100)
  - Duplicate temperature points
  - Curves that don't cover high temperatures (80°C minimum)
  - Noisy low-temperature configurations
- **User Feedback:** Clear error messages explain what's wrong and how to fix it

---

## 🚀 Optimizations

### ⚡ WMI Query Caching
- **Performance:** Cached static WMI data (Fan Count, Thermal Policy Levels, Max Speed) to reduce WMI query overhead
- **Impact:** Reduces WMI calls from 3-4 per poll to 1, decreasing CPU usage and improving responsiveness
- **Startup:** Application startup time improved by ~200-300ms on systems with slow BIOS

### 🔄 Adaptive Process Polling
- **Smart Intervals:** Game detection polling now uses adaptive intervals based on activity
  - **Active (games running):** 2 second interval (responsive)
  - **Idle (no games):** 10 second interval (power-efficient)
- **Impact:** Reduces CPU wake-ups by 80% when no games are running
- **Battery Life:** Improved battery efficiency on laptops when idle

### 🌀 Fan Curve Application Fix (CRITICAL)
- **Issue Fixed:** Auto mode was not applying its fan curve - it was using BIOS defaults only
- **Now:** Auto mode actively applies a software-controlled fan curve for proper cooling response
- **Behavior:** At 70°C, fans now correctly spin at 70% as shown in the curve editor
- **Custom Mode:** Fixed custom curves not applying when selected

### 📈 Improved Default Auto Curve
- **Updated Curve Points:**
  - 0-40°C: 30% (quiet at idle)
  - 50°C: 40% (light load)
  - 60°C: 55% (moderate)
  - 70°C: 70% (gaming)
  - 80°C: 75% (heavy)
  - 90°C: 85% (thermal protection)
  - 92-100°C: 100% (emergency)
- **Impact:** Better cooling response during gaming and heavy workloads

---

## 🐛 Bug Fixes & Stability

### Critical Fixes
- **Command Exception Handling:** Added exception handling to `AsyncRelayCommand` to prevent unhandled exceptions from crashing the app
  - **Impact:** All WPF command failures now show user-friendly error dialogs instead of crashing
  - **Logging:** Exceptions are automatically logged for debugging
  - **User Guidance:** Error messages include recovery tips

### Keyboard RGB Fixes (beta3)
- **ColorTable Format:** Fixed keyboard RGB color table structure to match OmenMon's 128-byte format
  - **Issue:** Colors were placed at wrong offset (byte 0 instead of byte 25)
  - **Fix:** Proper structure: ZoneCount at byte 0, 24-byte padding, colors at byte 25+
  - **Impact:** Should fix keyboards that report WMI success but don't change colors
- **Keyboard Type Detection:** Added `GetKeyboardType()` method to detect Standard/TenKeyLess/PerKeyRgb keyboards
- **Backlight Check:** Added `HasBacklight()` method to verify keyboard backlight support

### XTU Detection Fix (beta3)
- **Issue:** XTU was detected by checking running processes, but XTU runs as a service
- **Fix:** Now uses `ServiceController` to check for XTU3SERVICE and XTUOCDriverService
- **Impact:** Correctly detects XTU when installed, allowing proper undervolting warnings

### OGH Cleanup Enhancement (beta3)
- **Added Services:** HpTouchpointAnalyticsService, HPDiagsCap added to cleanup list
- **Impact:** More thorough OMEN Gaming Hub removal

### Corsair Device Detection (beta3)
- **New Device Type:** Added `WirelessDongle` type to properly identify USB receivers
- **Dark Core RGB PRO:** Fixed detection - 0x1B80 is the mouse, 0x1B81 is the receiver
- **Fixed PIDs:**
  - 0x1B80 → Dark Core RGB PRO Wireless (mouse)
  - 0x1B81 → Dark Core RGB PRO Receiver (dongle)
  - 0x1BA4 → Dark Core RGB PRO SE Wireless (mouse variant)
  - 0x0A4E → HS70 PRO Wireless Receiver (headset dongle)
- **Better Logging:** Shows 📡 for receivers, 🎮 for actual devices
- **Notes Field:** Added device notes to explain limitations (e.g., "USB receiver for Dark Core RGB PRO mouse")

### General
- **Thread Safety:** Fixed potential thread-safety issues in fatal error dialogs (proper `Dispatcher.CheckAccess()` usage)
- **Error Messages:** Improved error message clarity with actionable recovery guidance throughout the application
- **Input Validation:** Added better validation feedback for fan curve editor and profile management
- **XAML Resources:** Fixed `BooleanToVisibilityConverter` resource name mismatch causing startup crashes

---

## 🔧 Code Quality Improvements

### Reliability
- **Exception Safety:** All critical async operations now have proper exception handling paths
- **Resource Cleanup:** Improved timer disposal patterns in background services
- **Logging:** Enhanced error logging with contextual information for easier debugging

### Maintainability  
- **Validation Logic:** Centralized fan curve validation with reusable methods
- **Code Comments:** Added detailed XML documentation for new safety features
- **Constants:** Replaced magic numbers with named constants in polling intervals

### New Converters
- **InverseBooleanConverter:** For inverted boolean bindings
- **StringNotEmptyToVisibilityConverter:** Show/hide UI based on string content
- **IntGreaterThanZeroToVisibilityConverter:** Show/hide UI based on numeric values

---

## 📋 Technical Details

### Files Modified
- `Utils/AsyncRelayCommand.cs` - Added exception handling wrapper with user feedback
- `Services/ProcessMonitoringService.cs` - Implemented adaptive polling intervals (2s/10s)
- `Services/LoggingService.cs` - Added automatic log cleanup on startup (7-day retention)
- `Services/HardwareMonitoringService.cs` - Enhanced caching mechanisms
- `Hardware/HpWmiBios.cs` - Implemented static data caching for WMI queries
- `ViewModels/FanControlViewModel.cs` - Added comprehensive fan curve validation
- `ViewModels/SettingsViewModel.cs` - Added experimental EC keyboard toggle and bloatware removal
- `Services/KeyboardLightingService.cs` - Added experimental mode support and telemetry
- `ViewModels/LightingViewModel.cs` - Added telemetry logging and user hints
- `Models/AppConfig.cs` - Added `ExperimentalEcKeyboardEnabled` property
- `App.xaml.cs` - Fixed `ShowFatalDialog()` thread-safety
- `Views/SettingsView.xaml` - Added bloatware removal UI section
- `Utils/InverseBooleanConverter.cs` - New converter for inverted boolean bindings
- `Utils/StringNotEmptyToVisibilityConverter.cs` - New converter for string-based visibility
- `Utils/IntGreaterThanZeroToVisibilityConverter.cs` - New converter for numeric-based visibility

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
- **Bloatware Safety:** Confirmation dialog with warnings before removal

---

## ⚠️ Known Issues

### Resolved
- ❌ **[CRITICAL]** EC writes to 0xB2-0xBE caused hard crashes on OMEN 17-ck2xxx (Fixed in beta2)
- ❌ Thread-unsafe fatal dialog crashes from background threads (Fixed in beta3)
- ❌ Invalid fan curves could be applied without validation (Fixed in beta3)
- ❌ XAML resource name mismatch causing startup crash (Fixed in v1.4.0)

### Still Being Investigated
- 🔍 **Keyboard RGB doesn't work** - WMI reports success but colors don't change on most models (complete rework planned for v1.5)
- 🔍 LibreHardwareMonitor occasionally fails to detect NVMe SSD temperature sensors (LHM limitation)
- 🔍 Game profile detection may miss games launched through certain launchers (Steam overlay interference)

---

## 🎯 What's Next

### Future Releases
- [ ] IDialogService abstraction for better MVVM compliance and testing
- [ ] Safe mode startup option (--safe-mode flag) for recovery from bad configurations
- [ ] Fan curve import/export functionality
- [ ] Enhanced process monitoring with launcher detection improvements
- [ ] Additional bloatware detection patterns for newer HP packages

---

## 💾 Installation

**Download:** `OmenCoreSetup-1.4.0-beta3.exe`
**SHA256:** `C87CDF40051ED8D9BB401937C5FBED1ADB150ED01B9C04F618578CBCF3718CCE`

### Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in installer)
- HP OMEN laptop (2020+ recommended) or OMEN desktop (25L/30L/40L/45L)
- Administrator privileges for driver installation

### Upgrade Notes
- **From beta3:** Direct upgrade supported, all features preserved
- **From v1.3.x:** Review experimental features section if enabling EC keyboard
- **Clean Install:** Recommended if experiencing issues from older beta versions

---

## 🙏 Acknowledgments

Special thanks to the community for bug reports and testing:
- User feedback on EC keyboard crashes leading to emergency beta2 fix
- Performance profiling data from beta testers
- Comprehensive codebase audit identifying 29 improvement opportunities
- Desktop PC compatibility testing from OMEN 30L users
- Bloatware removal feature requests and testing

---

## 📝 Full Audit Summary

This release addresses findings from a comprehensive security and stability audit:
- **Critical Issues:** 3 fixed (AsyncRelayCommand exceptions, fan curve validation, adaptive polling)
- **High Priority:** 5 addressed (WMI caching, thread safety, error recovery, logging cleanup, XAML resources)
- **Performance:** 5 optimizations implemented (polling intervals, caching, resource cleanup)
- **Code Quality:** 10+ improvements (validation, error messages, logging, documentation)
- **New Features:** 2 major additions (bloatware removal, experimental EC keyboard)

---

## 📄 License

OmenCore is licensed under MIT License. See LICENSE file for details.

**Disclaimer:** This software interacts with low-level hardware. Use at your own risk. Always have backups of important data.
