# [RELEASE] OmenCore v2.4.0 - Major Stability & Safety Release

**Date:** January 15, 2026  
**Download:** https://github.com/theantipopau/omencore/releases/tag/v2.4.0

---

## 🎯 What's New in v2.4.0

This release focuses on **critical bug fixes** and **UI/UX improvements** after extensive community feedback.

### ⚠️ CRITICAL FIXES

#### 🚨 **Fan Runaway Acceleration (GitHub #49) - SAFETY CRITICAL**

One user reported their fans accelerating beyond intended speed with sounds like a "short circuit" - would have burned out if the laptop wasn't powered down immediately.

**Root Cause:** Missing safety cap on fan speed calculations allowed values exceeding 100%.

**What We Fixed:**
- Added `Math.Clamp(0, 100)` to fan interpolation calculations
- Applied clamping at 3 layers:
  1. **Calculation Layer** (NEW) - FanCurveService & FanCurveEngine clamp speed
  2. **Validation Layer** (existing) - ViewModels prevent invalid input
  3. **Hardware Layer** (existing) - WMI controller final cap
- Result: **Fans cannot exceed safe maximum regardless of input**

#### 🔴 **UI Freeze During Gaming - CRITICAL**

Some users experienced complete UI freezes after 20-30 minutes of gaming, requiring task kill to recover.

**Root Causes Identified:**
- No timeout on WMI/CIM operations - could hang indefinitely
- Dispatcher backlog from accumulated `BeginInvoke()` calls
- Potential MSI Afterburner conflicts (auto-detection planned v2.5.0)

**What We Fixed:**
- Added 5-second timeout to ALL WMI BIOS operations
- Implemented throttle flag - only one UI update queued at a time
- Result: **UI stays responsive during extended gaming**

#### 📋 **EC Address Blocking on Older Models**

Omen 15-dc0xxx (2018 models) users got "EC write to address 0x2C is blocked for safety" errors.

**What We Fixed:**
- Confirmed 0x2C is in the EC allowlist (was added in v2.1.0)
- Added fallback logic for legacy registers (0x2E/0x2F)
- Result: **Fan control works on more models**

---

### 🐛 Other Bug Fixes

- **Quiet Mode Thermal Tuning (GitHub #47)** - Increased fan curve aggressiveness at warm temperatures; one user reported 75°C temps while watching movies in Quiet mode - now caps at ~70°C
- **Linux RAM Display** - Now shows "8.2 / 16.0 GB" instead of just percentage
- **Linux Version Display** - Updated to show v2.4.0 correctly
- **CS8602 Warnings** - Fixed nullable reference issues for strict CI builds

---

### ✨ UI/UX Improvements (GitHub #48)

**Settings Reorganization:**
- 5 logical tabs instead of one massive scrolling page:
  - **Status** - System info, backend status, PawnIO, telemetry
  - **General** - Start with Windows, minimize behavior, auto-update
  - **Advanced** - Monitoring intervals, hotkeys, EC reset, battery care
  - **Appearance** - OSD, notifications, UI preferences
  - **About** - Version info, links, GitHub

**New Diagnostics Tab:**
- Moved Fan & Keyboard diagnostics to dedicated tab
- Side-by-side layout for better space utilization
- Advanced tab now focuses on performance tuning

**Collapsible Logs:**
- Hide/show Recent Activity and System Log panels
- Hides by default to reduce screen clutter

**Update Notifications:**
- Hide "You're on latest version" banner (cleaner UI)

---

## 📦 Download Options

**Windows:**
- **Installer** (100.51 MB): `OmenCoreSetup-2.4.0.exe` - Full installation
- **Portable** (103.78 MB): `OmenCore-2.4.0-win-x64.zip` - No installation needed

**Linux:**
- **x86_64** (66.24 MB): `OmenCore-2.4.0-linux-x64.zip`
- **ARM64** (35.80 MB): `OmenCore-2.4.0-linux-arm64.zip` - Raspberry Pi 4/5 compatible

---

## 🔒 Verify Your Downloads

SHA256 checksums for integrity verification:

```
91DAF951A8E357B90359E7C1557DC13EF3472F370F0CB30073C541244FCAE32C  OmenCoreSetup-2.4.0.exe
18CEB337EB9FA99604F96A352E48744601703046DEA54528BDDFD666E35F0DE1  OmenCore-2.4.0-win-x64.zip
6C13F67F377D7140ECE28DABAC77C9C0267636BE732E87512AED466D7B0DE437  OmenCore-2.4.0-linux-x64.zip
60BF36CCECC576642830DC8E85AD747A9D534E491984A5445E3BDB9A2AFE5408  OmenCore-2.4.0-linux-arm64.zip
```

**How to verify:**
- **Windows:** `certUtil -hashfile OmenCoreSetup-2.4.0.exe SHA256`
- **Linux/Mac:** `sha256sum OmenCore-2.4.0-linux-x64.zip`

---

## 📋 Known Issues

- **Omen 15-dc0xxx (2018):** EC register 0x2C may not work on some 2018 models - use WMI BIOS control instead
- **MSI Afterburner:** Running Afterburner + OmenCore may cause UI freezes - close Afterburner first (auto-detection coming v2.5.0)

---

## 🔄 Upgrade Notes

- **From v2.3.x:** No config migration needed - update recommended for UI freeze and WMI timeout fixes
- **From v2.2.x or earlier:** Update strongly recommended

---

## 🙏 Credits

**Bug Reports:**
- u/Prince-of-Nothing - Fan runaway (GitHub #49)
- kg290 - Thermal & UI (GitHub #47, #48)
- its-urbi - Collapsible logs (GitHub #48)
- dfshsu - Linux RAM display and version display
- Reddit user - UI freeze bug

**Development:** theantipopau + GitHub Copilot

---

## ⏭️ What's Next (v2.5.0)

- 🔍 MSI Afterburner auto-detection and conflict warnings
- 🧪 Unit tests for fan control logic
- 📊 Improved CI/CD pipeline with strict warnings

---

**Questions or issues?**
- **GitHub Issues:** https://github.com/theantipopau/omencore/issues
- **Documentation:** https://github.com/theantipopau/omencore#readme

Thanks to everyone who reported bugs and tested - your feedback makes OmenCore better! 🎉
