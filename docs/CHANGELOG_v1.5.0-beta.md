# Changelog v1.5.0-beta

**Release Date:** In Development  
**Status:** Beta  
**Previous Version:** v1.4.0

---

## Overview

Version 1.5.0-beta focuses on reliability improvements, bug fixes, and architectural cleanup based on user feedback from v1.4.0. Key themes:

1. **Settings Persistence** - Fix TCC/GPU settings not surviving reboot
2. **Verification Layer** - Closed-loop fan control with RPM readback
3. **WinRing0 Removal** - Eliminate problematic driver dependency
4. **Power Detection** - More reliable AC/battery state detection
5. **UI Polish** - Fix fan curve editor visual glitches
6. **OmenCap Detection** - Detect HP DriverStore component blocking MSR
7. **OSD Overlay** - In-game performance overlay with customizable metrics
8. **OMEN Key** - Intercept OMEN key for custom actions
9. **RGB Persistence** - Keyboard colors survive app/PC restarts

---

## Latest Fixes (2025-12-17)

### Critical: Window Focus-Stealing Bug Fixed
**Files Changed:**
- `Services/OmenKeyService.cs`

**Problem:** OmenCore repeatedly came to foreground after being minimized, particularly when trying to open Remote Desktop or other applications. Window would reappear 6-7 times before other app could be used.

**Root Cause:** The WMI event watcher for OMEN key detection was using overly broad queries like `SELECT * FROM hpqBEvnt` which caught ALL HP BIOS events (fan changes, thermal events, power state changes), not just the OMEN key press (eventId=29, eventData=8613). Each event was triggering the default action which brought the window to front.

**Solution:**
- Restricted WMI event query to ONLY the specific OMEN key event: `SELECT * FROM hpqBEvnt WHERE eventData = 8613 AND eventId = 29`
- Removed broad fallback queries that were catching unrelated BIOS events
- If specific query fails, rely on keyboard hook only (safer than catching all events)
- Added debounce logging to help diagnose any future key detection issues

---

### Fan Safety: Thermal Protection Now Uses RestoreAutoControl
**Files Changed:**
- `Services/FanService.cs`

**Problem:** When thermal protection released after temps normalized, the code called `SetFanSpeed(0)` assuming "0 = auto mode". On some HP firmware, `SetFanLevel(0,0)` means "minimum/stop fans" rather than "return to BIOS auto control".

**Solution:**
- Replaced `SetFanSpeed(0)` with `RestoreAutoControl()` which is the proper API for returning control to BIOS
- Added warning comments explaining why `SetFanSpeed(0)` should never be used for auto mode
- Ensures fans properly return to automatic control after emergency thermal protection events

---

### Max Cooling No Longer Forces GPU Power to Maximum
**Files Changed:**
- `Hardware/WmiFanController.cs`

**Problem:** The "Max" fan preset was also setting GPU power to Maximum, which generates MORE heat - counterproductive when users want maximum cooling.

**Solution:**
- Removed `SetGpuPower(Maximum)` call from Max fan preset
- Max preset now ONLY controls fan speed (100%)
- Users who want both max cooling AND max GPU power can set them independently
- Added comments explaining the design decision: "max fans is for COOLING, not more power"

---

### Performance Mode EC Availability Indicator
**Files Changed:**
- `Services/PerformanceModeService.cs`
- `ViewModels/SystemControlViewModel.cs`

**Problem:** Users didn't know if performance mode changes were actually applying CPU/GPU power limits or just changing Windows power plan.

**Solution:**
- Added `EcPowerControlAvailable` property to PerformanceModeService
- Added `ControlCapabilityDescription` property showing what controls are available
- Added `PerformanceModeCapabilityStatus` to SystemControlViewModel for UI display
- When EC unavailable: "ℹ️ Partial control (Power Plan + Fan only - EC unavailable)"
- When EC available: "✓ Full control (Power Plan + Fan + CPU/GPU Limits)"

---

### Reduced Fan Telemetry UI Churn
**Files Changed:**
- `Services/FanService.cs`

**Problem:** Fan telemetry was cleared and re-populated every monitoring loop iteration, causing unnecessary GC pressure and UI update churn even when values hadn't changed.

**Solution:**
- Added `_lastFanSpeeds` tracking list
- Added `FanSpeedChangeThreshold` (50 RPM) for meaningful change detection
- Fan telemetry only updates when RPM values change by more than threshold
- Reduces collection clear/add cycles when fans are stable

---

### HP Spectre Dynamic Branding
**Files Changed:**
- `Models/SystemInfo.cs`
- `Services/SystemInfoService.cs`
- `ViewModels/GeneralViewModel.cs`
- `Views/GeneralView.xaml`

**Problem:** OmenCore showed OMEN branding on HP Spectre laptops.

**Solution:**
- Added `IsHpSpectre` detection to SystemInfo model
- GeneralView header now shows spectre.png on Spectre systems, omen.png otherwise
- Detection is model-name based (safe fallback to OMEN logo if detection fails)
- No visual flicker on startup - logo is determined once at ViewModel creation

---

### UI Cleanup - Removed Redundant Elements
**Files Changed:**
- `Views/GeneralView.xaml`
- `Views/DashboardView.xaml`

**Problem:** Several UI elements were duplicated across views, causing confusion:
1. Performance Mode / Fan Mode status box in General tab duplicated sidebar info
2. "Reduce CPU Usage" button in Dashboard header duplicated Settings option

**Solution:**
- Removed "Current Status Display" section from General tab (Performance Mode / Fan Mode box)
- Removed "Reduce CPU Usage" toggle button from Dashboard header
- Both are now only in their proper locations (sidebar for status, Settings for monitoring options)
- Simplified grid layouts after removing unused columns/rows

---

### RGB Keyboard Zone Fixes
**Files Changed:**
- `ViewModels/LightingViewModel.cs`
- `Views/LightingView.xaml`
- `Styles/ModernStyles.xaml`

**Problem:** Multiple issues with RGB keyboard zone controls:
1. Zone color mapping was inverted - Zone 1 "Left" was controlling the right side of the keyboard
2. Color picker dialog wouldn't open for Zones 2, 3, and 4 (only Zone 1 worked)
3. Color picker crashed with "Cannot find resource named 'PrimaryButton'" error

**Root Cause:**
- HP WMI BIOS uses reversed zone ordering: Z0=Right, Z1=Middle-R, Z2=Middle-L, Z3=WASD
- UI zones 1-4 were being sent directly to WMI zones 0-3 without remapping
- InputBindings (MouseBinding commands) were missing from Zone 2-4 Border elements
- PrimaryButton style was not defined in ModernStyles.xaml

**Solution:**
- Reordered color array in `ApplyKeyboardColorsAsync()` to map UI zones to physical keyboard areas:
  ```csharp
  // UI [Z1,Z2,Z3,Z4] -> WMI [Z3,Z2,Z1,Z0]
  var colors = new[] { Zone4, Zone3, Zone2, Zone1 };  // Reversed for WMI
  ```
- Added missing InputBindings to Zone 2, 3, and 4 Border elements in LightingView.xaml
- Added PrimaryButton style to ModernStyles.xaml for the color picker dialog
- Updated zone labels to match physical keyboard layout:
  - Zone 1: "WASD" (W/A/S/D key area)
  - Zone 2: "Left" (left side of keyboard)
  - Zone 3: "Right" (right side of keyboard)
  - Zone 4: "Far Right" (arrows, numpad area)

---

### Performance Mode Sync & UI
**Files Changed:**
- `ViewModels/SystemControlViewModel.cs`
- `Views/AdvancedView.xaml`
- `Styles/ModernStyles.xaml`

**Problem:** Performance mode wasn't being applied on startup - only the selection was restored but the actual WMI call wasn't made. Additionally, there was no easy way to switch performance modes from the Advanced tab.

**Solution:**
- Added `ReapplySavedPerformanceMode()` method that actually calls `_performanceModeService.Apply()` on startup
- Uses existing retry mechanism with exponential backoff (3 retries, 2000ms initial delay)
- Added new **Performance Mode** section at top of Advanced view with large clickable buttons:
  - 🌙 **Quiet** - Silent operation, reduced fan noise
  - ⚖️ **Balanced** - Optimal balance for everyday use  
  - 🚀 **Performance** - Maximum power for gaming/workloads
- Added `IsQuietMode`, `IsBalancedMode`, `IsPerformanceMode` computed properties for UI binding
- Added `SelectPerformanceModeCommand` that selects AND applies mode immediately
- New `PerformanceModeRadioStyle` with gradient highlighting when selected

**UI Features:**
- Mode buttons use OneWay binding (read-only computed properties)
- Active mode shows cyan accent border with subtle gradient background
- Current mode displayed in status bar below buttons
- Clicking any button immediately applies the mode via WMI BIOS

---

### RGB Keyboard Color Persistence
**Files Changed:** 
- `ViewModels/LightingViewModel.cs`
- `ViewModels/MainViewModel.cs`
- `Models/AppConfig.cs`

**Problem:** Keyboard RGB colors reset to defaults after restarting OmenCore or the PC.

**Solution:**
- Added `KeyboardLightingSettings` class to AppConfig with Zone1-4Color properties
- Colors automatically saved to config when applying
- Colors automatically restored on app startup (if `ApplyOnStartup` is true)
- New log messages: "Loaded keyboard colors from config" and "Restored keyboard colors on startup"

---

### OSD Overlay Improvements
**Files Changed:**
- `Views/OsdOverlayWindow.xaml`
- `Views/OsdOverlayWindow.xaml.cs`
- `Services/OsdService.cs`
- `Services/FanService.cs`
- `Models/AppConfig.cs`

**Problem:** OSD showed 0% and 0°C for all values, settings changes didn't update live, too opaque.

**Solution:**
- Fixed OSD not receiving temperature data - exposed `ThermalProvider` from FanService
- Added live settings updates - `UpdateSettings()` method for real-time changes
- Reduced opacity default from 0.85 to 0.6 for less intrusive overlay
- Smaller, more compact design (180px width, 10pt font)
- Added new metrics:
  - Current mode display (Auto, Performance, etc.)
  - FPS placeholder (requires game integration)
- Changed default position to TopRight
- Changed default hotkey to Ctrl+Shift+F12 (F12 often blocked)

---

### OMEN Key Detection Expanded
**Files Changed:** `Services/OmenKeyService.cs`, `Models/FeaturePreferences.cs`, `Models/AppConfig.cs`

**Problem:** OMEN key not detected on some laptop models.

**Solution:** Added support for additional key codes reported by users:
- VK 157 (0x9D) - Some OMEN models
- F24 (0x87) - Some OMEN models  
- VK_LAUNCH_APP1 (0xB6) - Newer OMEN models
- VK_LAUNCH_APP2 (0xB7) - Common OMEN key
- Scan code 0x009D added to detection list
- Enhanced debug logging for key detection
- Changed default `OmenKeyInterceptionEnabled` from `false` to `true`
- Changed default action from "ShowQuickPopup" to "ToggleOmenCore"

**Supported Key Codes:**
```
VK_LAUNCH_APP2 (0xB7) - Most common
VK_LAUNCH_APP1 (0xB6) - Some models
VK 157 (0x9D) - Some models
F24 (0x87) - Some models
VK_OEM_OMEN (0xFF) - Some models
Scan codes: 0xE045, 0xE046, 0x0046, 0x009D
```

---

### Window Toggle Fix (OMEN Key / Hotkey)
**Files Changed:** `ViewModels/MainViewModel.cs`

**Problem:** Pressing OMEN key or Ctrl+Shift+O when app started minimized to tray would not show the window.

**Solution:**
- Added `Topmost = true` temporarily to force window to foreground
- Fixed `ShowInTaskbar` toggle when hiding/showing
- Better logging for debugging window state

---

### System Tray Improvements
**Files Changed:** `Utils/TrayIconService.cs`, `Models/FeaturePreferences.cs`, `ViewModels/SettingsViewModel.cs`

**Changes:**
- Changed icon from 48px circle to 32px rounded rectangle (proper Windows tray size)
- Increased font weight to ExtraBold for better visibility
- Added `TrayTempDisplayEnabled` setting to disable temp display on tray icon
- Added UI toggle in Settings for "Show temperature on tray icon"

---

## Previous Session Fixes

### System Tray Crash Fix
**Files Changed:** `Utils/TrayIconService.cs`

**Problem:** Right-clicking the system tray icon caused immediate crash with error:
```
'System.Windows.Controls.Border' already has a child and cannot add 'System.Windows.Controls.Primitives.Popup'
```

**Root Cause:** Custom MenuItem ControlTemplate tried to append a Popup element to a Border that already had a Grid child. WPF Border can only have one child element.

**Solution:** Replaced complex ControlTemplate with simpler style-based approach that:
- Uses property setters instead of template override
- Adds hover/pressed triggers for visual feedback
- Preserves dark theme appearance without breaking MenuItem functionality

---

### OmenCap.exe Detection
**Files Changed:** 
- `Services/OmenGamingHubCleanupService.cs`
- `Hardware/CpuUndervoltProvider.cs`
- `ViewModels/SystemControlViewModel.cs`

**Problem:** Users reported "XTU blocking undervolt" error even without Intel XTU installed. Investigation revealed HP's `OmenCap.exe` persists in Windows DriverStore after uninstalling OMEN Gaming Hub:
```
C:\Windows\System32\DriverStore\FileRepository\hpomencustomcapcomp.inf_amd64_<hash>\OmenCap.exe
```

This component runs automatically via Windows driver infrastructure and holds exclusive access to MSR (Model Specific Register), blocking undervolting.

**Solution:**
1. Added OmenCap to process detection list in CpuUndervoltProvider
2. Added DriverStore pattern detection in OGH cleanup service
3. Added detailed removal instructions in UI and logs:
   - `pnputil /enum-drivers | findstr /i omen` to find driver
   - `pnputil /delete-driver oem##.inf /force` to remove
   - Reboot required after removal

**User-Facing Messages:**
- Undervolt tab shows "HP OmenCap (DriverStore)" as external controller
- Settings > Clean OGH logs detailed removal instructions if OmenCap found
- Clear step-by-step guide for complete removal

---

### UI Polish Fixes

**Sidebar Redesign:**
- Increased sidebar width from 200px to 230px (MinWidth: 210, MaxWidth: 280)
- Enlarged logo from 44x44 to 56x56 pixels for better brand presence
- Increased app title font size (14 → 15)
- Better vertical spacing throughout:
  - More margin below logo section
  - More margin above status indicator (5→8px)
  - More margin above Quick Actions section (0→4px)
  - More margin above System Info section (10→14px)
  - Taller bottom buttons (28→30px) with increased spacing (4→5px)
- Reduces the large gap between System Info and bottom action buttons

**EC Keyboard Setting Visibility:**
- "Experimental EC Keyboard" toggle now always visible in Settings > Features
- Previously hidden until "Keyboard Backlight Control" was enabled
- Users can now find and enable it directly

**Version Number:**
- Updated to v1.5.0-beta in VERSION.txt, TrayIconService, and About window

---

## Completed Changes

### Phase 0: Quick Bug Fixes ✅

#### 1. Settings Persistence Fix
**Files Changed:** `ViewModels/SystemControlViewModel.cs`

**Problem:** TCC Offset and GPU Power Boost settings would reset to defaults after reboot because:
- Used fragile `Task.Run` with hardcoded delays (2000ms/3000ms)
- No retry logic if WMI BIOS wasn't ready
- Settings would silently fail to apply

**Solution:**
- Added `ReapplySettingWithRetryAsync()` method with:
  - Exponential backoff (up to 5 retries)
  - Initial delay: 1500ms (GPU), 2000ms (TCC)
  - Max delay cap to prevent excessive waiting
  - Random jitter to avoid thundering herd
- Modified `ReapplySavedGpuPowerBoost()` to throw on failure (enables retry)
- Modified `ReapplySavedTccOffset()` to throw on failure (enables retry)

**Code Pattern:**
```csharp
await ReapplySettingWithRetryAsync(
    "GPU Power Boost",
    () => ReapplySavedGpuPowerBoost(savedLevel),
    maxRetries: 5,
    initialDelayMs: 1500,
    maxDelayMs: 5000
);
```

---

#### 2. AC/Battery Detection Enhancement
**Files Changed:** `Services/PowerAutomationService.cs`

**Problem:** Power automation wasn't triggering profile changes on AC/battery transitions.

**Solution:**
- Added comprehensive logging for `PowerModeChanged` events
- Implemented 3-tier fallback detection:
  1. `System.Windows.Forms.SystemInformation.PowerStatus` (primary)
  2. WinRT `Battery.AggregateBattery.GetReport()` (secondary)
  3. WMI `Win32_Battery` query (tertiary)
- Added debug logging at each detection stage to help diagnose issues

**Logging Output:**
```
PowerModeChanged event received: Mode=StatusChange
Current AC state detected: True (was: False)
Power state changed: AC Connected
Power automation is enabled, applying profile...
```

---

#### 3. Fan Curve Editor UI Fix
**Files Changed:** `Controls/FanCurveEditor.xaml.cs`

**Problem:** Fan curve points would visually "jump" or jitter when:
- Adding new points
- Dragging points
- Collection was being sorted

**Root Cause:** `CollectionChanged` events triggered `RenderCurve()` multiple times during batch operations (Clear → Add → Add → Add).

**Solution:**
- Added `_suppressRender` flag to prevent cascading renders
- Wrapped batch collection operations in suppress blocks:
```csharp
_suppressRender = true;
try
{
    CurvePoints.Clear();
    foreach (var p in sorted)
        CurvePoints.Add(p);
}
finally
{
    _suppressRender = false;
}
RenderCurve(); // Single render after batch complete
```

---

### Phase 1: Verification Layer ✅

#### 1. FanVerificationService (NEW)
**Files Created:** `Services/FanVerificationService.cs`

**Purpose:** Provides closed-loop verification for fan control commands. After setting a fan speed, reads back actual RPM to verify the setting was applied.

**Key Features:**
- Uses `FanService.FanTelemetry` for RPM readings (hardware-agnostic)
- Uses `HpWmiBios.SetFanLevel(fan1, fan2)` for setting levels
- 2500ms wait for fan mechanical inertia
- 20% RPM tolerance for verification
- Automatic retry on verification failure
- Detailed `FanApplyResult` with diagnostics

**Classes:**
- `FanVerificationService` - Main service
- `FanApplyResult` - Result object with:
  - `RequestedPercent`, `AppliedLevel`
  - `ActualRpmBefore`, `ActualRpmAfter`
  - `ExpectedRpm`, `DeviationPercent`
  - `WmiCallSucceeded`, `VerificationPassed`
  - `ErrorMessage`, `Duration`

**Usage:**
```csharp
var verifier = new FanVerificationService(wmiBios, fanService, logging);
var result = await verifier.ApplyAndVerifyFanSpeedAsync(fanIndex: 0, targetPercent: 80);
if (!result.Success)
{
    _logging.Warn($"Fan verification failed: {result.ErrorMessage}");
}
```

---

#### 2. SettingsRestorationService (NEW)
**Files Created:** `Services/SettingsRestorationService.cs`

**Purpose:** Centralized service for restoring saved settings on startup with proper retry logic. Designed to integrate with `StartupSequencer`.

**Features:**
- Restores GPU Power Boost, TCC Offset, Fan Preset
- Hardware readiness check before restoration
- Verification after applying settings
- Event-based notification of restoration status

**Restoration Tasks:**
| Priority | Task | Retries | Delay |
|----------|------|---------|-------|
| 10 | Wait for Hardware | 10 | 1500ms |
| 20 | Restore GPU Power Boost | 3 | 1000ms |
| 30 | Restore TCC Offset | 3 | 1000ms |
| 40 | Restore Fan Preset | 3 | 1000ms |

---

## In Progress

### Phase 2: WinRing0 Removal ✅
**Status:** Complete

**Goal:** Abstract WinRing0 behind interfaces and prefer PawnIO. Mark WinRing0 as deprecated/legacy.

**Rationale:**
- WinRing0 is blocked by Secure Boot and Memory Integrity
- Many users can't disable these security features
- WMI BIOS provides fan/thermal/GPU control without drivers
- PawnIO is Secure Boot compatible for advanced features

**Changes Made:**

#### 1. IMsrAccess Interface (NEW)
**Files Created:** `Hardware/IMsrAccess.cs`

**Purpose:** Abstract interface for MSR access backends (PawnIO, WinRing0).

**Methods:**
- `bool IsAvailable` - Check if backend is usable
- `ReadCoreVoltageOffset()` / `ApplyCoreVoltageOffset(int mv)` - CPU core voltage
- `ReadCacheVoltageOffset()` / `ApplyCacheVoltageOffset(int mv)` - CPU cache voltage
- `ReadTccOffset()` / `SetTccOffset(int offset)` / `ReadTjMax()` / `GetEffectiveTempLimit()` - Thermal control

#### 2. MsrAccessFactory (NEW)
**Files Created:** `Hardware/MsrAccessFactory.cs`

**Purpose:** Factory that prefers PawnIO (Secure Boot compatible) over WinRing0 (legacy fallback).

**Properties:**
- `ActiveBackend` - Enum: `None`, `PawnIO`, `WinRing0`
- `StatusMessage` - Human-readable status

#### 3. PawnIOMsrAccess Updates
**Files Changed:** `Hardware/PawnIOMsrAccess.cs`

**Changes:**
- Now implements `IMsrAccess` interface
- Added TCC methods: `ReadTccOffset()`, `ReadTjMax()`, `SetTccOffset()`, `GetEffectiveTempLimit()`
- Feature parity with WinRing0MsrAccess

#### 4. WinRing0MsrAccess Deprecation
**Files Changed:** `Hardware/WinRing0MsrAccess.cs`

**Changes:**
- Added `[Obsolete]` attribute with deprecation warning
- Now implements `IMsrAccess` interface
- Retained only as legacy fallback

#### 5. Consumer Updates
**Files Changed:**
- `ViewModels/SystemControlViewModel.cs` - Uses `MsrAccessFactory.Create()` instead of `new WinRing0MsrAccess()`
- `Services/SettingsRestorationService.cs` - Uses `MsrAccessFactory.Create()` instead of `new WinRing0MsrAccess()`
- `Hardware/CpuUndervoltProvider.cs` - Refactored `IntelUndervoltProvider` to use single `IMsrAccess` via factory

#### 6. UI Updates
**Files Changed:**
- `ViewModels/SettingsViewModel.cs`:
  - WinRing0 status now shows as "Legacy" with orange color
  - Install Driver button now opens PawnIO website directly
  - Updated messaging to recommend PawnIO
- `Views/SettingsView.xaml`:
  - Removed "WinRing0 is a legacy fallback" text
  - Changed button from "Get Driver Backend" to "Get PawnIO (Recommended)"
  - Updated Secure Boot status text

---

### Phase 3: Keyboard RGB Rework ✅
**Status:** Complete

**Goal:** Multi-backend RGB system with model database and automatic detection.

**Files Created:**

#### 1. IKeyboardBackend Interface
**File:** `Services/KeyboardLighting/IKeyboardBackend.cs`

**Purpose:** Common interface for all keyboard lighting backends.

**Key Types:**
- `KeyboardMethod` enum: `Unknown`, `Unsupported`, `BacklightOnly`, `ColorTable2020`, `NewWmi2023`, `EcDirect`, `HidPerKey`
- `KeyboardType` enum: `Unknown`, `FourZone`, `FourZoneTkl`, `PerKeyRgb`, `BacklightOnly`, `Desktop`
- `KeyboardEffect` enum: `Static`, `Breathing`, `ColorCycle`, `Wave`, `Reactive`, `Off`
- `RgbApplyResult` class: Result with success, verification, timing, and error info

**Interface Methods:**
- `InitializeAsync()` - Initialize and check availability
- `SetZoneColorsAsync(Color[])` - Set all 4 zone colors
- `SetZoneColorAsync(zone, color)` - Set single zone
- `ReadZoneColorsAsync()` - Read current colors (verification)
- `SetBrightnessAsync(brightness)` - Set backlight brightness
- `SetBacklightEnabledAsync(enabled)` - Toggle backlight
- `SetEffectAsync(effect, colors, speed)` - Apply lighting effect

#### 2. KeyboardModelDatabase
**File:** `Services/KeyboardLighting/KeyboardModelDatabase.cs`

**Purpose:** Database of known OMEN models and their keyboard configurations.

**Known Models:**
| Product ID | Model | Type | Preferred Method |
|------------|-------|------|------------------|
| 8A14/8A15 | OMEN 15 (2020) | FourZoneTkl | ColorTable2020 |
| 8BAD/8BAE | OMEN 15 (2021) | FourZoneTkl | ColorTable2020 |
| 8BAF/8BB0 | OMEN 16 (2021) | FourZone | ColorTable2020 |
| 8CD0/8CD1 | OMEN 16 (2022) | FourZone | ColorTable2020 |
| 8E67/8E68 | OMEN 16 (2023) | FourZone | NewWmi2023 |
| 8BCD | OMEN 16-xd0xxx (2024) | FourZone | ColorTable2020 |
| 8E69 | OMEN 17-ck2xxx (2024) | FourZone | NewWmi2023 |
| ah0097nr | OMEN Max 16 (2025) | PerKeyRgb | HidPerKey |

**Features:**
- Model lookup by Product ID or model name
- Default configuration for unknown models
- EC register addresses per model
- Fallback method chain

#### 3. WmiBiosBackend
**File:** `Services/KeyboardLighting/WmiBiosBackend.cs`

**Purpose:** WMI BIOS ColorTable backend for 2020-2022 models.

**Features:**
- Uses existing `HpWmiBios.SetColorTable()` method
- Readback verification via `GetColorTable()`
- 5-unit tolerance for color verification
- 4-zone support (Right, Middle, Left, WASD)

#### 4. EcDirectBackend
**File:** `Services/KeyboardLighting/EcDirectBackend.cs`

**Purpose:** Direct EC register writes for models where WMI fails.

**Features:**
- Model-specific EC register addresses
- Default addresses: 0xB1-0xBC (colors), 0xBD (brightness), 0xBE (effect)
- Readback verification
- Requires `ExperimentalEcKeyboardEnabled` setting or model-specific config

#### 5. KeyboardLightingServiceV2
**File:** `Services/KeyboardLighting/KeyboardLightingServiceV2.cs`

**Purpose:** Unified service with automatic backend detection.

**Features:**
- Model-based configuration lookup
- Auto-detection with fallback chain
- Telemetry tracking (success/failure rates)
- Test pattern functionality
- Backend switching for debugging

**Usage:**
```csharp
var kbService = new KeyboardLightingServiceV2(logging, wmiBios, ecAccess, configService, systemInfoService);
var probeResult = await kbService.InitializeAsync();

if (kbService.IsAvailable)
{
    var colors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.White };
    var result = await kbService.SetZoneColorsAsync(colors);
    
    if (!result.Success)
        _logging.Warn($"RGB failed: {result.FailureReason}");
}
```

---

### Phase 4: AMD Curve Optimizer ✅
**Status:** Already Implemented (Discovered existing code)

**Note:** During Phase 4 work, we discovered that AMD Curve Optimizer support was already fully implemented in the codebase:

**Existing Files:**
- `Hardware/RyzenControl.cs` - AMD CPU family detection
- `Hardware/RyzenSmu.cs` - PawnIO-based SMU communication
- `Hardware/AmdUndervoltProvider.cs` - Full Curve Optimizer implementation

**Supported Features:**
- All-Core Curve Optimizer offset (-30 to +30)
- iGPU Curve Optimizer offset
- STAPM limit adjustment
- Temperature limit adjustment
- Family support: Zen1 through Strix Halo/Fire Range

---

### Phase 5: GPU Dynamic Boost ✅
**Status:** Complete

**Goal:** Extended GPU power levels for RTX 5080 and newer GPUs that support +25W boost.

**Files Changed:**

#### 1. GpuPowerLevel Enum Extended
**File:** `Hardware/HpWmiBios.cs`

**Changes:**
- Added `Extended3 = 0x03` - For RTX 5080 +25W boost
- Added `Extended4 = 0x04` - Future-proofing
- Updated SetGpuPower() to send PPAB values > 1 for extended levels
- Enhanced logging to show CustomTgp/PPAB values

**New Enum:**
```csharp
public enum GpuPowerLevel : byte
{
    Minimum = 0x00,   // Base TGP only
    Medium = 0x01,    // Custom TGP (+15W typical)
    Maximum = 0x02,   // Custom TGP + PPAB (+15-25W)
    Extended3 = 0x03, // Extended PPAB for RTX 5080
    Extended4 = 0x04  // Future models
}
```

#### 2. UI Updates
**Files Changed:**
- `ViewModels/SystemControlViewModel.cs`:
  - Added "Extended" option to `GpuPowerBoostLevels` collection
  - Updated `GpuPowerBoostDescription` for Extended level
  - Updated `ApplyGpuPowerBoost()` and `DetectGpuPowerBoost()` methods
- `Views/SystemControlView.xaml`:
  - Updated description text to mention RTX 5080 +25W
- `Views/AdvancedView.xaml`:
  - Updated description text to mention Extended option

#### 3. Settings Restoration
**File:** `Services/SettingsRestorationService.cs`

- Added handling for "Extended" level in `RestoreGpuPowerBoostAsync()`
- Maps to `GpuPowerLevel.Extended3`

---

### Phase 6: Fan Calibration ✅
**Status:** Complete

**Goal:** Per-model fan calibration with closed-loop verification.

**Files Created:**

#### 1. FanCalibrationProfile Model
**File:** `Services/FanCalibration/FanCalibrationProfile.cs`

**Purpose:** Stores per-model fan calibration data.

**Key Properties:**
- `ProductId` - HP Product ID
- `ModelName` - Human-readable name
- `MaxLevel` - Model-specific max (55 or 100)
- `MinSpinLevel` - Level where fans actually start spinning
- `Fan0LevelToRpm` / `Fan1LevelToRpm` - Level → RPM mappings
- `Fan0MaxRpm` / `Fan1MaxRpm` - Maximum measured RPM

**Methods:**
- `PercentToLevel(int percent)` - Convert target % to model-specific level
- `GetExpectedRpm(int fanIndex, int percent)` - Interpolate expected RPM
- `RpmToLevel(int fanIndex, int rpm)` - Reverse lookup

**Supporting Classes:**
- `FanApplyResult` - Result with verification status, timing, and error info
- `CalibrationStep` - Single step in calibration wizard

#### 2. FanCalibrationService
**File:** `Services/FanCalibration/FanCalibrationService.cs`

**Purpose:** Fan calibration wizard and closed-loop verification.

**Key Features:**

**Calibration Wizard:**
- Steps through levels: 0, 10, 20, 30, 40, 45, 50, 55
- Waits 3000ms for fan response at each level
- Records actual RPM from hardware
- Builds level → RPM mapping
- Detects minimum spin level and max RPM
- Saves profile to JSON

**Closed-Loop Verification:**
```csharp
public async Task<FanApplyResult> ApplyAndVerifyAsync(int fanIndex, int targetPercent)
{
    // 1. Get expected RPM from calibration data
    // 2. Read current RPM
    // 3. Apply setting
    // 4. Wait 3000ms for fan response
    // 5. Read actual RPM
    // 6. Compare with expected (15% tolerance)
    // 7. Retry once if verification fails
}
```

**Profile Management:**
- Profiles stored at `%LocalAppData%\OmenCore\fan_calibration_profiles.json`
- Auto-loads profile on startup based on SystemSku
- Falls back to default profile if no calibration exists

**Events:**
- `CalibrationStepCompleted` - After each level tested
- `CalibrationCompleted` - Wizard finished successfully
- `CalibrationError` - Error during calibration

#### 3. FanCalibrationViewModel
**File:** `ViewModels/FanCalibrationViewModel.cs`

**Purpose:** UI bindings for fan calibration wizard.

**Properties:**
- `Status` - Current status message
- `Progress` - 0-100 progress
- `IsCalibrating` - True during wizard
- `CalibrationInfo` - Profile summary text
- `CalibrationSteps` - Observable collection of completed steps
- `Profile` - Current FanCalibrationProfile

**Commands:**
- `StartCalibrationCommand` - Begin calibration wizard
- `CancelCalibrationCommand` - Stop calibration early

---

## Planned

### Backend Requirements (from ROADMAP)

| Feature             | Backend                   |
|---------------------|---------------------------|
| Fans / Thermal      | WMI                       |
| Performance modes   | WMI                       |
| GPU power           | WMI                       |
| Intel undervolt     | PawnIO (capability-gated) |
| AMD Curve Optimizer | PawnIO                    |
| Zone RGB            | WMI (new V2 system)       |
| Per-key RGB         | HID / OpenRGB             |
| EC fallback         | PawnIO (opt-in, warned)   |

### Files Modified This Release

| File | Changes |
|------|---------|
| `ViewModels/SystemControlViewModel.cs` | Settings restoration with retry, IMsrAccess, Extended GPU level |
| `Services/PowerAutomationService.cs` | Enhanced AC/battery detection |
| `Controls/FanCurveEditor.xaml.cs` | Suppressed render during batch updates |
| `Services/FanVerificationService.cs` | **NEW** - Fan RPM verification |
| `Services/SettingsRestorationService.cs` | **NEW** - Centralized settings restoration, Extended GPU support |
| `Hardware/IMsrAccess.cs` | **NEW** - MSR access interface |
| `Hardware/MsrAccessFactory.cs` | **NEW** - PawnIO-first factory |
| `Hardware/PawnIOMsrAccess.cs` | Added TCC methods, implements IMsrAccess |
| `Hardware/WinRing0MsrAccess.cs` | Deprecated, implements IMsrAccess |
| `Hardware/CpuUndervoltProvider.cs` | Uses MsrAccessFactory |
| `Hardware/HpWmiBios.cs` | Extended GpuPowerLevel enum (Extended3/4) |
| `Services/KeyboardLighting/IKeyboardBackend.cs` | **NEW** - Keyboard backend interface |
| `Services/KeyboardLighting/KeyboardModelDatabase.cs` | **NEW** - Model compatibility DB |
| `Services/KeyboardLighting/WmiBiosBackend.cs` | **NEW** - WMI ColorTable backend |
| `Services/KeyboardLighting/EcDirectBackend.cs` | **NEW** - EC register backend |
| `Services/KeyboardLighting/KeyboardLightingServiceV2.cs` | **NEW** - Multi-backend service |
| `Services/FanCalibration/FanCalibrationProfile.cs` | **NEW** - Fan calibration model |
| `Services/FanCalibration/FanCalibrationService.cs` | **NEW** - Calibration wizard & verification |
| `ViewModels/FanCalibrationViewModel.cs` | **NEW** - Calibration UI bindings |
| `Views/SystemControlView.xaml` | Updated GPU boost descriptions |
| `Views/AdvancedView.xaml` | Updated GPU boost descriptions |
| `Views/MainWindow.xaml` | Title bar polish, version badge styling |
| `Views/DashboardView.xaml` | Improved header spacing |
| `Views/SettingsView.xaml` | Added subtitle description |
| `Views/LightingView.xaml` | Added subtitle description |
| `Views/GeneralView.xaml` | Header padding improvements |
| `Views/AboutWindow.xaml` | Updated feature list, version badge with BETA label |
| `Views/SplashWindow.xaml` | Version badge styling, v1.5.0-beta label |
| `Styles/ModernStyles.xaml` | Added FocusRing color, section comments |
| `installer/OmenCoreInstaller.iss` | Improved welcome, autostart task, uninstall cleanup |
| `installer/generate-wizard-images.ps1` | Modern styling, version badge, purple theme |

---

### UI Polish ✅
**Status:** Complete

**Goal:** Final visual pass for consistency, readability, and modern appearance.

**Changes Made:**

#### 1. Title Bar Improvements
**File:** `Views/MainWindow.xaml`
- Increased title bar height (40→44px) for better touch targets
- Version label now in styled badge with background
- Changed font-weight from Bold to SemiBold for cleaner look
- Added `RenderOptions.BitmapScalingMode="HighQuality"` for logo

#### 2. View Headers Consistency
**Files:** Dashboard, Settings, Lighting, Advanced, General Views
- All main views now have consistent subtitle descriptions
- Added opacity (0.9) for subtle secondary text treatment
- Standardized margins (0,0,0,20 for header sections)
- Better visual hierarchy with spacing improvements

#### 3. Splash Screen Polish
**File:** `Views/SplashWindow.xaml`
- Version now displayed in styled badge with background
- Updated subtitle color for better contrast
- Added v1.5.0-beta label at bottom

#### 4. About Window Updates
**File:** `Views/AboutWindow.xaml`
- Updated feature list to reflect v1.5.0 capabilities
- Added BETA badge next to version number
- Better spacing between feature items

#### 5. Installer Improvements
**File:** `installer/OmenCoreInstaller.iss`
- New welcome message highlighting v1.5.0 features
- Added "Start with Windows" task option
- Added "Create Start Menu shortcut" task
- Improved PawnIO driver handling
- Better uninstall cleanup

#### 6. Installer Graphics Script
**File:** `installer/generate-wizard-images.ps1`
- Modern purple/dark gradient theme
- Version badge on wizard images
- Feature icons with visual hierarchy
- Diagonal accent lines for modern feel
- Fallback design when logo unavailable

---

## Testing Checklist

- [ ] Settings persist after reboot (TCC, GPU Power Boost)
- [ ] AC/battery profile switching works
- [ ] Fan curve editor doesn't jitter
- [ ] Fan verification logs show RPM readback
- [ ] App starts without WinRing0 (WMI-only mode)
- [ ] Keyboard RGB works via WMI BIOS backend
- [ ] Keyboard RGB test pattern runs successfully
- [ ] Model detection finds correct keyboard config
- [ ] GPU Extended boost option visible in dropdown
- [ ] Extended boost saves/restores correctly
- [ ] Fan calibration wizard completes
- [ ] Fan calibration profile saves to JSON
- [ ] Fan verification uses calibration data

---

*Last Updated: December 17, 2025*
