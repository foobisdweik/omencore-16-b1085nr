# OmenCore v1.1.1 Changelog

## 🚀 OmenCore v1.1.1 - Universal OMEN Support, Game Profiles, Desktop Detection & More

**Release Date**: December 13, 2025  
**Download**: [GitHub Releases](https://github.com/theantipopau/omencore/releases/tag/v1.1.1)

---

## ✨ New Features

### 🔥 AMD Ryzen Undervolting Support
- **NEW**: Full AMD Ryzen Curve Optimizer undervolting support!
- Auto-detects Intel vs AMD CPU and uses appropriate provider
- Supports all modern Ryzen families:
  - Renoir/Lucienne (4000 series)
  - Cezanne/Barcelo (5000 series)
  - Rembrandt (6000 series)
  - Phoenix (7040 series)
  - Hawk Point (8040 series)
  - **Strix Point** (Ryzen AI 300 - GitHub Issue #5!)
  - Strix Halo (Ryzen AI MAX)
  - Fire Range (HX series)
- Uses SMU (System Management Unit) communication via PawnIO
- iGPU Curve Optimizer support for supported APUs
- Log shows: `CPU undervolt provider: AMD Ryzen (StrixPoint) - PawnIO (SMU)`

### 🎧 Direct HID Peripheral Access (No iCUE/G HUB Required)
- **NEW**: Corsair devices detected via direct USB HID - iCUE no longer required!
- **NEW**: Logitech devices detected via direct USB HID - G HUB no longer required!
- **NEW**: HidSharp library integration for hardware-level device communication
- Priority order: Direct HID → Vendor SDK → Stub fallback
- Peripherals now work completely independently of vendor software

### 🌡️ CPU Temperature Limit (TCC Offset)
- **NEW**: Limit maximum CPU temperature using Intel TCC (Thermal Control Circuit) Offset
- Set a temperature ceiling (e.g., 85°C) to reduce heat and fan noise
- Slider in System Control → CPU Temperature Limit section
- Reads TjMax from CPU to show actual temperature limits
- Useful for users who want quieter operation at the cost of some performance
- Uses MSR 0x1A2 (IA32_TEMPERATURE_TARGET) - Intel CPUs only

### 🖥️ Desktop PC Detection
- **NEW**: Automatic detection of OMEN desktop PCs (25L/30L/40L/45L)
- Shows warning banner for desktop users about limited EC support
- Detects chassis type via Win32_SystemEnclosure WMI
- `IsDesktop` and `IsLaptop` properties in DeviceCapabilities
- Better messaging for users with desktop OMEN systems

### 🔐 PawnIO Support (Secure Boot Compatible EC Access)
- **NEW**: Full PawnIO driver integration for EC access with Secure Boot enabled!
- Automatically detects PawnIO installation and uses it for fan/thermal control
- Falls back to WinRing0 if PawnIO not installed and Secure Boot disabled
- Uses `LpcACPIEC` module for ACPI Embedded Controller access
- No need to disable Secure Boot for full fan control anymore!
- Install PawnIO from https://pawnio.eu/ for Secure Boot compatible operation
- EC Backend displayed in status (PawnIO, WinRing0, or None)

### 📦 Bundled PawnIO Modules (Self-Contained Operation)
- **NEW**: All essential PawnIO modules now bundled with OmenCore
- No need to manually copy `.bin` or `.amx` files from PawnIO installation
- Included modules:
  - `PawnIOLib.dll` - Core PawnIO library
  - `LpcACPIEC.bin` - ACPI Embedded Controller access (fan control)
  - `IntelMSR.bin` - Intel MSR access (TCC offset, undervolt)
  - `AMDFamily17.bin`, `AMDFamily10.bin`, `AMDFamily0F.bin` - AMD CPU support
  - `RyzenSMU.bin` - Ryzen SMU access
  - `AMDReset.bin` - AMD reset functionality
  - `IsaBridgeEC.bin` - ISA bridge EC access (alternative EC method)
- Users only need to install base PawnIO driver - OmenCore provides everything else

### 🔧 Improved Installer with PawnIO Option
- **NEW**: Installer now offers to install PawnIO driver during setup
- Optional checkbox: "Install PawnIO driver (recommended for Secure Boot)"
- Runs PawnIO installer silently if selected
- Skips if PawnIO already installed
- Single installer covers both WinRing0 (legacy) and PawnIO (modern) systems

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
- **BACKEND**: HP Client Management Script Library integration service
- Check for available BIOS SoftPaqs from HP's servers
- Download and launch official HP BIOS installers
- One-click CMSL module installation for PowerShell
- **Note**: UI uses direct HTTP-based BIOS check. CMSL backend available for future PowerShell-based workflows.

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

## ⚡ Performance & Quality Improvements

### Startup Performance
- **Lazy Loading ViewModels**: FanControl, SystemControl, Dashboard, Settings now load on-demand
- **Batched CMSL Queries**: HP CMSL device info now fetched in single PowerShell call (5-10s → 1-2s)
- **Optimistic OGH Detection**: OGH proxy validates interface without speculative commands
- **Capability-Based Factory**: Fan controller uses pre-detected capabilities instead of re-probing

### UI Enhancements
- **Fan Backend Indicator**: Sidebar shows active fan control backend (OGH Proxy, WMI BIOS, EC Direct, etc.)
- **Secure Boot Warning**: Dismissable banner when Secure Boot limits functionality
- **Desktop/Capability Warning**: Warning banner now displays for desktop PCs, fan-unavailable mode, etc.
- **Hotkey OSD Popup**: On-screen display shows mode changes when using keyboard shortcuts
  - Stylish overlay appears in bottom-right corner
  - Shows mode icon, category, and hotkey used
  - Auto-fades after 2 seconds with smooth animation
  - Color-coded accent by mode (orange=Performance, teal=Quiet, cyan=Balanced)
- **Profile Validation**: Real-time validation feedback in Game Profile Manager
- **Smooth Scrolling**: `SmoothScrollViewer` style for pixel-based smooth scrolling throughout the app
  - Applied to all major views: DashboardView, SettingsView, LightingView, SystemControlView, MainWindow sidebar
  - Reduced scrollbar opacity with hover-fade effect for cleaner appearance
  - Eliminated "chunky" item-based scrolling that felt slow and unintuitive
- **SystemControlView Scrolling**: Long content (Undervolt, TCC, GPU Mode, Cleanup sections) now scrolls correctly
- **Visual Consistency**: Brand images, typography hierarchy, and color palette consistency across all UI components

---

## 🐛 Bug Fixes

### Critical Fixes
- **Fixed**: Process handle leak in `ProcessMonitoringService` - was leaking ~200 handles every 2 seconds
- **Fixed**: OGH Error Code 2 spam - proxy now validates commands before claiming availability
- **Fixed**: Slow startup - ViewModels now lazy-load on first tab access instead of all at once
- **Fixed**: Secure Boot EC access - Added PawnIO support as alternative to blocked WinRing0
- **Fixed**: OGH cleanup hanging indefinitely - Added command timeouts (2-3 min per command)
- **Fixed**: "Access to the path 'Aga.Controls.dll' is denied" during LibreHardwareMonitor auto-download

### HP WMI BIOS Fan Control - Complete Rewrite
- **Fixed**: Fan control not working on HP OMEN laptops (OGH commands returning error code 2)
- **Rewritten**: HpWmiBios.cs now matches OmenMon's exact CIM implementation
- Switched from System.Management to **Microsoft.Management.Infrastructure** (modern CIM API)
- Fixed SetFanMode to use correct byte format: `{0xFF, mode, 0x00, 0x00}`
- Added proper BIOS signature: `"SECU"` (0x53, 0x45, 0x43, 0x55)
- Corrected all command type IDs to match OmenMon exactly
- New `BiosCmd` enum: Default=0x20008, Keyboard=0x20009, Legacy=0x00001, GpuMode=0x00002
- Fan modes now working: `✓ Fan mode set to: Performance (0x31)`

### Fan RPM & Control Fixes
- **Fixed**: Fan real-time status showing 0% RPM on HP OMEN laptops
  - HP OMEN motherboards (HP 8BAD) use proprietary EC, not exposed via standard Super I/O
  - Now uses `HpWmiBios.GetFanLevel()` which returns krpm (e.g., 28 = 2800 RPM)
  - Falls back to HP WMI when LibreHardwareMonitor can't detect fan sensors
- **Fixed**: "Max" fan preset not having any audible/physical effect
  - Thermal policies (Performance/Cool/Default) only affect temperature response curves
  - "Max" preset now calls `SetFanMax(true)` to force 100% fan speed immediately
  - Non-max presets properly disable forced max mode via `SetFanMax(false)`

### System Tray Improvements
- **Fixed**: System tray showing different performance mode than the app
  - Tray menu now syncs correctly with app on startup and mode changes
  - Force-loads Dashboard/SystemControl before subscribing to ensure correct initial values
- **Fixed**: Tray tooltip showing version 1.1.0 instead of 1.1.1
- **Improved**: Completely redesigned tray context menu with modern styling
  - Gradient dark background with subtle depth
  - Color-coded temperature displays (red for CPU, cyan for GPU)
  - Styled control items with accent colors (OMEN red, cyan)
  - Submenu items now show descriptions
  - Better visual hierarchy with styled separators
  - Version displayed in header

### WMI BIOS Heartbeat
- **NEW**: Added WMI BIOS heartbeat mechanism for 2023+ OMEN models (13th gen Intel+)
  - Periodic WMI queries keep fan control unlocked on newer HP systems
  - Automatic initialization sequence tries multiple command formats
  - 60-second heartbeat interval maintains BIOS command responsiveness
- **Improved**: OGH cleanup now shows real-time progress with step-by-step status updates
- **Improved**: EcAccessFactory - Automatic backend selection (PawnIO → WinRing0 → None)
- **Improved**: Capability detection now checks for PawnIO installation
- **Improved**: Better diagnostic messages when OGH proxy commands fail
- **Hidden**: RGB & Peripherals tab when no Corsair/Logitech devices found (stub SDK cleanup)

### User-Reported Issues ([GitHub Issue #4](https://github.com/theantipopau/omencore/issues/4))
- **Fixed**: Settings not saving on restart - Config folder path was inconsistent between UI and actual storage location
- **Fixed**: Performance mode not persisting after restart - Now saves last selected performance mode to config and restores on startup
- **Fixed**: Hotkey toggle state not saving - Added `HotkeysEnabled` to config, now persists across restarts
- **Fixed**: Notification settings not saving - All notification toggles now persist to config
- **Fixed**: WinRing0 driver not detected - Now tries multiple device paths (`WinRing0_1_2_0`, `WinRing0_1_2`, `WinRing0`)
- **Fixed**: Intel XTU service conflict - Added detection and specific instructions to disable XTU before undervolting
- **Fixed**: WMI BIOS interface not detected - Added heartbeat mechanism and retry sequence for 2023+ OMEN models
- **Fixed**: HP BIOS information retrieval - Now handles HTML redirect responses and provides direct HP support page link
- **Fixed**: Log folder button opening wrong directory - Now correctly opens `AppData\Local\OmenCore`
- **Fixed**: Fan boost error messages now explain exactly why it failed and how to fix it:
  - Detects when OGH WMI commands return error code 2 (unsupported on model)
  - Provides clear instructions: "Install PawnIO + LpcACPIEC.amx module"
  - Changed backend priority: EC Access → WMI BIOS → OGH (OGH often claims "available" but commands fail)
- **Fixed**: Fan cleaning/boost now uses **multiple backends** when EC access is blocked:
  - Priority: EC Access (WinRing0/PawnIO) → HP WMI BIOS → OGH Proxy
  - Shows which backend is being used in progress messages
  - Provides model-specific guidance when all backends fail
- **Improved**: CPU undervolt now uses PawnIO MSR backend on Secure Boot systems (falls back to WinRing0)
- **Improved**: Corsair iCUE device detection - Now checks if iCUE software is running before SDK initialization
  - Shows helpful message if iCUE is not running
  - Reminds user to enable "SDK" option in iCUE Settings → General
- **Improved**: GPU mode detection - Better hybrid vs discrete detection with diagnostic logging
  - Uses active-display heuristics to avoid false "Hybrid" when iGPU is disabled/hidden
  - Now logs all detected GPUs (NVIDIA and Intel) for troubleshooting
  - Fixed false "Discrete only" detection on hybrid systems
- **Improved**: BIOS update checker now detects HTML responses (HP API redirects) and skips JSON parsing errors
- **Clarified**: HP OMEN keyboard lighting section now explains that per-zone control requires OMEN Gaming Hub
- **Fixed**: GPU Power Boost showing "Available" but failing with "no backend available" - Now properly detects when WMI GPU power commands don't work and disables the feature with clear explanation

### User-Reported Issues ([GitHub Issue #5](https://github.com/theantipopau/omencore/issues/5))
- **Fixed**: AMD Ryzen AI CPU temperature not showing on Omen 16 Max (HX 375, etc.)
  - Enhanced CPU temp sensor detection for AMD Phoenix/Strix Point processors
  - Added fallback sensors: "CPU", "Core Max", "Core Average", "CCDs Average", "Core #0"
- **Fixed**: Corsair device duplication - devices with multiple HID interfaces no longer appear multiple times
- **Fixed**: Logitech stub no longer returns fake devices when no real devices are connected

### Known Model-Specific Limitations
- **OMEN 17-ck2xxx (13th Gen Intel)**: OGH WMI fan commands return error code 2. Fan boost requires PawnIO with LpcACPIEC.amx module for direct EC access.
- **GPU Power Boost on 17-ck2xxx**: WMI GPU power commands not functional on this model series. GPU Power Boost control will show as "Not available" with explanation. This is a hardware/firmware limitation - HP's WMI BIOS interface exists but doesn't respond to GPU power commands.
- **Corsair iCUE SDK**: Enhanced diagnostics added - logs now show provider loading status, device count from both surface and provider, and detailed exception messages. Some device configurations may still not be detected due to iCUE SDK limitations.

### Corsair SDK Improvements
- **Enhanced Diagnostics**: Added detailed logging for SDK initialization
  - Shows provider load success/failure with specific error messages
  - Displays device count from both RGBSurface and provider
  - Lists each detected device with model, type, and manufacturer
  - Provides actionable troubleshooting steps when no devices found
- **Better Error Handling**: SDK load exceptions are now caught and logged instead of silently failing
- **Extended Enumeration Time**: Increased wait time for wireless device detection (1000ms)

### GPU Mode Switching Safety Fix
- **CRITICAL FIX**: Removed dangerous registry-based GPU mode switching that could corrupt GPU drivers
- **Fixed**: GPU mode switching on HP Transcend/Victus causing dGPU to become unresponsive ([reported issue](https://github.com/theantipopau/omencore/issues/4))
  - Previously, the app would modify low-level graphics driver registry keys which corrupted driver state
  - Users had to use Display Driver Uninstaller (DDU) to recover
- **Changed**: GPU mode switching now ONLY works via HP WMI BIOS on confirmed HP OMEN systems
- **Added**: System compatibility check before allowing GPU mode changes
- **Added**: Clear error messages explaining why GPU switching isn't available on non-OMEN HP systems
- **Note**: HP Transcend, Victus, and other non-OMEN HP laptops should use NVIDIA Control Panel or BIOS settings directly

### AMD Ryzen Support Improvements
- **Fixed**: AMD CPU temperature detection (Tctl/Tdie sensors)
- **Fixed**: AMD CPU power reporting (Package Power sensor)
- **Improved**: Better sensor name fallback chain for all CPU types
- **Added**: Debug logging for sensor detection issues

### Code Quality Improvements
- **Async Exception Handling** - All `async void` methods now have proper try/catch blocks
- **Memory Leak Prevention** - Fixed event handler memory leaks with proper unsubscription in Dispose()
- **Deadlock Prevention** - Changed all `Dispatcher.Invoke` to `BeginInvoke` to prevent UI deadlocks

---

## 📝 Technical Details

### New Files
- `Models/RyzenModels.cs` - RyzenFamily enum, RyzenUndervoltOffset, RyzenPowerLimits
- `Hardware/RyzenSmu.cs` - SMU communication via PawnIO PCI config access
- `Hardware/RyzenControl.cs` - CPU family detection and SMU address configuration
- `Hardware/AmdUndervoltProvider.cs` - ICpuUndervoltProvider for AMD Ryzen
- `TccOffsetStatus.cs` - Model for TCC offset status and limits
- `PawnIOEcAccess.cs` - PawnIO driver implementation of IEcAccess
- `EcAccessFactory.cs` - Factory for automatic EC backend selection
- `OghServiceProxy.cs` - OGH service detection and WMI proxy for 2023+ models
- `DeviceCapabilities.cs` - Runtime capability matrix (fan, thermal, GPU, undervolt, lighting methods)
- `CapabilityDetectionService.cs` - 10-phase capability detection at startup
- `HpCmslService.cs` - HP CMSL integration for safe BIOS updates via SoftPaq
- `HpWmiBios.cs` - HP WMI BIOS interface for fan/GPU control without WinRing0
- `WmiFanController.cs` - WMI-based fan controller implementation
- `FanControllerFactory.cs` - Intelligent backend selection (OGH → WMI → EC → Fallback)
- `IFanController.cs` - Common interface for fan control backends
- `BiosUpdateService.cs` - HP BIOS update checking
- `GameProfile.cs` - Game profile model with settings and analytics
- `GameProfileService.cs` - Profile persistence and management
- `ProcessMonitoringService.cs` - Game detection and process monitoring
- `GameProfileManagerView.xaml` - Profile manager UI
- `HotkeyOsdWindow.xaml/cs` - On-screen display for hotkey mode changes
- `drivers/` folder - Bundled PawnIO modules (PawnIOLib.dll, LpcACPIEC.bin, IntelMSR.bin, AMD*.bin, RyzenSMU.bin)

### Modified Files (v1.1.1 specific)
- `Hardware/CpuUndervoltProvider.cs` - Added CpuUndervoltProviderFactory for Intel/AMD auto-detection
- `Hardware/HpWmiBios.cs` - Complete rewrite using CIM (Microsoft.Management.Infrastructure)
- `Hardware/WmiFanController.cs` - Added HP WMI fan RPM reading, max fan speed support
- `ViewModels/MainViewModel.cs` - Uses factory to create undervolt provider, tray menu sync fix
- `OmenCoreApp.csproj` - Added Microsoft.Management.Infrastructure v3.0.0 package
- `App.xaml.cs` - Fixed LHM download to use unique temp folders
- `DeviceCapabilities.cs` - Added ChassisType enum, IsDesktop/IsLaptop properties
- `CapabilityDetectionService.cs` - Added chassis type detection via WMI
- `WinRing0MsrAccess.cs` - Added TCC offset read/write methods (MSR 0x1A2)
- `SystemControlViewModel.cs` - Added TCC offset support with commands
- `SystemControlView.xaml` - Added CPU Temperature Limit UI section
- `OmenGamingHubCleanupService.cs` - Added timeouts and progress events

### Desktop Support Notes
OMEN desktop PCs (25L/30L/40L/45L) have different EC register layouts than laptops. While OmenCore can detect these systems, fan control functionality may be limited. Desktop users are advised to:
1. Keep OMEN Gaming Hub installed for full fan control
2. Use OmenCore primarily for monitoring and game profiles
3. Report any working EC registers to help improve desktop support

---

## 📥 Installation

### Fresh Install
1. Download `OmenCoreSetup-1.1.1.exe` from releases
2. Run installer (optionally check "Install PawnIO driver" for Secure Boot systems)
3. Run OmenCore from Start Menu or Desktop shortcut
4. Grant Administrator privileges when prompted

### Portable Install
1. Download `OmenCore-v1.1.1-win-x64.zip` from releases
2. Extract to desired location
3. Run `OmenCore.exe` as Administrator
4. For Secure Boot systems, install PawnIO driver separately from https://pawnio.eu/

### Upgrade from v1.0.x
1. Close OmenCore if running
2. Extract new version over existing installation
3. Your settings and profiles are preserved

---

## ⚠️ Known Issues & Limitations

### GPU Mode Switching
- **HP Transcend, Victus, and non-OMEN HP laptops**: GPU mode switching is disabled to prevent driver corruption. Use NVIDIA Control Panel or BIOS settings directly.
- **OMEN laptops**: GPU mode switching only works if your BIOS has the "GPU Mode" or "Graphics Mode" setting exposed via HP WMI.

### Intel XTU Conflict
- If Intel Extreme Tuning Utility (XTU) is installed and running, OmenCore's undervolting will fail with "Failed to write MSR 0x152" error.
- **Solution**: Stop and disable the Intel XTU service in Windows Services (services.msc).

### 2023+ OMEN Models (13th Gen Intel and newer)
- Some features may require the heartbeat mechanism to work. If fan control stops working after ~2 minutes, ensure OmenCore is running continuously.
- If WMI BIOS commands fail, try keeping OMEN Gaming Hub installed as a fallback.

---

## 📊 Changelog Summary

| Category | Changes |
|----------|---------|
| New Features | 18+ |
| Performance | 4 |
| Bug Fixes | 30+ |
| UI Improvements | 8 |
| New Files | 22 |
| Bundled Modules | 9 |

---

## 💬 Community

- **Subreddit**: [r/Omencore](https://reddit.com/r/Omencore)
- **GitHub Issues**: [Report bugs](https://github.com/theantipopau/omencore/issues)
- **Discussions**: [GitHub Discussions](https://github.com/theantipopau/omencore/discussions)

---

*Thank you to everyone who reported bugs and suggested features! Your feedback makes OmenCore better for everyone.*
