# OmenCore v1.1.2 Changelog

## 🚀 OmenCore v1.1.2 - User Feedback Hotfix Release

**Release Date**: December 13, 2025  
**Download**: [GitHub Releases](https://github.com/theantipopau/omencore/releases/tag/v1.1.2)

---

This release addresses user-reported issues from [GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6) and other community feedback regarding startup, fan control, temperature monitoring, and UI clarity.

---

## ✨ New Features

### 🎮 Gaming Fan Preset
- **NEW**: Added "Gaming" quick preset for aggressive cooling during gaming sessions
- Uses Performance thermal policy (0x31) for proper fan ramping at high temps
- Aggressive fan curve: 45°C→30%, 55°C→45%, 65°C→65%, 75°C→85%, 80°C→100%
- Recommended for users experiencing inadequate fan response in "Auto" mode

### 🔄 Task Scheduler Startup (Elevated)
- **NEW**: Windows startup now uses Task Scheduler with `HIGHEST` run level
- Solves issue where OmenCore wouldn't start on boot even with startup enabled
- Task Scheduler approach properly elevates the app (required for hardware access)
- Falls back to registry Run key if Task Scheduler fails
- Users no longer need to manually create scheduled tasks

### 💾 GPU Power Boost Persistence  
- **NEW**: GPU Power Boost level now saved to config and restored on startup
- Last used setting ("Minimum", "Medium", "Maximum") persists across restarts
- Note: Hardware may still reset after sleep/reboot on some models (BIOS limitation)

---

## 🎯 User Experience Improvements

### 📊 Fan Curve Editor Overhaul
- **NEW**: "How Fan Curves Work" explanation box in the curve editor
  - Explains temperature → fan speed mapping with examples
  - Helps users understand how to create effective curves
- **NEW**: Preset guide showing what each mode does:
  - Gaming: Aggressive cooling, fans ramp at 60°C+
  - Auto: BIOS default thermal policy (may be conservative on some models)
  - Silent: Quiet operation but allows higher temperatures
- **IMPROVED**: Better tooltips on quick preset buttons explaining their behavior
- **RENAMED**: "Quiet" button now labeled "Silent" for clarity

### 🔒 Secure Boot Banner Clarification
- **IMPROVED**: Secure Boot warning now explains what's actually limited
- **OLD**: "Some features limited" (vague)
- **NEW**: "EC access limited. Use WMI/OGH or install PawnIO." (actionable)
- Added tooltip with full explanation and solutions
- Changed icon from ⚠ to 🔒 for visual clarity

### 🖥️ GPU Mode Switching Guidance
- **NEW**: Hardware limitation warning box in GPU Switching section
- Explains that GPU mode switching requires reboot
- Directs users to BIOS settings if software switching doesn't work
- Added tip explaining Hybrid vs Discrete mode benefits

### ⚡ GPU Power Boost Warning
- **NEW**: Warning that GPU power settings may reset after sleep/reboot
- Explains this is a BIOS behavior limitation on some OMEN models
- Users know OmenCore will attempt to restore settings on startup

---

## 🐛 Bug Fixes

### 🚀 Startup Issues ([GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6))
- **Fixed**: OmenCore not starting with Windows even when startup enabled
  - Root cause: Registry Run key doesn't elevate apps, hardware access requires admin
  - Solution: Uses Task Scheduler with `HIGHEST` privileges for proper elevation
  - Creates task: `schtasks /create /tn "OmenCore" /tr "path" /sc onlogon /rl highest`
  - Also adds registry fallback for systems where Task Scheduler fails

### 🌡️ CPU Temperature Showing 0°C ([GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6))
- **Fixed**: CPU temp stuck at 0°C after reboot on AMD Ryzen 8940HX and similar CPUs
- **Added**: More AMD sensor name fallbacks:
  - `CPU (Tctl/Tdie)` - AMD Ryzen variant naming
  - `CCD1 (Tdie)`, `CCD 1 (Tdie)` - CCD-specific sensors
  - `SoC`, `Socket` - APU/SoC temperature fallbacks
- **Added**: Auto-reinitialize mechanism when consecutive 0°C readings detected
  - After 5 consecutive zero readings, hardware monitor reinitializes automatically
  - Helps recover from sensor stale state after system resume
- **Added**: Force hardware update when initial sensor read returns 0
- **Added**: "Any temperature above 10°C" fallback to catch edge cases
- **Improved**: Detailed logging of available sensors when detection fails

### 🌀 Auto Fan Mode Not Ramping Properly ([GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6))
- **Clarified**: "Auto" mode uses BIOS default thermal policy, which may be too conservative on some models
- **Added**: "Gaming" preset that uses Performance thermal policy (0x31) for aggressive fan response
- **Recommendation**: Users experiencing inadequate fan ramping should use "Gaming" or "Max" preset

### ⚡ Dynamic Boost Resetting ([GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6))
- **Improved**: GPU Power Boost level now persisted to config
- **Added**: Config property `LastGpuPowerBoostLevel`
- **Note**: Some OMEN models reset GPU power settings at BIOS level (EC behavior)
- **Added**: UI warning explaining this limitation

### 🖥️ GPU Mode Switching via Software ([GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6))
- **Clarified**: GPU mode switching is a BIOS-level operation requiring reboot
- **Added**: Hardware limitation warning in UI
- **Added**: Guidance to use BIOS settings directly if software switching fails
- **Note**: Some OMEN models only support GPU switching via BIOS menu

---

## 📝 Technical Details

### New Config Properties
```json
{
  "LastGpuPowerBoostLevel": "Maximum",
  "LastFanPresetName": "Gaming"
}
```

### New Fan Mode
- Added `FanMode.Performance` (uses HP WMI thermal policy 0x31)
- Added `FanMode.Quiet` (explicit quiet mode enum value)

### Task Scheduler Integration
- Task name: `OmenCore`
- Trigger: On logon
- Run level: Highest (admin)
- Fallback: `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`

### LibreHardwareMonitor Improvements
- `Reinitialize()` method to reset hardware monitor state
- Auto-reinit after 5 consecutive 0°C CPU temp readings
- Extended AMD sensor fallback chain (15+ patterns)
- Force `hardware.Update()` retry on zero readings

### Files Modified
- `ViewModels/SettingsViewModel.cs` - Task Scheduler startup implementation
- `ViewModels/SystemControlViewModel.cs` - GPU Power Boost persistence
- `ViewModels/FanControlViewModel.cs` - Gaming preset and mode commands
- `Hardware/LibreHardwareMonitorImpl.cs` - AMD sensor fixes, auto-reinit
- `Models/AppConfig.cs` - New persistence properties
- `Models/FanMode.cs` - Performance and Quiet enum values
- `Views/FanControlView.xaml` - Curve editor UX improvements
- `Views/SystemControlView.xaml` - GPU mode and power boost warnings
- `Views/MainWindow.xaml` - Improved Secure Boot banner

---

## 📥 Installation

### Upgrade from v1.1.1
1. Close OmenCore if running
2. Download `OmenCoreSetup-1.1.2.exe` or extract portable zip
3. Run installer or extract over existing installation
4. **Important**: If startup wasn't working before:
   - Disable and re-enable "Start with Windows" in Settings
   - This creates the new scheduled task with proper elevation

### Fresh Install
1. Download `OmenCoreSetup-1.1.2.exe` from releases
2. Run installer
3. Enable "Start with Windows" in Settings tab
4. Grant Administrator privileges when prompted

---

## ⚠️ Known Model-Specific Limitations

### HP Omen 16-n0123AX (Ryzen 7 6800H + RTX 3070 Ti)
- **Auto mode**: May not ramp fans aggressively enough for gaming
  - **Workaround**: Use "Gaming" or "Max" preset while gaming
- **GPU mode switching**: May only work via BIOS on this model
- **Dynamic Boost**: May reset after reboot (BIOS behavior)

### Ryzen 8940HX / Hawk Point Systems
- **CPU temperature**: Fixed in this release with expanded sensor detection
- If still showing 0°C, check logs at `%LOCALAPPDATA%\OmenCore\` for sensor names

### General Notes
- **Auto fan mode**: Uses BIOS default thermal policy. Behavior varies by model.
- **GPU Power Boost**: Saved to config but may reset at hardware level on sleep/reboot.
- **GPU switching**: Requires reboot. Some models only support BIOS-based switching.

---

## 📊 Changelog Summary

| Category | Changes |
|----------|---------|
| New Features | 3 |
| UX Improvements | 5 |
| Bug Fixes | 5 |
| Sensor Fallbacks | 8+ |

---

## 💬 Community Feedback

This release directly addresses issues reported in:
- [GitHub Issue #6](https://github.com/theantipopau/omencore/issues/6) - HP Omen 16 (Ryzen 6800H) compatibility
- Community reports of startup issues
- CPU temperature detection problems on newer AMD CPUs

Thank you to all users who reported issues with detailed system information!

---

## 🔗 Links

- **Subreddit**: [r/Omencore](https://reddit.com/r/Omencore)
- **GitHub Issues**: [Report bugs](https://github.com/theantipopau/omencore/issues)
- **Discussions**: [GitHub Discussions](https://github.com/theantipopau/omencore/discussions)
- **Website**: [omencore.info](https://omencore.info)

---

*Thank you to the community for detailed bug reports and system information. Your feedback helps make OmenCore compatible with more OMEN laptops!*
