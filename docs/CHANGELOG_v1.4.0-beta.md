# OmenCore v1.4.0-beta Changelog

**Release Date:** December 16, 2025  
**Status:** Beta  
**Focus:** Bug fixes, UI improvements, and new features based on v1.3.0-beta2 community feedback

---

## ✨ New Features

### 🎨 Interactive 4-Zone Keyboard Controls
- **Visual Zone Editor**: Click on any of the 4 keyboard zones to select and edit colors individually
- **Hex Color Input**: Enter precise colors using hex codes (#FF0000, #00FF00, etc.)
- **Quick Presets**: Dropdown with popular color presets including:
  - OMEN Red (#C40000)
  - Dragon Purple (#8B00FF)  
  - Cyber Blue (#00D4FF)
  - Gaming Green (#00FF41)
  - Sunset Orange (#FF6B00)
  - Hot Pink (#FF1493)
  - Ice White (#FFFFFF)
  - Stealth Black (#1A1A1A)
- **Apply Buttons**: "Apply to Keyboard" sends colors to hardware, "All Same Color" applies Zone 1 color to all zones
- **Visual Feedback**: Zone boxes show current colors with selection highlighting

### 🚀 Startup Reliability Improvements
- **StartupSequencer Service**: New centralized startup manager ensures boot-time reliability
  - Priority-ordered task execution
  - Configurable retry logic with exponential backoff
  - Progress tracking for startup operations
  - Handles Windows race conditions gracefully

### 🖼️ Splash Screen
- **Branded Loading Experience**: New OMEN diamond logo splash screen during startup
- **Progress Bar**: Visual progress indicator during initialization
- **Status Messages**: Shows current startup operation
- **Smooth Animations**: Fade in/out transitions

### 🔔 Enhanced Notification System  
- **In-App Notification Center**: New `AddInfo()`, `AddSuccess()`, `AddWarning()`, `AddError()` methods
- **Notification Types**: Support for Info, Success, Warning, Error with appropriate icons
- **Unread Count**: Track and display unread notification count
- **Read/Unread State**: Mark notifications as read
- **Timestamp Tracking**: All notifications include creation time

### Fan Profile UI Redesign
- **Unified preset selector**: Replaced confusing "Quick Presets" buttons + "Choose Preset" dropdown with a single card-based interface
- **Visual preset cards**: Max, Gaming, Auto, Silent, and Custom modes now shown as clickable cards with icons
- **Active mode indicator**: Current fan mode clearly displayed
- **Streamlined layout**: Cleaner, more intuitive fan control experience

### Undervolt Status Improvements
- **Informative error messages**: When undervolting is not available, shows detailed explanation of why (Intel Plundervolt, AMD Curve Optimizer)
- **CPU-specific guidance**: Different explanations for Intel vs AMD processors
- **Alternative suggestions**: Points users to BIOS settings or manufacturer tools when OmenCore can't help

### OSD Overlay Positions
- **New positions**: Added TopCenter and BottomCenter options for OSD overlay placement
- **6 total positions**: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight

### Documentation
- **Antivirus FAQ**: New comprehensive guide explaining why AV software may flag OmenCore and how to whitelist it
- **Whitelist instructions**: Step-by-step guides for Windows Defender, Avast, Bitdefender, Kaspersky, Norton, ESET

---

## 🐛 Bug Fixes

### Critical Fixes

#### TCC Offset (CPU Temperature Limit) Now Persists Across Reboots
- **Issue:** CPU temperature limit reset to 100°C after PC restart
- **Fix:** TCC offset is now saved to config when applied and automatically restored on startup
- **Technical:** Added `LastTccOffset` config property with startup restore logic and verification

#### GPU Power Boost Restoration Improved
- **Issue:** GPU TGP/Dynamic Boost sometimes reset to Minimum after reboot
- **Fix:** Existing restore logic verified; startup delay ensures WMI BIOS is ready

#### Thermal Protection Made More Aggressive
- **Issue:** Fans were too gentle - CPU reached 85-90°C before ramping
- **Fix:** Lowered thermal protection thresholds:
  - **Warning threshold:** 90°C → **80°C** (fans start ramping at 70%)
  - **Emergency threshold:** 95°C → **88°C** (100% fans immediately)
  - **Release threshold:** 85°C → **75°C** (5°C hysteresis)
- **Technical:** Fan ramp formula now: 70% + 3.75% per °C above 80°C

#### Auto-Start Detection Fixed
- **Issue:** "Start with Windows" toggle didn't correctly detect existing startup entries
- **Fix:** Now checks both Task Scheduler AND registry for startup entries
- **Technical:** Added `CheckStartupTaskExists()` and `CheckStartupRegistryExists()` helper methods

### UI/UX Fixes

#### SSD Sensor 0°C Display Fix
- **Issue**: Storage card displayed 0°C when no SSD temperature sensor was available
- **Fix**: Storage widget now automatically hides when `SsdTemperatureC <= 0`
- **Technical**: Added `IsSsdDataAvailable` property to `MonitoringSample` model

#### Overlay Hotkey Registration on Minimized Start
- **Issue**: Overlay hotkey (Ctrl+Shift+O) failed to register when app started minimized to tray
- **Fix**: Implemented retry mechanism with 5 attempts at 2-second intervals
- **Technical**: Added `StartHotkeyRetryTimer()` and `RegisterHotkeyWithHandle()` to `OsdService`

#### Tray Menu Refresh Rate Display Now Updates
- **Issue:** After changing refresh rate, tray popup still showed old value
- **Fix:** Tray menu item header now updates immediately after changing refresh rate

#### Undervolt Section Hides When Not Supported
- **Issue:** Undervolt controls visible on AMD Ryzen systems that don't support it
- **Fix:** CPU Undervolting section in Advanced view now hides when `IsUndervoltSupported` is false
- **Technical:** Added `IsUndervoltSupported` property with visibility binding

#### Lighting ViewModel Improvements
- **Improvement:** Added `HasCorsairDevices` and `HasLogitechDevices` properties
- **Purpose:** Allows UI to conditionally show/hide peripheral sections

#### Version String Display Fixed
- **Issue:** Log showed "v1.3.0" even when running v1.4.0-beta2
- **Fix:** Updated AssemblyVersion and FileVersion in csproj to 1.4.0
- **Technical:** Assembly attributes now match VERSION.txt

#### OGH Detection False Positive Fixed
- **Issue:** OmenCore detected OMEN Gaming Hub as installed even after uninstall/cleanup
- **Fix:** Now uses ServiceController to check if services are actually *running*, not just registered
- **Technical:** Changed from WMI Win32_Service query to ServiceController for more accurate detection

#### Intel XTU Detection False Positive Fixed
- **Issue:** "Intel XTU active" warning appeared when XTU wasn't installed
- **Fix:** XTU detection now checks Windows services (ServiceController), not process names
- **Technical:** Service names like "XTU3SERVICE" are services, not processes - fixed detection method

#### Auto-Update Version Parsing Fixed
- **Issue:** Auto-update said "No update available" when checking v1.3.0-beta2 → v1.4.0-beta2
- **Fix:** Added proper semantic versioning support with prerelease tag comparison
- **Technical:** `Version.TryParse` doesn't handle "-beta2" suffixes; added custom prerelease parser
- **Logic:** 1.4.0-beta2 > 1.3.0-beta2, beta2 > beta1, stable > prerelease of same version

#### Keyboard RGB Dual-Write for Compatibility
- **Issue:** WMI BIOS keyboard commands reported success but didn't affect hardware on some models
- **Fix:** Added dual-write mode - applies colors via both WMI and EC for better compatibility
- **Technical:** Some OMEN 17-ck2xxx models have WMI that returns success but doesn't control keyboard

---

## 🔧 Technical Changes

### AppConfig Changes
```csharp
// New property for TCC offset persistence
public int? LastTccOffset { get; set; }

// OSD position now supports 6 options
// TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight
```

### FanService Thermal Protection
```csharp
// Old thresholds
private const double ThermalProtectionThreshold = 90.0;
private const double ThermalEmergencyThreshold = 95.0;

// New thresholds (more aggressive)
private const double ThermalProtectionThreshold = 80.0;
private const double ThermalEmergencyThreshold = 88.0;
```

### New Files Created
- `src/OmenCoreApp/Services/StartupSequencer.cs` - Centralized startup manager
- `src/OmenCoreApp/Views/SplashWindow.xaml` - Splash screen UI
- `src/OmenCoreApp/Views/SplashWindow.xaml.cs` - Splash screen code-behind

---

## 📦 Installation

### Fresh Install
1. Download `OmenCoreSetup-1.4.0-beta2.exe`
2. Run installer (may require admin rights)
3. Launch OmenCore from Start Menu or desktop shortcut

### Upgrade from v1.3.x
1. Close OmenCore completely (exit from system tray)
2. Run the new installer - it will upgrade in place
3. Your settings and custom fan curves are preserved

---

## 🧪 Testing Notes

### What to Test
- [ ] Set TCC offset, reboot PC, verify it's restored
- [ ] Set GPU Power Boost to Maximum, reboot, verify it's restored
- [ ] Enable "Start with Windows", reboot, verify OmenCore starts
- [ ] Run CPU stress test, verify fans ramp at 80°C, not 90°C
- [ ] Change refresh rate from tray, verify tray menu shows new value
- [ ] Change OSD position to TopCenter/BottomCenter, verify positioning
- [ ] On AMD system, verify undervolt section is hidden
- [ ] Test 4-zone keyboard color controls
- [ ] Verify SSD widget hides when no temperature sensor
- [ ] Test auto-update detection from v1.3.0-beta2 → v1.4.0-beta2
- [ ] Verify OGH/XTU not falsely detected after uninstall

---

## 📝 Community Feedback Addressed

Based on reports from:
- Omen 17-ck2xxx users (thermal issues)
- Omen 15-en0027ur user (AMD Ryzen, UI feedback)
- Omen Max 16 users (TCC/undervolt issues)
- Multiple users (refresh rate display, auto-start)

Thank you for the detailed bug reports and logs! 🙏

---

## 📊 Build Information

- **Build Configuration:** Release
- **Target Framework:** .NET 8.0
- **Platform:** Windows x64
- **Self-Contained:** Yes

---

## 📥 Download

**Installer:** `OmenCoreSetup-1.4.0-beta2.exe`  
**SHA256:** `0622B0EB68F27386560D0702BE6D656F0CA3940FA2F5D07980EA91D0BB608A0D`

---

## 🔗 Links

- **GitHub:** https://github.com/theantipopau/omencore
- **Issues:** https://github.com/theantipopau/omencore/issues
- **v1.4 Roadmap:** [ROADMAP_v1.4.md](ROADMAP_v1.4.md)
