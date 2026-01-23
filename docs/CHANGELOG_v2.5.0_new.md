# OmenCore v2.5.0 - Reliability Hardening & RGB Integration Release

**Release Date:** January 21, 2026

This release focuses on reliability hardening, verification systems, advanced RGB lighting integration, and comprehensive hardware monitoring enhancements. Key improvements include power limit verification, improved fan control diagnostics, expanded unit testing, temperature-responsive RGB lighting, performance mode synced effects, throttling indicators, power consumption tracking, battery health monitoring, live fan curve visualization, Victus 16 hardware stability fixes, enhanced monitoring diagnostics, UI/UX improvements for better temperature visibility, automatic system log scrolling, and proper fan auto-control restoration on app shutdown.

---

## 🛡️ Safety & Reliability Improvements

### ✅ **Victus 16 Hardware Stability Fixes** (Reliability)
- **Issue**: Victus 16 laptops experiencing sensor freezes, worker crashes, and unreliable fan control
- **Root Cause**: Insufficient error handling and recovery mechanisms for hardware failures
- **Fixes Implemented**:
  1. ✅ **Enhanced Stuck Sensor Detection**: Automatic hardware reinitialize when CPU/GPU temperature sensors report identical values for 20+ consecutive readings
  2. ✅ **Worker Robustness Improvements**: Replaced permanent worker disable with 30-minute cooldown period for automatic recovery from crashes
  3. ✅ **Fan Control Hardening**: Implemented multi-level retry logic (3 attempts at controller level, 2 attempts at service level) with verification
  4. ✅ **Comprehensive Retry Framework**: Fan commands now retry on failure with 300-500ms delays and detailed attempt logging
- **Reporters**: Multiple Victus 16 users experiencing hardware instability
- **Files**: `LibreHardwareMonitorImpl.cs`, `HardwareWorkerClient.cs`, `WmiFanController.cs`, `FanService.cs`

### ✅ **Fan Auto-Control Restoration on Shutdown** (Safety)
- **Symptom**: Fans remained at manual high-speed settings after closing OmenCore, running indefinitely
- **Root Cause**: Application shutdown didn't restore BIOS/system default fan control
- **Fix**: Modified `FanService.Dispose()` to restore BIOS/system auto-control when application closes
- **Reporter**: Users reporting fans staying at high RPM after app closure
- **Files**: `FanService.cs`

### ✅ **Hardware Monitoring Data Display Fix** (Reliability)
- **Symptom**: GPU temperature and sensor data not appearing in monitoring tab
- **Root Cause**: Asynchronous UI update threading issue with `BeginInvoke` calls
- **Fix**: Changed to `BeginInvoke(DispatcherPriority.Normal)` for reliable UI thread queuing
- **Reporter**: Users unable to see hardware monitoring data
- **Files**: `HardwareMonitoringViewModel.cs`

---

## 🐛 Bug Fixes

### ✅ **Temperature Display Visibility on Startup** (UI/UX)
- **Symptom**: CPU/GPU temperatures not immediately visible when app starts
- **Root Cause**: Default tab set to Monitoring instead of General
- **Fix**: Changed default application tab to General to ensure temperature displays are visible on startup
- **Reporter**: Users confused about temperature visibility
- **Files**: `MainWindow.xaml`, `MainViewModel.cs`

### ✅ **System Log Auto-Scroll** (UI/UX)
- **Symptom**: System logs required manual scrolling to see latest entries
- **Root Cause**: Log display didn't automatically scroll to bottom
- **Fix**: Added automatic scrolling to system logs in footer
- **Reporter**: Users missing important log messages
- **Files**: `MainWindow.xaml.cs`, `LoggingService.cs`

### ✅ **Sensor Freeze Prevention** (Reliability)
- **Symptom**: Temperature sensors reporting stuck values, causing incorrect fan control
- **Root Cause**: No automatic recovery from stuck sensor readings
- **Fix**: Automatic detection and recovery from stuck temperature sensors with hardware reinitialize
- **Reporter**: Users experiencing frozen temperature readings
- **Files**: `LibreHardwareMonitorImpl.cs`

### ✅ **Worker Crash Recovery** (Reliability)
- **Symptom**: Hardware worker crashes permanently disabled monitoring
- **Root Cause**: Permanent worker disable on crash without recovery mechanism
- **Fix**: 30-minute cooldown period allowing automatic recovery from crashes
- **Reporter**: Users experiencing permanent monitoring loss after crashes
- **Files**: `HardwareWorkerClient.cs`

### ✅ **Fan Command Reliability** (Reliability)
- **Symptom**: Silent fan control failures without user notification
- **Root Cause**: Single attempt fan commands with no retry or verification
- **Fix**: Multiple retry attempts with verification prevent silent failures
- **Reporter**: Users reporting fan control not working
- **Files**: `WmiFanController.cs`, `FanService.cs`

---

## ⚡ Performance & Monitoring Enhancements

### ✅ **Power Limits Verification System** (Reliability)
- **What**: Implemented `PowerVerificationService` that applies performance mode power limits and reads back EC registers to verify success
- **Why**: Ensures power limit settings are actually applied to hardware
- **Implementation**: Added `IPowerVerificationService` interface and `PowerLimitApplyResult` class
- **Files**: `PowerVerificationService.cs`, `IPowerVerificationService.cs`

### ✅ **GPU Power Boost Integration** (Performance)
- **What**: Enhanced GPU power boost accuracy with NVAPI TDP limit integration
- **Why**: Provides more accurate GPU power control and status reporting
- **Features**: Combined WMI+NVAPI control with live wattage indicators
- **Files**: `GpuPowerBoostService.cs`, `SystemControlViewModel.cs`

### ✅ **Advanced RGB Lighting System** (Features)
- **What**: Comprehensive temperature-responsive lighting, performance mode synchronization, and throttling indicators
- **Why**: Enhanced visual feedback for system status and performance
- **Support**: HP OMEN, Corsair, Logitech, and Razer devices
- **Features**: 6 new OMEN Light Studio-compatible presets, cross-device RGB sync
- **Files**: `RgbLightingService.cs`, `LightingPresets.cs`

### ✅ **Hardware Monitoring Enhancements** (Features)
- **What**: Added power consumption tracking, battery health monitoring, and live fan curve visualization
- **Why**: Comprehensive system monitoring and diagnostics
- **Features**: Interactive charts, real-time power tracking, battery diagnostics
- **Files**: `HardwareMonitoringService.cs`, `BatteryService.cs`

### ✅ **Fan Curve Stability Improvements** (Reliability)
- **What**: Added GPU power boost level integration and improved hysteresis settings
- **Why**: Prevents fan oscillation and provides stable temperature control
- **Settings**: 4°C dead-zone, 1s ramp-up, 5s ramp-down
- **Files**: `FanService.cs`, `FanCurveService.cs`

---

## 🔧 Technical Improvements

### ✅ **Build System Hardening** (Development)
- **What**: Comprehensive build cleanup resolving all compilation errors and warnings
- **Fixes**:
  - Fixed `AfterburnerGpuData` property naming inconsistencies
  - Corrected `PerformanceModeService` configuration access path
  - Added explicit type casts for `uint` to `int` conversions
  - Implemented missing `IFanVerificationService` interface methods
  - Enhanced nullability safety with proper annotations
- **Result**: Clean Release build with 0 errors, 0 warnings, all 66 unit tests passing
- **Files**: Multiple files across solution

### ✅ **Windows Defender Guidance** (Compatibility)
- **What**: Added comprehensive documentation for WinRing0 false positives
- **Content**: Explains false positive causes, PawnIO recommendations, Defender exclusions, admin-run guidance
- **Files**: `docs/DEFENDER_FALSE_POSITIVE.md`

### ✅ **Settings UX Improvements** (UI/UX)
- **What**: Settings now show Defender guidance and recommend PawnIO when WinRing0 detected
- **Why**: Better user guidance for compatibility issues
- **Files**: `SettingsViewModel.cs`, `SettingsView.xaml`

### ✅ **Diagnostics & Logging Improvements** (Development)
- **What**: Enhanced logging around verification and sensor detection
- **Why**: Aids diagnosis of RPM mismatches and temperature issues
- **Files**: `LoggingService.cs`, `HardwareMonitoringService.cs`

### ✅ **Linux QA & Artifacts** (Development)
- **What**: Added linux-x64 packaging workflow with checksums and testing guide
- **Features**: CI workflow, comprehensive testing checklist
- **Files**: `LINUX_QA_TESTING.md`, GitHub Actions workflow

### ✅ **Diagnostic Export System** (Features)
- **What**: Added `DiagnosticExportService` scaffold and Linux CLI support
- **Features**: JSON bundle capture, `diagnose --export` command
- **Files**: `DiagnosticExportService.cs`, CLI commands

---

## 🎮 User Experience Improvements

### ✅ **Thermal Protection Notifications** (Safety)
- **What**: User notifications when thermal protection activates
- **Why**: Prevents overheating by alerting users to protection activation
- **Display**: Shows temperature and protection level
- **Files**: `ThermalProtectionService.cs`, `NotificationService.cs`

### ✅ **Ctrl+S Hotkey** (UX)
- **What**: Keyboard shortcut to apply currently selected performance mode
- **Why**: Quick settings application without mouse navigation
- **Files**: `MainWindow.xaml.cs`, `SystemControlViewModel.cs`

### ✅ **Auto-Save Settings** (UX)
- **What**: GPU power boost level and performance mode selections auto-save
- **Why**: Eliminates need for manual apply button clicks
- **Files**: `SettingsService.cs`, `SystemControlViewModel.cs`

### ✅ **Fan UI Clarity** (UX)
- **What**: Real-time fan status shows last-updated time, RPM source, thermal protection indicator
- **Why**: Users know when curves are overridden and data freshness
- **Files**: `FanStatusControl.xaml`, `FanService.cs`

---

## 🧪 Testing & Quality Assurance

### ✅ **Hardware Monitoring Diagnostics** (Testing)
- **What**: Added extensive debug logging framework for monitoring issues
- **Coverage**:
  - Monitoring loop diagnostics tracking sample acquisition
  - Metrics update tracking
  - UI update diagnostics
  - End-to-end data flow tracing
- **Files**: `HardwareMonitoringService.cs`, `LoggingService.cs`

### ✅ **Unit Test Expansion** (Quality)
- **What**: Comprehensive test coverage for new services and reliability features
- **Coverage**: All 66 unit tests passing with new reliability and verification tests
- **Files**: Test project files

---

## 📋 Known Issues & Limitations

### **Windows Defender False Positives**
- **Issue**: WinRing0 driver triggers Defender false positives
- **Workaround**: Use PawnIO driver or add Defender exclusions
- **Status**: Documented in `DEFENDER_FALSE_POSITIVE.md`

### **Victus 16 Sensor Stability**
- **Issue**: Some Victus 16 models may still experience occasional sensor issues
- **Status**: Significantly improved with automatic recovery, monitoring for further improvements

### **RGB Lighting Compatibility**
- **Issue**: Some third-party RGB devices may have limited effect support
- **Status**: Core temperature and performance mode sync working, advanced effects device-dependent

---

## 🔄 Migration Notes

- **Settings Auto-Save**: Performance mode and GPU power boost settings now save automatically
- **Fan Control**: Fans properly return to system control on application exit
- **Default Tab**: Application now opens to General tab for immediate temperature visibility
- **PawnIO Recommendation**: Settings will prompt PawnIO installation when WinRing0 issues detected

---

## 🙏 Acknowledgments

Special thanks to all beta testers and users who provided feedback on Victus 16 stability, fan control reliability, and RGB lighting integration. Your detailed bug reports and testing efforts made this release possible.