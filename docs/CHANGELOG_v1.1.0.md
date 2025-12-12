# OmenCore v1.1.0 Changelog

## 🚀 OmenCore v1.1.0 - Universal OMEN Support, Game Profiles, AMD Fixes & GPU Power Boost

**Release Date**: December 2024  
**Download**: [GitHub Releases](https://github.com/theantipopau/omencore/releases/tag/v1.1.0)

---

## ✨ New Features

### 🔌 OGH Service Proxy (2023+ Model Support)
- **NEW**: OMEN Gaming Hub service proxy for 2023+ laptops with Secure Boot
- Automatically detects OGH services (HPOmenCap, OmenCommandCenterBackground, etc.)
- Uses `hpCpsPubGetSetCommand` WMI interface when OGH is running
- Seamless fallback: OGH Proxy → WMI BIOS → EC Access → Monitoring-only
- Fan control works even with Secure Boot enabled!

### 🧠 Capability-Based Provider Architecture
- **NEW**: Runtime capability detection for universal OMEN model support
- 10-phase detection: device ID, security status, OGH, drivers, WMI BIOS, fan control, thermal, GPU, undervolt, lighting
- Automatically selects best available backend for each feature
- `DeviceCapabilities` model exposes what your specific laptop supports
- Future-proof architecture for new HP models

### 📦 HP CMSL BIOS Update Integration
- **NEW**: Safe BIOS updates via HP Client Management Script Library
- Check for available BIOS SoftPaqs from HP's servers
- Download and launch official HP BIOS installers
- One-click CMSL module installation for PowerShell
- No manual BIOS hunting required!

### 🎮 Game Profile System (Auto-Switching)
- **NEW**: Create per-game profiles with custom fan, performance, undervolt, GPU, and lighting settings
- Auto-switch profiles when games launch - no manual adjustments needed
- Restore default settings when games exit
- Track launch count and playtime per game
- Import/Export profiles to share with the community
- Modern dark-themed profile manager UI

### 🔥 WMI-Based Fan Control (No Driver Required!)
- **NEW**: HP WMI BIOS interface for fan control - no WinRing0 driver needed!
- Automatic backend selection: WMI BIOS → EC (WinRing0) → Monitoring-only
- Better AMD Ryzen laptop support without driver hassles
- Set fan modes: Default, Performance, Cool via BIOS

### ⚡ GPU Power Boost Control (+15W Dynamic Boost)
- **NEW**: Control GPU TGP and Dynamic Boost (PPAB) directly
- Three power levels: Minimum (base TGP), Medium (Custom TGP), Maximum (+15W boost)
- Same feature as Omen Gaming Hub's "GPU Power" slider
- Located in System Control → GPU Power Boost

### 🔧 BIOS Update Checker
- Check for HP BIOS updates directly from OmenCore
- View current BIOS version and date
- Quick link to HP Support page for your specific model

### 📤 Fan Profile Import/Export
- Export your custom fan curves to JSON files
- Import fan profiles from other OmenCore users
- Share your optimized fan configurations with the community

### 📊 Enhanced GPU Monitoring
- **GPU Power Draw** - Real-time wattage consumption
- **GPU Core Clock** - Current GPU frequency
- **GPU Memory Clock** - VRAM clock speed
- **GPU VRAM Total** - Total video memory
- **GPU Fan Speed** - Fan percentage
- **GPU Hotspot Temperature** - Junction/hotspot temp (if available)

### 🔔 Advanced Thermal Alerts
- Configurable CPU/GPU warning thresholds
- Critical temperature notifications
- SSD temperature monitoring
- Alert cooldown to prevent notification spam
- Driver issue notifications

### 💻 HP Victus Support
- Full support for HP Victus gaming laptops
- Same fan control and monitoring features as OMEN

### 🎨 UI Polish
- Unified dark theme across all windows (Game Profile Manager now matches main app)
- Custom window chrome with consistent title bars
- Improved About window with updated feature descriptions
- Better tooltips throughout the application

---

## � Performance & Quality Improvements

### Startup Performance
- **Lazy Loading ViewModels**: FanControl, SystemControl, Dashboard, Settings now load on-demand
- **Batched CMSL Queries**: HP CMSL device info now fetched in single PowerShell call (5-10s → 1-2s)
- **Optimistic OGH Detection**: OGH proxy validates interface without speculative commands
- **Capability-Based Factory**: Fan controller uses pre-detected capabilities instead of re-probing

### UI Enhancements
- **Fan Backend Indicator**: Sidebar shows active fan control backend (OGH Proxy, WMI BIOS, EC Direct, etc.)
- **Secure Boot Warning**: Dismissable banner when Secure Boot limits functionality
- **Hotkey OSD Popup**: On-screen display shows mode changes when using keyboard shortcuts
  - Stylish overlay appears in bottom-right corner
  - Shows mode icon, category, and hotkey used
  - Auto-fades after 2 seconds with smooth animation
  - Color-coded accent by mode (orange=Performance, teal=Quiet, cyan=Balanced)
- **Profile Validation**: Real-time validation feedback in Game Profile Manager
  - Profile name required (max 100 chars)
  - Executable must end with .exe
  - Undervolt range validation (-200 to 0 mV)
  - Priority range validation (0-100)

---

## �🐛 Bug Fixes
### Critical Fixes (December 2025)
- **Fixed**: Process handle leak in `ProcessMonitoringService` - was leaking ~200 handles every 2 seconds
- **Fixed**: OGH Error Code 2 spam - proxy now validates commands before claiming availability
- **Fixed**: Slow startup - ViewModels now lazy-load on first tab access instead of all at once
- **Hidden**: RGB & Peripherals tab when no Corsair/Logitech devices found (stub SDK cleanup)
### AMD Ryzen Support Improvements
- **Fixed**: AMD CPU temperature detection (Tctl/Tdie sensors)
- **Fixed**: AMD CPU power reporting (Package Power sensor)
- **Improved**: Better sensor name fallback chain for all CPU types
- **Added**: Debug logging for sensor detection issues

### Code Quality Improvements
1. **Async Exception Handling** - All `async void` methods now have proper try/catch blocks
2. **Memory Leak Prevention** - Fixed event handler memory leaks with proper unsubscription in Dispose()
3. **Deadlock Prevention** - Changed all `Dispatcher.Invoke` to `BeginInvoke` to prevent UI deadlocks

---

## 📝 Technical Details

### New Files (OGH & Capability Architecture)
- `OghServiceProxy.cs` - OGH service detection and WMI proxy for 2023+ models
- `DeviceCapabilities.cs` - Runtime capability matrix (fan, thermal, GPU, undervolt, lighting methods)
- `IHardwareProvider.cs` - Provider interfaces for modular hardware access
- `CapabilityDetectionService.cs` - 10-phase capability detection at startup
- `HpCmslService.cs` - HP CMSL integration for safe BIOS updates via SoftPaq

### New Files (WMI Support)
- `HpWmiBios.cs` - HP WMI BIOS interface for fan/GPU control without WinRing0
- `WmiFanController.cs` - WMI-based fan controller implementation
- `FanControllerFactory.cs` - Intelligent backend selection (OGH → WMI → EC → Fallback)
- `IFanController.cs` - Common interface for fan control backends
- `BiosUpdateService.cs` - HP BIOS update checking
- `ThermalMonitoringService.cs` - Thermal threshold monitoring

### New Files (Game Profiles)
- `GameProfile.cs` - Game profile model with settings and analytics
- `GameProfileService.cs` - Profile persistence and management
- `ProcessMonitoringService.cs` - Game detection and process monitoring
- `GameProfileManagerView.xaml` - Profile manager UI
- `GameProfileManagerViewModel.cs` - Profile manager MVVM logic

### New Files (UI)
- `HotkeyOsdWindow.xaml/cs` - On-screen display for hotkey mode changes
- `NullToVisibilityConverter.cs` - Converter to hide UI elements when data is null

### Files Modified
- `MainViewModel.cs` - Lazy loading ViewModels, event unsubscription, WMI integration, Dispatcher fixes
- `ProcessMonitoringService.cs` - Fixed Process handle leak (disposing untracked processes)
- `OghServiceProxy.cs` - Fixed OGH command validation to prevent error spam
- `FanService.cs` - Updated to use IFanController interface
- `PerformanceModeService.cs` - Updated to use IFanController interface  
- `SystemControlViewModel.cs` - Added GPU Power Boost UI support
- `SystemControlView.xaml` - Added GPU Power Boost section
- `LibreHardwareMonitorImpl.cs` - AMD CPU temp detection, GPU metric collection
- `DashboardViewModel.cs` - Dispatcher fixes
- `SettingsViewModel.cs` - Dispatcher fixes
- `GameProfileManagerViewModel.cs` - Exception handling
- `FanControlViewModel.cs` - Import/Export commands
- `TrayIconService.cs` - Dispatcher fixes
- `MonitoringSample.cs` - Enhanced GPU metrics
- `NotificationService.cs` - New notification types

---

## 📥 Installation

### Fresh Install
1. Download `OmenCoreSetup-1.1.0.exe` from [Releases](https://github.com/theantipopau/omencore/releases/tag/v1.1.0)
2. Run the installer (requires Administrator)
3. Launch OmenCore from the Start Menu

### Upgrade from v1.0.x
1. Close OmenCore if running
2. Run the new installer - it will upgrade in place
3. Your settings and profiles are preserved

---

## 🐛 Bug Fixes
- Fixed potential deadlocks when updating UI from background threads
- Fixed resource leaks in WinRing0 driver access
- Fixed event handler memory leaks in MainViewModel
- Fixed async void methods that could crash silently

---

## 📊 Changelog Summary

| Category | Changes |
|----------|---------|
| New Features | 9 |
| Performance | 4 |
| Bug Fixes | 12 |
| UI Polish | 6 |
| New Files | 18 |

---

## 💬 Community

- **Subreddit**: [r/omencore](https://reddit.com/r/omencore)
- **GitHub Issues**: [Report bugs](https://github.com/theantipopau/omencore/issues)
- **Discussions**: [GitHub Discussions](https://github.com/theantipopau/omencore/discussions)

---

*Thank you to everyone who reported bugs and suggested features! Your feedback makes OmenCore better for everyone.*
