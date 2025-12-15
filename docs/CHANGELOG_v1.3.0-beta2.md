# OmenCore v1.3.0-beta2 Changelog

**Release Date:** 2025-01-XX

This beta release adds new features to make OmenCore a complete replacement for HP OMEN Gaming Hub.

## New Features

### Quick Popup UI (Middle-Click Tray)
- **Quick Access Popup**: Middle-click the tray icon to show a compact popup
- Shows CPU/GPU temperatures and load at a glance
- Quick buttons for Fan Mode (Auto/Max/Quiet)
- Quick buttons for Performance Mode (Balanced/Performance/Quiet)
- Display Off button (turn off screen, system keeps running)
- Refresh Rate toggle button

### RGB Keyboard Zones (WMI BIOS Backend)
- Added WMI BIOS backend for keyboard RGB control
- `SetColorTable()` - Set all 4 zones at once
- `SetZoneColor()` - Set individual zone colors
- `GetColorTable()` - Read current zone colors
- Works on models where EC access fails

### Display Controls (Already in Tray Menu)
- **Turn Off Display**: Screen off while downloads/music continue
- **Refresh Rate Toggle**: Quick switch between 60Hz/144Hz/165Hz
- High/Low refresh rate presets from tray menu

### Modular Feature Toggles
- New Settings section: "Feature Modules"
- Enable/disable features to reduce resource usage:
  - Corsair iCUE Integration
  - Logitech G HUB Integration
  - Game Profile Auto-Switching
  - Keyboard Backlight Control
  - Custom Fan Curves
  - Power Source Automation
  - GPU Mode Switching
  - CPU Undervolt Controls
- Restart OmenCore after changing feature toggles

### OMEN Key Interception (Experimental)
- Capture physical OMEN key press
- Show OmenCore instead of HP OMEN Gaming Hub
- Configurable action: Show Popup, Show Window, Toggle Fan, Toggle Perf
- Uses low-level keyboard hook
- Disabled by default (enable in Settings → Feature Modules)

## Technical Changes

### New Files
- `Services/DisplayService.cs` - Display control (refresh rate, power)
- `Services/OmenKeyService.cs` - OMEN key interception
- `Views/QuickPopupWindow.xaml` - Quick popup UI
- `Models/FeaturePreferences.cs` - Feature toggle settings

### Changes from v1.3.0-beta1
- Added QuickPopup UI with middle-click support
- Added Feature Toggles settings section
- Added OMEN Key interception service
- Updated TrayIconService for QuickPopup
- Updated SettingsViewModel with new toggles

## Known Issues

- OMEN key code varies by laptop model - may not work on all models
- Feature toggles require app restart to take effect
- QuickPopup auto-hides when clicking outside

## Installation

1. Download the latest release
2. Extract to any folder
3. Run OmenCore.exe as Administrator
4. Middle-click tray icon for Quick Popup
5. Right-click tray icon for full menu

## Feedback

This is a beta release. Please report any issues on GitHub:
https://github.com/theantipopau/omencore/issues
