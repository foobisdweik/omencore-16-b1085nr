# Changelog v2.0.0

All notable changes to OmenCore v2.0.0 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.0-beta] - 2026-01-01

### Added

#### 🎛️ System Optimizer
Complete Windows gaming optimization suite with one-click presets:
- **Power**: Ultimate Performance plan, GPU scheduling, Game Mode, foreground priority
- **Services**: Telemetry, SysMain/Superfetch, Search Indexing, DiagTrack toggles
- **Network**: TCP NoDelay, ACK frequency, Nagle algorithm, P2P updates
- **Input**: Mouse acceleration, Game DVR, Game Bar, fullscreen optimizations
- **Visual**: Transparency, animations, shadows, performance presets
- **Storage**: TRIM, last access timestamps, 8.3 names, SSD detection
- **Safety**: Registry backup and system restore point creation
- **Risk indicators** for each optimization (Low/Medium/High)

#### 🌈 RGB Provider Framework
Extensible multi-brand RGB control system:
- **`IRgbProvider` & `RgbManager`** - Unified provider model for cross-brand lighting
- **Corsair Provider** - iCUE integration + direct HID control (K70/K95/K100 keyboards, Dark Core RGB PRO mouse)
- **Logitech Provider** - G HUB integration with brightness (`color:#RRGGBB@<brightness>`) and breathing (`breathing:#RRGGBB@<speed>`) effects
- **Razer Provider** - Synapse detection (full Chroma SDK planned)
- **System Generic Provider** (experimental) - RGB.NET fallback for unsupported brands
- **"Apply to System"** action to sync colors across all connected RGB devices
- **Corsair HID-only mode** toggle for advanced users (Settings → Hardware)

#### 🖱️ Corsair Mouse DPI Control
Full DPI stage management via HID:
- 5-stage DPI profiles with per-stage configuration
- DPI profile save/load/delete with overwrite confirmation
- Stage names, angle snapping, lift-off distance settings
- Tooltips explaining each DPI feature
- Extensive HID protocol documentation ([HID_DPI_RESEARCH.md](HID_DPI_RESEARCH.md))

#### 🔧 Fan Control Enhancements
- **Extreme fan preset** (100% at 75°C for high-power systems)
- **Fan transition smoothing** - Ramped increments reduce abrupt speed changes
- **"Immediate Apply" option** - Low-latency for user-triggered changes
- **Force reapply command** - Manual re-application of fan presets

#### � Razer Chroma SDK
Full Chroma SDK integration via REST API:
- **Session management** with heartbeat timer (automatic keep-alive)
- **Effect types**: Static, Breathing, Spectrum Cycling, Wave, Reactive, Custom
- **Per-key RGB** for keyboard custom effects
- **Device type support**: Keyboard, Mouse, Mousepad, Headset, Keypad, ChromaLink
- **Event notifications** for effect changes and SDK connection status

#### 🎨 Lighting View Redesign
Complete overhaul of the RGB Lighting page:
- **Brand headers** with logos and connection badges
- **DeviceCard styles** with modern hover states
- **Logitech section**: Device list, per-device color picker, breathing effect controls
- **Razer section**: Chroma SDK status, effect picker, device enumeration
- **HP OMEN section**: Per-zone color picker, brightness, apply-on-startup toggle

#### 🔌 Unified RGB Engine
Enhanced cross-brand RGB synchronization:
- **IRgbProvider interface** expanded with ProviderId, IsConnected, DeviceCount, SupportedEffects
- **Unified effect methods**: SetStaticColorAsync, SetBreathingEffectAsync, SetSpectrumEffectAsync, TurnOffAsync
- **RgbEffectType enum** for consistent effect naming
- **RgbManager sync methods**: SyncStaticColorAsync, SyncBreathingEffectAsync, SyncSpectrumEffectAsync
- **Status tracking** with GetStatus() for provider health monitoring

#### 🐧 Linux CLI (Research/Prototype)
New OmenCore.Linux project for cross-platform support:
- **fan command**: `--mode auto|max`, `--custom <cpu%> <gpu%>`, `--status`
- **performance command**: `--mode balanced|performance|quiet`
- **keyboard command**: `--color #RRGGBB`, `--brightness 0-100`, `--zone 1-4`
- **status command**: Display all hardware info (temps, fans, mode)
- **monitor command**: Real-time monitoring with `--interval` option
- **LinuxEcController**: EC register access via `/sys/kernel/debug/ec/ec0/io`
- **LinuxHwMonController**: hwmon sensor integration for temps/fans
- **LinuxKeyboardController**: HP WMI keyboard RGB via sysfs

#### �🎨 UI Improvements
- **DarkContextMenu control** - Custom context menu with no white margins
- **Modern toggle switches** - iOS-style toggles replace checkboxes in Settings
- **GPU Voltage/Current Graph** - Added GPU V/C monitoring chart to dashboard
- **Per-Core Undervolt** - Individual undervolt controls for each CPU core
- **Keyboard Lighting Diagnostics** - Device detection, test patterns, log collection
- **Keyboard RGB Status Hints** - Contextual tips for troubleshooting
- **Sidebar width increased** from 200px to 230px for better readability

### Changed

#### 📈 Performance Improvements
- **LoadChart rendering** improved from 10 FPS to 20 FPS
  - Reduced render throttling from 100ms to 50ms intervals
  - Polyline reuse to avoid object recreation overhead
  - BitmapCache for better visual performance
- **Fan Curve Editor UX** - Improved point dragging
  - Increased point size from 16px to 20px (26px on hover)
  - Glowing highlight effect on hover
  - Increased line thickness for better visibility

#### 🔄 Architecture
- **TrayIconService refactored** to use DarkContextMenu
- **MainViewModel** initializes RgbManager and registers providers at startup
- **Corsair SDK** respects `CorsairDisableIcueFallback` config flag

### Fixed

#### 🐞 Critical Fixes
- **Fan Preset Restoration on Startup** - `SettingsRestorationService` was never called in production
  - Added `RestoreSettingsOnStartupAsync()` to MainViewModel with retry logic
  - Fan presets and GPU Power Boost now properly restored after reboot
- **Auto-Start --minimized Flag** - Command line args weren't being processed
  - Now properly checks `--minimized`, `-m`, `/minimized` flags
  - Added command line argument logging for debugging
- **Corsair HID Stack Overflow** - Fixed recursion bug in `BuildSetColorReport`
  - Method was calling itself instead of using product-specific switch

#### 🔧 System Tray Fixes
- **System Tray Crash** - Fixed crash when right-clicking tray icon
  - Bad ControlTemplate tried to put Popup inside Border
- **Tray Icon Update Crash** - Fixed "Specified element is already the logical child" error
  - Now creates fresh UI elements instead of reusing existing ones
- **Right-click Context Menu** - Fixed menu not appearing
  - Applied dark theme styling (dark background, white text, OMEN red accents)
  - Temperature display and color changes restored

#### ⚙️ Other Fixes
- **Auto-Start Admin Error Feedback** - Better error handling for Task Scheduler
  - Shows clear error when attempting to enable auto-start without admin privileges
- **Platform compatibility warnings** - Added `[SupportedOSPlatform("windows")]` attributes
  - Fixed 57 compilation warnings for Windows-only APIs
- **Keyboard color reset on startup** - Fixed unexpected auto-set to red
- **Quick profile state mismatch** - Fixed panel vs active profile mismatch
- **Fan-profile transition latency** - Improved switching speed
- **CPU temperature monitoring** - Better smoothing and sensor selection
- **Test Parallelization Conflicts** - Fixed 6 flaky tests with collection isolation
- **Code Quality** - Fixed 26+ analyzer warnings (IDE0052, IDE0059, IDE0060)
- **Duplicate UI Elements** - Fixed 9x repeated "Apply Colors on Startup" toggle in Lighting view
- **Corsair Mouse HID Support** - Added 20+ additional mouse PIDs for color/DPI control
  - Dark Core RGB PRO (0x1BF0), M65 family, Ironclaw, Nightsword, Harpoon, Sabre, Katar, Scimitar

### Technical

#### 🧪 Testing
- **66 tests passing** (up from 24)
- DPI profile tests with save/load/overwrite scenarios
- Corsair mouse PID payload tests
- RGB provider unit tests (Corsair, Logitech, RgbNetSystem)
- Test collection isolation for config-sensitive tests

#### 📦 Build System
- Clean compilation (0 warnings, 0 errors)
- Removed obsolete methods: `CreateCpuTempIcon`, `TryAlternativeWmiWatcher`, `ApplyViaEc`
- Modernized C# syntax (target-typed new, pattern matching, switch expressions)
- Logging improvements with fallback files and `OMENCORE_DISABLE_FILE_LOG` toggle

---

## Development Progress

### Phase 1: Foundation & Quick Wins ✅
- [x] System Tray Overhaul (context menu, dark theme, icons)
- [x] Settings View Improvements (toggle switches instead of checkboxes)
- [x] Tray Icon Update Crash fixes (WPF logical parent issues)

### Phase 2: System Optimizer ✅
- [x] Core Infrastructure (services, backup, verification)
- [x] Power Optimizations (Ultimate Performance, GPU scheduling, Game Mode)
- [x] Service Optimizations (telemetry, SysMain, search indexing)
- [x] Network Optimizations (TCP settings, Nagle algorithm)
- [x] Input & Graphics (mouse acceleration, Game DVR, fullscreen opts)
- [x] Visual Effects (animations, transparency, presets)
- [x] Storage Optimizations (TRIM, 8.3 names, SSD detection)
- [x] UI Implementation (tabs, toggles, risk indicators, presets)

### Phase 3: RGB Overhaul ✅
- [ ] Asset Preparation (device images, brand logos)
- [x] Enhanced Corsair SDK (iCUE 4.0, full device enumeration)
- [x] Corsair HID direct control (K70/K95/K100 keyboards, Dark Core RGB PRO mouse)
- [x] Corsair Mouse DPI Control (5-stage profiles with full HID)
- [x] Full Razer Chroma SDK (REST API, effects: static, breathing, spectrum, wave, reactive, custom)
- [x] Enhanced Logitech SDK (G HUB integration, direct HID)
- [x] Unified RGB Engine (enhanced IRgbProvider, sync all, cross-brand effects)
- [x] Lighting View Redesign (brand headers, DeviceCard styles, connection badges)

### Phase 4: Linux Support (Research Complete)
- [x] OmenCore.Linux CLI project created
- [x] Fan control command (--mode, --custom, --status)
- [x] Performance mode command (--mode balanced/performance/quiet)
- [x] Keyboard RGB command (--color, --brightness, --zone)
- [x] Status command (display all hardware info)
- [x] Monitor command (real-time temps/fans/power)
- [x] EC register controller (via /sys/kernel/debug/ec)
- [x] hwmon sensor integration
- [x] HP WMI keyboard controller

### Phase 4: Linux Support (Planned)
- [ ] Linux CLI (EC access, fan control, keyboard lighting)
- [ ] Linux Daemon (systemd service, automatic curves)
- [ ] Distro Testing (Ubuntu, Fedora, Arch, Pop!_OS)

### Phase 5: Advanced Features (Planned)
- [ ] OSD Overlay (RTSS integration, customizable metrics)
- [ ] Game Profiles (process detection, per-game settings)
- [ ] GPU Overclocking (NVAPI, core/memory offsets)
- [ ] CPU Overclocking (PL1/PL2, turbo duration)

### Phase 6: Polish & Release (Planned)
- [ ] Linux GUI (Avalonia UI port)
- [ ] Bloatware Manager (enumeration, safe removal)
- [ ] Final Testing (regression, performance, documentation)

**Overall Progress: 54/114 tasks (47%)**

---

## Current Status

- **Branch:** `v2.0-dev`
- **Build:** ✅ Succeeded (0 warnings, 0 errors)
- **Tests:** ✅ 66/66 passing
- **Next Release:** v2.0.0 (when RGB overhaul is complete)
