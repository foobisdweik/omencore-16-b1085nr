# v2.0.1 Bug Report Tracker

**Report Date:** January 2, 2026  
**Source:** Reddit user feedback  
**Version:** 2.0.1-beta  
**Last Updated:** January 2026

## Critical Bugs (App Crashes/Non-Functional)

### 1. ✅ CPU Undervolting Toggle Crashes App
**Status:** 🟢 FIXED  
**Symptom:** Toggling CPU undervolting in Settings or System Control crashes the app  
**Root Cause:** Null reference in `ApplyUndervoltAsync()` when `config.Undervolt` was null  
**Fix Applied:** Added null coalescing (`config.Undervolt ?? new UndervoltPreferences()`) and wrapped in try-catch  
**Files:** `SystemControlViewModel.cs`

### 2. ✅ TCC Offset Doesn't Do Anything
**Status:** 🟢 FIXED  
**Symptom:** Setting TCC offset via slider has no effect on CPU throttling  
**Root Cause:** MSR write silently fails due to Secure Boot/HVCI, but no user feedback  
**Fix Applied:** Added verification read-back after write, and user dialogs for success/failure with HVCI hint  
**Files:** `SystemControlViewModel.cs`

### 3. ✅ 80% Battery Charge Cap Not Working
**Status:** 🟢 FIXED  
**Symptom:** Enabling 80% charge limit has no effect - battery still charges to 100%  
**Root Cause:** `SetBatteryCareMode()` returning false silently, no feedback to user  
**Fix Applied:** Added null check, verification read-back, and user dialogs for success/failure  
**Files:** `SettingsViewModel.cs`

### 4. ✅ Fan Cleaner Errors After PawnIO Install
**Status:** 🟢 FIXED  
**Symptom:** Fan cleaner works initially with PawnIO but errors after refresh  
**Root Cause:** `DetermineActiveBackend()` called once at construction, not refreshed when PawnIO installed  
**Fix Applied:** Added `RefreshBackend()` method and call it at start of `StartCleaningAsync()`  
**Files:** `FanCleaningService.cs`

---

## High Priority Bugs (Feature Not Working)

### 5. ❌ Per-Key RGB Update Doesn't Work
**Status:** 🟡 Incomplete Implementation  
**Symptom:** Per-key RGB controls have no effect on keyboard  
**Root Cause:** `HidPerKey` backend returns `null` - marked as "TODO: not yet implemented"  
**Fix Required:** Implement HID per-key backend for OMEN keyboards  
**Files:** `KeyboardLightingServiceV2.cs`, need new `HidPerKeyBackend.cs`

### 6. ❌ Calculator Key Launches Software When Mapped to OMEN Key
**Status:** 🟡 Needs Investigation  
**Symptom:** Calculator key triggers OmenCore when OMEN key interception enabled  
**Likely Cause:** Calculator key shares scan code with OMEN key on some models  
**Fix:** Add scan code filtering to exclude Calculator key (VK_LAUNCH_APP2 overlap)
**Files:** `OmenKeyService.cs`

### 7. ❌ Overlay Statistics Toggle Doesn't Work
**Status:** 🟡 Needs Investigation  
**Symptom:** Hotkey to toggle overlay stats has no effect  
**Investigation:**
- [ ] Check if overlay toggle hotkey is registered
- [ ] Check if `OsdOverlayWindow` visibility toggle works
**Files:** `OsdOverlayWindow.xaml.cs`, `OsdService.cs`

### 8. ✅ Power/Fan Profiles Don't Switch When Plugged/Unplugged
**Status:** 🟢 FIXED  
**Symptom:** AC/Battery profile switching not happening automatically  
**Root Cause:** `PowerAutomationService.IsEnabled` never synced from UI toggle - SettingsViewModel saved to config but didn't notify the service at runtime  
**Fix Applied:** Injected `PowerAutomationService` into `SettingsViewModel` and synced `IsEnabled` and preset properties when UI toggles change  
**Files:** `SettingsViewModel.cs`, `MainViewModel.cs`

---

## UI/UX Bugs

### 9. ✅ Initial HotKey Popup Position Issue
**Status:** 🟢 FIXED  
**Symptom:** HotKey OSD popup doesn't fully appear on bottom right initially, fixes after subsequent presses  
**Root Cause:** Window positioning calculated before layout complete, `ActualWidth`/`ActualHeight` were 0  
**Fix Applied:** Deferred `PositionWindow()` call using `Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ...)` after `Show()`  
**Files:** `HotkeyOsdWindow.xaml.cs`

---

## Feature Requests

### 10. 💡 Add SSD Temperature Monitoring
**Status:** 📋 Enhancement  
**Request:** Show SSD/NVMe temperature alongside CPU/GPU temps  
**Implementation:** Use LibreHardwareMonitor or SMART to read NVMe temps  
**Files:** `HardwareMonitoringService.cs`, `DashboardViewModel.cs`

---

## Fix Priority Order

## Bug Fix Summary

| # | Issue | Status | Priority |
|---|-------|--------|----------|
| 1 | CPU Undervolting Crash | ✅ Fixed | Critical |
| 2 | TCC Offset Not Working | ✅ Fixed | High |
| 3 | Battery Charge Cap | ✅ Fixed | High |
| 4 | Fan Cleaner PawnIO Error | ✅ Fixed | High |
| 5 | Per-Key RGB | ❌ Needs Implementation | Medium |
| 6 | Calculator Key Conflict | ❌ Needs Investigation | Medium |
| 7 | Overlay Toggle | ❌ Needs Investigation | Medium |
| 8 | AC/Battery Profile Switching | ✅ Fixed | Medium |
| 9 | Hotkey Popup Position | ✅ Fixed | Low |
| 10 | SSD Temps | 📋 Enhancement | Feature Request |

**Fixed:** 6/9 bugs (66%)  
**Remaining:** 3 bugs + 1 feature request

---

## Testing Checklist for Fixes

- [ ] Test undervolting toggle on Intel system
- [ ] Test undervolting toggle on AMD system
- [ ] Test TCC offset with read-back verification
- [ ] Test battery charge cap on AC power
- [ ] Test fan cleaner with fresh PawnIO install
- [ ] Test fan cleaner after app restart
- [ ] Test AC/Battery profile switching by unplugging
- [ ] Test OMEN key vs Calculator key discrimination
- [ ] Test overlay toggle hotkey
- [ ] Verify hotkey popup positions correctly on first show
