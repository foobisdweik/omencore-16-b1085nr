# OmenCore v2.5.1 - Fan "Max" Reliability & Diagnostics Release

**Release Date:** January 24, 2026

This release focuses on making the "Max" fan preset robust and observable, increases diagnostics to capture failures, and fixes UI mismatches where the GUI could show 100% while fans were not actually at max. Includes critical safety improvements for fan curve protection and enhanced throttling detection.

---

## 🛡️ Safety Improvements

### 🔴 **CRITICAL: Fan Curve Safety Bounds** (Safety)
- **Issue**: Custom fan curves could be configured too aggressively, risking hardware damage from overheating
- **Root Cause**: No safety limits on fan curve interpolation - curves could theoretically go below minimum safe speeds at high temperatures
- **Fixes Implemented**:
  1. ✅ **Multi-layer thermal clamping** with emergency protection at 88°C (forces 100% fans)
  2. ✅ **Progressive minimum speeds** (80% at 80°C, 60% at 70°C, 40% at 60°C, 20% at 50°C, 10% base)
  3. ✅ **Emergency override** that bypasses all other controls when temperatures exceed 88°C
  4. ✅ **Applied to all curve types** (unified and independent fan curves)
- **Reporter**: Internal safety review

### 🟠 **MEDIUM: EDP Throttling Detection** (Safety)
- **Issue**: CPU power throttling not properly detected, leading to performance issues during gaming/high load
- **Root Cause**: LibreHardwareMonitor sensor-based detection may miss some throttling events
- **Fixes Implemented**:
  1. ✅ **MSR-based throttling detection** using IA32_THERM_STATUS register (0x19C)
  2. ✅ **Dual detection method** - sensor fallback with MSR enhancement
  3. ✅ **Secure Boot compatibility** via PawnIO MSR access
  4. ✅ **Enhanced monitoring samples** with MSR throttling status
- **Reporter**: Performance testing feedback

---

## 🐛 Bug Fixes

### ✅ **GUI: Max Preset Shows 100% But Fans Don't Spin** (Fixed)
- **Symptom**: UI indicated fans were at 100% while fans stayed at low RPM or off
- **Root Cause**: UI flagged Max mode active immediately after SetFanMax commands, but hardware never actually responded on some BIOS/EC combinations
- **Fix**: Added verification loop (3 attempts, 500ms intervals) reading ReadFanSpeeds() and checking duty/RPM. If verification fails, reverts manual state and returns error
- **Reporters**: Multiple users in Discord and GitHub issues
- **Files**: `src/OmenCoreApp/Hardware/WmiFanController.cs`, `src/OmenCoreApp/Services/FanService.cs`

### ✅ **Fan Control: WMI/EC Command Reliability** (Improved)
- **Symptom**: Fan commands accepted but not applied, BIOS reverts settings after 120 seconds
- **Root Cause**: Some firmware needs alternate sequences, commands may not reflect immediately, BIOS timeout issues
- **Fix**: Apply-and-verify loops with retries, OmenMon-style continuous re-application (15-second intervals), fallback sequences for different BIOS versions
- **Reporters**: @kastenbier2743, @xenon205 (GitHub #44), OMEN Max/17-ck users
- **Files**: `src/OmenCoreApp/Hardware/WmiFanController.cs`, `src/OmenCoreApp/Hardware/HpWmiBios.cs`

### ✅ **Hardware Monitoring: Temperature Freezing on Drive Sleep** (Fixed)
- **Symptom**: Temps freeze when storage drives sleep, SafeFileHandle disposal errors
- **Root Cause**: Hardware monitoring calls during drive sleep cause handle disposal
- **Fix**: Added exception handling around hardware.Update() calls and WMI BIOS temperature fallback method
- **Reporters**: Discord users reporting temp freeze issues
- **Files**: `src/OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs`

### ✅ **Hardware Monitoring: CPU Temperature Display** (Fixed)
- **Symptom**: CPU temperatures showed 0°C in GUI despite worker logging correct values
- **Root Cause**: IPC pipe communication failure in out-of-process worker mode, LibreHardwareMonitor CPU temp reading failures without proper MSR access
- **Fix**: Switched back to out-of-process worker mode to leverage proven PawnIO MSR implementation in hardware worker, added PawnIO CPU temp fallback in main app
- **Reporters**: v2.5.1 testing feedback
- **Files**: `src/OmenCoreApp/ViewModels/MainViewModel.cs`, `src/OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs`, `src/OmenCore.HardwareWorker/Program.cs`

### ✅ **Hardware Monitoring: RAM Total Display** (Fixed)
- **Symptom**: Monitoring tab showed "0.0 / 0 GB" for RAM usage while system tray showed correct values
- **Root Cause**: LibreHardwareMonitor "Memory Available" sensor missing on some systems, causing RAM total calculation to fail (total = used + available, but available = 0)
- **Fix**: Added WMI fallback to get system RAM total when sensor unavailable, cached during initialization for performance
- **Reporters**: User testing feedback showing UI inconsistency
- **Files**: `src/OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs`

### ✅ **Diagnostics: Fan Diagnostic Tool Fallback** (Fixed)
- **Symptom**: Fan diagnostic tool showed "WMI BIOS not available" error on systems with Secure Boot
- **Root Cause**: FanVerificationService only used WMI BIOS commands, but WMI is non-functional on Secure Boot systems
- **Fix**: Added EC fallback using FanService when WMI unavailable, implemented per-fan control in EC controller (REG_FAN1_SPEED_PCT/REG_FAN2_SPEED_PCT registers)
- **Reporters**: v2.5.1 testing feedback
- **Files**: `src/OmenCoreApp/Services/FanVerificationService.cs`, `src/OmenCoreApp/Hardware/FanController.cs`, `src/OmenCoreApp/Services/FanService.cs`

### ✅ **Fan Control: Per-Fan EC Register Support** (Added)
- **Symptom**: EC controller only supported unified fan control, limiting diagnostic capabilities
- **Root Cause**: FanController.WriteDuty() set both fans to same percentage, no individual fan control
- **Fix**: Added SetFanSpeeds() method with separate CPU/GPU percentage registers, updated all IFanController implementations
- **Reporters**: Diagnostic tool enhancement request
- **Files**: `src/OmenCoreApp/Hardware/FanControllerFactory.cs`, `src/OmenCoreApp/Hardware/FanController.cs`

### ✅ **EDP Throttling Mitigation** (Added)
- **Symptom**: CPU power throttling during gaming/high load reduces performance
- **Root Cause**: No automatic response to EDP throttling events
- **Fix**: Automatic undervolt adjustment (-50mV additional) when EDP throttling detected via MSR, restores when throttling ends
- **Reporters**: Performance testing feedback
- **Files**: `src/OmenCoreApp/Services/EdpThrottlingMitigationService.cs`, `src/OmenCoreApp/ViewModels/SystemControlViewModel.cs`

### ✅ **PawnIO Driver Embedding** (Enhanced)
- **Symptom**: PawnIO installation was optional, could be missed by users
- **Root Cause**: Driver required for MSR access but not always installed
- **Fix**: Made PawnIO installation required (always checked), clearer messaging about MSR access requirement
- **Reporters**: Users with missing MSR functionality
- **Files**: `installer/OmenCoreInstaller.iss`

### ✅ **Diagnostics: EC Register Readback & WMI History** (Enhanced)
- **Symptom**: Limited diagnostic data for complex fan control issues
- **Root Cause**: No capture of EC register states or WMI command history for analysis
- **Fix**: Added comprehensive EC register dumping (binary values) and WMI command history collection to diagnostic exports
- **Reporters**: Support requests for advanced troubleshooting
- **Files**: `src/OmenCoreApp/Services/Diagnostics/DiagnosticExportService.cs`, `src/OmenCoreApp/Hardware/WmiFanController.cs`

---

## ⚠️ Known Issues & FAQ

### **Fan Curves Exceeding 100%**
- **Cause**: Interpolation between curve points can theoretically exceed 100%
- **Status**: Safety bounds now clamp to 100% maximum - working as intended

### **EDP Throttling Still Occurring**
- **Cause**: MSR detection shows throttling status but doesn't prevent it
- **Status**: Detection implemented - mitigation features planned for future release
- **Workaround**: Monitor throttling status in diagnostics, adjust power plans if needed

### **"Limited Mode" Warning**
- **Answer**: Expected when OMEN Gaming Hub is not installed
- **Recommendation**: Keep OGH installed for best compatibility, or use PawnIO for full features

### **PawnIO Installation Issues**
- **Answer**: May require system restart after installation
- **Guide**: See installer prompts or [WINRING0_SETUP.md](WINRING0_SETUP.md) for manual installation

---

## 🔧 Technical Changes

### FanService.cs (Safety Bounds)
- Added `ApplySafetyBoundsClamping()` method with multi-layer thermal protection
- Emergency 100% at 88°C, progressive minimums (80% at 80°C, 60% at 70°C, etc.)
- Applied to both unified and independent curve application
- Clamping occurs after interpolation but before hysteresis/smoothing

### LibreHardwareMonitorImpl.cs (MSR Throttling)
- Added MSR fallback for CPU power throttling detection when sensors fail
- Enhanced throttling status in monitoring samples
- Added SafeFileHandle disposal handling and WMI BIOS fallback for temp freezing
- Improved exception handling around hardware.Update() calls
- Added system RAM total caching and WMI fallback when "Memory Available" sensor missing

### IMsrAccess.cs / PawnIOMsrAccess.cs / WinRing0MsrAccess.cs (MSR Access)
- Added `ReadThermalThrottlingStatus()`, `ReadPowerThrottlingStatus()` methods
- Implemented using MSR 0x19C (IA32_THERM_STATUS) register
- PawnIO implementation for Secure Boot compatibility
- WinRing0 fallback for legacy systems

### HardwareMonitoringService.cs (MSR Injection)
- Added `SetMsrAccess()` method for MSR access injection
- Called from SystemControlViewModel after MSR initialization
- Enables LibreHardwareMonitor to use enhanced throttling detection

### WmiFanController.cs (Verification & Reliability)
- Added `VerifyMaxAppliedWithRetries()` helper with 3 attempts and 500ms intervals
- Modified countdown extension to 15 seconds (OmenMon-style)
- Added continuous re-application instead of one-time extension
- Improved V2 command fallback logic for OMEN Max/17-ck models

### SystemControlViewModel.cs (MSR Initialization)
- Added MSR access injection to hardware monitoring service
- Initializes MSR access after hardware monitoring setup
- Enables enhanced throttling detection across the application

### DiagnosticExportControl.xaml(.cs) (Verification Export)
- Added optional "Fan Max Verification" checkbox
- UI wiring for verification test option
- Embeds verification results in diagnostic exports

### EdpThrottlingMitigationService.cs (EDP Mitigation)
- Monitors MSR power throttling status every 5 seconds
- Automatically applies -50mV additional undervolt when throttling detected
- Restores original undervolt settings when throttling ends
- Integrated with existing undervolt service for safe operation

### SystemControlViewModel.cs (EDP Integration)
- Initializes EDP mitigation service after MSR access setup
- Event handlers for throttling detection, mitigation application/removal
- Automatic service lifecycle management

### OmenCoreInstaller.iss (PawnIO Embedding)
- Removed optional flag from PawnIO installation task
- Updated description to emphasize MSR access requirement
- Ensures Secure Boot compatibility for all users

### WmiFanController.cs (Command History)
- Added `WmiCommandHistoryEntry` class for tracking WMI operations
- Added `AddCommandToHistory()` and `GetCommandHistory()` methods
- Command history includes timestamps, command types, and success status
- Used by diagnostic exports for troubleshooting fan control issues

### DiagnosticExportService.cs (Enhanced Collection)
- Added `CollectWmiCommandHistoryAsync()` for WMI operation history
- Enhanced `CollectEcStateAsync()` with binary register value dumping
- EC register readback captures full 256-byte EC memory state
- WMI history includes command sequences and timing for failure analysis

---

## 📦 Download

### Windows
- **OmenCoreSetup-2.5.1.exe** - Full installer with auto-update
  - SHA256: `FB7391404867CABCBAE14D70E4BD9D7B31C76D22792BB4D9C0D9D571DA91F83A`
- **OmenCore-2.5.1-win-x64.zip** - Portable version
  - SHA256: `05055ABAC5ABBC811AF899E0F0AFEE708FE9C28C4079015FAFE85AA4EFE1989F`

### Linux
- **OmenCore-2.5.1-linux-x64.zip** - GUI + CLI bundle
  - SHA256: `AD07B9610B6E49B383E5FA33E0855329256FFE333F4EB6F151B6F6A3F1EBD1BC`

---

## 🙏 Credits

**Bug Reports & Testing**:
- **Multiple Discord users** - Max preset verification issues and fan control reliability reports
- **@kastenbier2743** (Discord) - OMEN Max fan control issues
- **@xenon205** (GitHub #44) - OMEN 17-ck fan preset problems
- **Performance testing team** - EDP throttling detection feedback
- **Secure Boot users** - PawnIO compatibility reports

**Feature Requests**:
- **Safety review team** - Fan curve safety bounds requirements
- **Diagnostic users** - Enhanced export capabilities for support

---

## ⚠️ Known Linux Limitations

### 🐧 **OMEN MAX 16z-ak000 (AMD Ryzen AI 9 HX 375)** - Kernel Driver Required
- **Issue**: Fan presets and performance profiles have no effect
- **Root Cause**: Linux `hp-wmi` kernel driver doesn't support this 2025 OMEN MAX AMD model
- **User Workaround**: Manually patched hp-wmi driver to add board model for 100W CPU boost
- **Status**: **Linux kernel limitation**, not an OmenCore bug
- **Action**: Submit board ID to [Linux HP-WMI maintainers](https://patchwork.kernel.org/project/platform-driver-x86/list/)

**Note for Linux users on newer OMEN models:**
- OmenCore relies on `hp-wmi` kernel driver for fan/thermal control
- New models may not be supported until kernel patches are merged
- Check `dmesg | grep -i wmi` to see if your model is recognized
- Kernel 6.18+ recommended for best HP-WMI support

---

## 📖 Upgrade Notes

**All users**:
- Fan curves now have safety bounds - custom curves will be clamped to safe minimums at high temperatures
- Max preset now verifies actual fan speeds before showing 100% in UI
- Enhanced throttling detection provides more accurate CPU power status

**Windows users with Secure Boot**:
- PawnIO is now installed by default - provides better compatibility than WinRing0
- May require system restart after installation

**Users experiencing Max preset issues**:
- Previous issues should be resolved with verification and retry logic
- If problems persist, use diagnostic export with "Fan Max Verification" enabled

---

## 🚀 What's Next?

### v2.5.2 (Planned - Advanced Telemetry & OSD)
- **Advanced telemetry** - Track fan control effectiveness by model
- **OSD improvements** - Horizontal layout and preset configurations
- **GPU overclock GUI** - Visual controls for GPU frequency/voltage

---

