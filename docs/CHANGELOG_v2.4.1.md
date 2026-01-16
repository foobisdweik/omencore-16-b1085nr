# OmenCore v2.4.1 - Critical Bug Fix & Feature Release

**Release Date:** January 16, 2026  
**Status:** Beta

---

## 🔴 Critical Bug Fixes

### Fan Control Issues (Fixed)

1. **Balanced preset causing max fan speed** - ✅ FIXED
   - **Root Cause:** `FanController.ApplyPreset()` was using `Max(p => p.FanPercent)` which selected the highest value in the curve regardless of temperature
   - **Fix:** Implemented `EvaluateCurve()` method that uses linear interpolation to find the correct fan speed based on current CPU/GPU temperature
   - **Result:** Balanced preset now correctly evaluates the fan curve at the current temperature

2. **Max preset doesn't max fans** - ✅ FIXED
   - **Root Cause:** Max preset wasn't enabling HP OMEN's fan boost mode (EC register 0xEC)
   - **Fix:** Added `SetMaxSpeed()` method that:
     - Sets fan duty to 100% on registers 0x2E/0x2F
     - Sets RPM units to max (55 = 5500 RPM) on 0x34/0x35
     - Enables manual mode via OMCC register 0x62
     - Enables fan boost via register 0xEC = 0x0C
   - **Result:** Max preset now truly maximizes fans with boost enabled

3. **RPM showing incorrect values (100% but fans off)** - ✅ FIXED
   - **Root Cause:** `ReadFanSpeeds()` was returning estimated RPM based on last set percentage, not actual fan speed
   - **Fix:** Implemented `ReadActualFanRpm()` that reads EC registers 0x34/0x35 which contain actual fan speed in 100 RPM units
   - **Result:** UI now shows actual fan RPM from EC registers, with fallback to estimation only if EC read fails

4. **Nothing works / fans fail to ramp** - ✅ FIXED
   - **Root Cause:** `WriteDuty()` was writing to incorrect/inadequate EC registers and missing critical control registers
   - **Fix:** Complete rewrite of `WriteDuty()` to properly control HP OMEN fans:
     - Writes fan percentage to 0x2E/0x2F (0-100 range)
     - Writes RPM units to 0x34/0x35 (value × 100 = RPM)
     - Sets OMCC register 0x62 = 0x06 for manual control
     - Manages fan boost register 0xEC for high-speed mode
   - **Result:** Fan control commands now properly execute on HP OMEN hardware

5. **WinRing0 intermittent detection** - ✅ FIXED
   - **Root Cause:** `WinRing0EcAccess` was using incorrect IOCTL codes (placeholder values)
   - **Fix:** Complete rewrite to use proper ACPI EC protocol:
     - Uses `IOCTL_OLS_READ_IO_PORT_BYTE` (0x9C402480) and `IOCTL_OLS_WRITE_IO_PORT_BYTE` (0x9C402488)
     - Implements proper ACPI EC protocol with ports 0x62/0x66
     - Handles EC status flags (IBF/OBF) correctly
     - Adds global EC mutex for thread safety
   - **Result:** WinRing0 backend now works reliably on systems without PawnIO

---

## 🆕 New Features

### MSI Afterburner Detection & GPU Telemetry
- **ConflictDetectionService** - Detects running applications that may conflict with OmenCore:
  - MSI Afterburner, RTSS, XTU, ThrottleStop, HWiNFO, FanControl, OMEN Gaming Hub
  - Reports conflict severity (Low/Medium/High) with mitigation suggestions
- **Afterburner Shared Memory Integration** - When MSI Afterburner is running:
  - Reads GPU temperature, fan RPM, power, and clock speeds from shared memory
  - Can be used as secondary GPU telemetry source

### Diagnostic Logging Mode
- **DiagnosticLoggingService** - Opt-in EC register capture for troubleshooting:
  - Captures raw EC register values at configurable intervals
  - Records fan control registers (0x2C-0x2F, 0x34-0x35, 0x62, 0xEC, etc.)
  - Records thermal registers for correlation
  - Detects conflicting processes
  - Exports detailed diagnostic reports for bug submissions

### RPM Source Indicator
- **RpmSource enum** - Shows where RPM data comes from:
  - `EC` - Direct EC register read (most accurate)
  - `HWMon` - LibreHardwareMonitor SuperIO
  - `MAB` - MSI Afterburner shared memory
  - `WMI` - WMI BIOS query
  - `Est` - Estimated from duty cycle (least accurate)
- **UI Display** - Fan Diagnostics view now shows RPM source badge

### EC Access Updates
- Added critical EC registers to allowlist:
  - 0x62 (OMCC - BIOS manual/auto control)
  - 0xEC (Fan boost)
  - 0x63 (Timer register)
  - 0xF4 (Fan state)
- Both WinRing0EcAccess and PawnIOEcAccess updated

---

## 🟡 Known Issues

### GPU Power Control
- **GPU capped at 60W** - Model-specific issue
  - Some OMEN models (17-ck2xxx series) don't respond to WMI GPU power commands
  - HP WMI BIOS interface exists but GPU power commands return empty results
  - This is a BIOS limitation, not an OmenCore bug
  - **Workaround:** Use HP Omen Hub if GPU power control is needed on affected models

---

## 📝 Technical Details

### HP OMEN EC Register Map (Reference)

| Register | Name | Range | Description |
|----------|------|-------|-------------|
| 0x2E | FAN1_PCT | 0-100 | Fan 1 speed percentage (legacy) |
| 0x2F | FAN2_PCT | 0-100 | Fan 2 speed percentage (legacy) |
| 0x34 | FAN1_RPM | 0-55 | Fan 1 speed in 100 RPM units |
| 0x35 | FAN2_RPM | 0-55 | Fan 2 speed in 100 RPM units |
| 0x62 | OMCC | 0x00/0x06 | BIOS control (0=Auto, 6=Manual) |
| 0xEC | FAN_BOOST | 0x00/0x0C | Fan boost (0=OFF, 12=ON) |
| 0xCE | PERF_MODE | varies | Performance mode register |

### WinRing0 IOCTL Codes (Corrected)
- `IOCTL_OLS_READ_IO_PORT_BYTE` = 0x9C402480
- `IOCTL_OLS_WRITE_IO_PORT_BYTE` = 0x9C402488
- Uses ACPI EC protocol on ports 0x62 (data) and 0x66 (command)

### Changed Files

**Hardware:**
- `WinRing0EcAccess.cs` - Complete rewrite with proper ACPI EC protocol
- `PawnIOEcAccess.cs` - Added 0x62, 0x63, 0xEC, 0xF4 to AllowedWriteAddresses
- `FanController.cs` - Major rewrite with curve evaluation, EC RPM reading
- `FanControllerFactory.cs` - Updated wrapper to use new methods

**Services:**
- `DiagnosticLoggingService.cs` - NEW: EC register capture
- `ConflictDetectionService.cs` - NEW: MSI Afterburner/conflict detection
- `FanVerificationService.cs` - Added GetCurrentFanStateWithSource()
- `IFanVerificationService.cs` - Added RPM source interface

**Models:**
- `FanTelemetry.cs` - Added RpmSource enum and RpmSourceDisplay property

**Views:**
- `FanDiagnosticsView.xaml` - Added RPM source badge display

**ViewModels:**
- `FanDiagnosticsViewModel.cs` - Added RpmSourceDisplay property

---

## ✅ Testing

All 66 unit tests pass:
- Fan control tests ✅
- WinRing0 EC access tests ✅  
- Fan diagnostics tests ✅
- Configuration tests ✅

---

## 🙏 Credits

Bug reports and testing assistance from Discord community:
- replaY! (OMEN 16-b0xxx)
- SimplyCarrying (OMEN MAX 16t-ah000)
- mentos (OMEN Transcend 16-u0xxx)
- kastenbier2743
- And all other community testers!

---

## 📦 Installation

1. Download `OmenCoreSetup-2.4.1.exe` from GitHub Releases
2. Run installer as Administrator
3. If upgrading from v2.4.0, no configuration changes needed
4. Verify fan control works on Fan Diagnostics page

**SHA256 Checksums:**
```
[To be generated after build]
```
