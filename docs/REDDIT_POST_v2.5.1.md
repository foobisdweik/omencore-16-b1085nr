# [RELEASE] OmenCore v2.5.1 - Fan "Max" Reliability & Diagnostics

**Date:** January 24, 2026  
**Download:** https://github.com/theantipopau/omencore/releases/tag/v2.5.1

---

## 🎯 What's New in v2.5.1

This release focuses on making the "Max" fan preset robust and reliable, with enhanced diagnostics and critical safety improvements for fan curve protection.

### 🌀 FAN CONTROL IMPROVEMENTS

#### ✅ **Max Preset Reliability**
- **Real-time verification**: UI only shows 100% after confirming fans are actually at maximum speed
- **Retry logic**: Automatic retry loops with alternative command sequences for different BIOS versions
- **Accurate state display**: Eliminates misleading 100% indicators when fans aren't responding

#### ✅ **Fan Reset on Exit**
- **BIOS auto-control restoration**: Fans properly return to Windows/BIOS defaults when app closes
- **Prevents stuck fans**: No more fans staying at manual speeds after application exit
- **Clean shutdown**: Explicit fan auto-control restoration during app shutdown sequence

#### ✅ **Enhanced Diagnostics**
- **Fan Max verification**: Diagnostic exports can include fan max verification results
- **Per-fan EC control**: Individual fan register control for better troubleshooting
- **Improved logging**: Enhanced debug information for hardware/driver failure analysis

### 🛡️ SAFETY & RELIABILITY

#### ✅ **Fan Curve Safety Bounds**
- **Thermal emergency protection**: Forces 100% fans at 88°C regardless of curve settings
- **Progressive minimum speeds**: Enforced safe minimums (80% at 80°C, 60% at 70°C, etc.)
- **Hardware damage prevention**: Protects against overheating from overly aggressive custom curves

#### ✅ **EDP Throttling Detection**
- **MSR-based detection**: Uses IA32_THERM_STATUS register for accurate CPU throttling detection
- **Secure Boot compatible**: Works with PawnIO for MSR access on locked-down systems
- **Automatic mitigation**: Can automatically adjust undervolts when EDP throttling is detected

### 🐛 CRITICAL BUG FIXES

- **Max preset UI accuracy**: Fixed showing 100% when fans weren't actually at maximum speed
- **WMI/EC command reliability**: Improved with verification loops and BIOS-specific fallbacks
- **Temperature freezing**: Fixed temperature freeze during storage drive sleep states
- **RAM display issues**: Fixed monitoring tab showing "0.0 / 0 GB" instead of actual values
- **CPU temperature display**: Fixed 0°C display with PawnIO MSR fallback implementation
- **Hardware monitoring**: Enhanced LibreHardwareMonitor integration with proper fallbacks

---

## 📦 Download Options

**Windows:**
- **Installer**: `OmenCoreSetup-2.5.1.exe` - Full installation with auto-update
- **Portable**: `OmenCore-2.5.1-win-x64.zip` - No installation required

**Linux:**
- **x86_64**: `OmenCore-2.5.1-linux-x64.zip` - GUI + CLI bundle

---

## 🔒 Verify Your Downloads

SHA256 checksums for security verification:

```
FB7391404867CABCBAE14D70E4BD9D7B31C76D22792BB4D9C0D9D571DA91F83A  OmenCoreSetup-2.5.1.exe
05055ABAC5ABBC811AF899E0F0AFEE708FE9C28C4079015FAFE85AA4EFE1989F  OmenCore-2.5.1-win-x64.zip
AD07B9610B6E49B383E5FA33E0855329256FFE333F4EB6F151B6F6A3F1EBD1BC  OmenCore-2.5.1-linux-x64.zip
```

**Verification commands:**
- Windows PowerShell: `Get-FileHash -Algorithm SHA256 filename`
- Linux: `sha256sum filename`

---

## ⏭️ What's Next (v2.6.0)

- 📊 **Advanced monitoring dashboard** with real-time charts and historical data
- 🎨 **Enhanced RGB lighting** with temperature-responsive effects and multi-device sync
- 🔍 **MSI Afterburner integration** improvements and conflict detection
- 🧪 **Expanded unit testing** for critical fan control and hardware monitoring logic</content>
<parameter name="filePath">f:\Omen\docs\REDDIT_POST_v2.5.1.md