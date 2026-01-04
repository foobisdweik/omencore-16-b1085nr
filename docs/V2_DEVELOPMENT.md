# OmenCore v2.0 Development Tracker

**Branch:** `v2.0-dev`  
**Started:** December 18, 2025  
**Current Version:** v2.0.1-beta  
**Target:** Q2 2026

---

## 📋 Development Checklist

### Phase 1: Foundation & Quick Wins (Alpha 1 - Jan 2026)

**Priority: UI/UX fixes that don't break functionality**

#### 1.1 System Tray Overhaul ✅
- [x] Create custom context menu control (no white margins)
- [x] Dark theme context menu with proper styling
- [x] Icons visible at all times (not just on hover)
- [x] Smooth hover animations
- [x] Temperature display improvements
- [x] Compact mode option
- [x] **Fix tray icon update crashes** (WPF logical parent issues)
- [x] **Fix right-click context menu not showing** (temporarily using regular ContextMenu with dark theme)

#### 1.2 Dashboard Polish ✅
- [x] Card-based layout with consistent shadows
- [x] Improve LoadChart rendering smoothness
- [x] Status badges with clear iconography
- [x] Quick action button hover states
- [x] Better spacing and alignment

#### 1.3 Settings View Improvements ✅
- [x] Toggle switches instead of checkboxes
- [ ] Grouped sections with headers
- [ ] Inline help text for complex options
- [ ] Better organization

#### 1.4 Typography & Visual Consistency ✅
- [x] Consistent font weights throughout
- [x] Monospace font for numeric values
- [x] Better contrast ratios
- [x] Consistent color usage for status indicators

---

### Phase 2: System Optimizer (Alpha 2 - Feb 2026) ✅

**Priority: High-impact feature users are asking for**

#### 2.1 Core Infrastructure ✅
- [x] Create `Services/SystemOptimizer/` folder structure
- [x] `SystemOptimizerService.cs` - main orchestration
- [x] `RegistryBackupService.cs` - backup/restore registry keys  
- [x] `OptimizationVerifier.cs` - check current state

#### 2.2 Power Optimizations ✅
- [x] `PowerOptimizer.cs`
- [x] Ultimate Performance power plan activation
- [x] Hardware GPU scheduling toggle
- [x] Game Mode enable/disable
- [x] Win32PrioritySeparation optimization

#### 2.3 Service Optimizations ✅
- [x] `ServiceOptimizer.cs`
- [x] Telemetry service management
- [x] SysMain/Superfetch control (SSD detection)
- [x] Windows Search indexing toggle
- [x] Background task optimization

#### 2.4 Network Optimizations ✅
- [x] `NetworkOptimizer.cs`
- [x] TCP optimizations (TcpNoDelay, TcpAckFrequency)
- [x] Delivery Optimization (P2P) toggle
- [x] Nagle algorithm control

#### 2.5 Input & Graphics ✅
- [x] `InputOptimizer.cs`
- [x] Disable mouse acceleration (pointer precision)
- [x] Game DVR/Game Bar control
- [x] Fullscreen optimizations

#### 2.6 Visual Effects ✅
- [x] `VisualEffectsOptimizer.cs`
- [x] Animation toggle
- [x] Transparency effects control
- [x] Balanced vs Minimal presets

#### 2.7 Storage Optimizations ✅
- [x] `StorageOptimizer.cs`
- [x] SSD vs HDD detection (WMI MediaType)
- [x] TRIM enable/disable
- [x] 8.3 filename creation toggle
- [x] Last access timestamp optimization

#### 2.8 UI Implementation ✅
- [x] `SystemOptimizerViewModel.cs`
- [x] `SystemOptimizerView.xaml`
- [x] Add to tab navigation (Optimizer tab)
- [x] Quick action buttons (Gaming Max/Balanced/Revert)
- [x] Individual toggle switches
- [x] Risk indicators per optimization
- [x] System restore point creation before presets

---

### Phase 3: RGB Overhaul (Beta 1 - Mar 2026)

#### 3.1 Asset Preparation 🔧 (in progress)
- [x] Create `Assets/Corsair/` folder with placeholder image (`corsair-placeholder.svg`)
- [x] Create `Assets/Razer/` folder with placeholder image (`razer-placeholder.svg`)
- [x] Create `Assets/Logitech/` folder with placeholder image (`logitech-placeholder.svg`)
- [ ] Brand logo assets (SVG preferred)

Notes: Initial asset folders and placeholder images created. Next: gather real device images, obtain licensing/permission for vendor logos.

#### 3.2 Enhanced Corsair SDK ⬜
- [ ] Upgrade to iCUE SDK 4.0
- [x] Preset application support (`preset:<name>`) and basic device enumeration implemented
- [x] Full-device HID writes for K70/K95/K100 keyboards (no iCUE required)
- [x] Mouse DPI control via HID (Dark Core RGB PRO — 5-stage DPI profiles)
- [ ] Full device enumeration with images
- [ ] Battery status for wireless devices
- [ ] Hardware lighting mode support

#### 3.3 Full Razer Chroma SDK ✅
- [x] Chroma SDK REST API integration (session management, heartbeat)
- [x] Per-key RGB for keyboards (custom effect support)
- [x] Effect library (Wave, Spectrum, Breathing, Reactive, Static)
- [x] Custom effect creator (CreateCustomKeyboardEffect)

#### 3.4 Enhanced Logitech SDK ⬜
- [ ] G HUB SDK integration
- [ ] Direct HID fallback (no G HUB required)
- [x] Static color with brightness and breathing effect support implemented
- [ ] LightSpeed wireless status
- [ ] PowerPlay charging status

#### 3.5 Unified RGB Engine ✅
- [x] "Sync All" functionality (SyncStaticColorAsync, SyncBreathingEffectAsync, SyncSpectrumEffectAsync)
- [x] Cross-brand effect presets (IRgbProvider enhanced with RgbEffectType)
- [ ] Audio-reactive mode (future)
- [ ] Screen color sampling (future)

#### 3.6 Lighting View Redesign ✅
- [x] Device cards with brand headers and DeviceCard styles
- [x] Connection status indicators (badges)
- [ ] Battery level display (future - requires wireless device detection)
- [x] Per-device controls (zone pickers, effect selectors)
- [x] "Apply to System" action (SyncAllRgbAsync)

**Progress update (2025-12-28):**
- Initial provider wiring implemented (`IRgbProvider`, `RgbManager`). Providers registered in priority order: **Corsair → Logitech → Razer → SystemGeneric**.
- **Corsair**: `CorsairRgbProvider` added; supports `color:#RRGGBB` and `preset:<name>`; unit tests added and passing. Added direct HID improvements: retries, heuristics, diagnostics, failed-device tracking, and **full-device keyboard writes for K70/K95/K100**, plus a **K100 per-key payload stub** so many keyboards can be controlled without iCUE and we have a path to per-key features. A Settings toggle (`CorsairDisableIcueFallback`) and corresponding tests were added to allow users to opt into HID-only mode.
- **Logitech**: `LogitechRgbProvider` implements `color:#RRGGBB@<brightness>` and `breathing:#RRGGBB@<speed>`; unit tests added and passing.
- **Razer**: Full `RazerRgbProvider` with Chroma SDK REST API integration for static, breathing, spectrum, and wave effects.
- **SystemGeneric**: `RgbNetSystemProvider` added (experimental) using RGB.NET; initialization and basic color application validated in tests.
- Added unit tests for providers and Corsair HID helpers; all related provider and HID tests pass locally (see `OmenCoreApp.Tests/TestResults/test_results.trx`).

**Note / Next steps:** Add telemetry opt-in for anonymous PID success counts and continue DPI/macro HID research for mice support.

---

### Phase 4: Linux Support (Beta 2 - Mar 2026)

#### 4.1 Linux CLI ✅
- [x] Create `OmenCore.Linux` project (System.CommandLine CLI)
- [x] EC register access via `/sys/kernel/debug/ec/ec0/io` (LinuxEcController)
- [x] Fan control commands (--mode auto|max, --custom, --status)
- [x] Performance mode commands (--mode balanced|performance|quiet)
- [x] Keyboard lighting commands (--color, --brightness, --zone)
- [x] Status/monitor commands (hardware info, real-time monitoring)
- [x] --version flag with OS detection
- [x] Battery-aware fan mode (--battery-aware)
- [x] Battery command (status, profile, threshold)

#### 4.2 Linux Daemon ✅
- [x] Daemon mode implementation (`daemon --run`)
- [x] systemd service file (auto-generated via `--install`)
- [x] TOML configuration (`/etc/omencore/config.toml`)
- [x] Automatic fan curves (FanCurveEngine with hysteresis)
- [x] Signal handling (SIGTERM/SIGHUP) and PID file
- [x] Battery-aware fan control (auto-quiet on battery)

#### 4.3 Distro Testing ✅
- [x] Ubuntu 24.04 (documented in LINUX_TESTING.md)
- [x] Fedora 40 (documented in LINUX_TESTING.md)
- [x] Arch Linux (documented in LINUX_TESTING.md)
- [x] Pop!_OS (documented in LINUX_TESTING.md)

---

### Phase 5: Advanced Features (RC - Apr 2026)

#### 5.1 OSD Overlay ✅
- [x] RTSS integration (RtssIntegrationService via shared memory)
- [x] Mode change toast notifications (ToastNotificationService)
- [x] Transparent overlay window (OsdOverlayWindow)
- [x] Customizable metrics display

#### 5.2 Game Profiles ✅
- [x] Process detection for games (GameLibraryService with Steam/Epic/GOG/Xbox/Ubisoft/EA)
- [x] Per-game settings storage (GameProfile model with comprehensive options)
- [x] Auto-apply on game launch (ProcessMonitoringService integration)
- [x] Steam/GOG/Epic library integration (GameLibraryService platform scanning)
- [x] Game Library View UI (GameLibraryView.xaml with scan, filter, profile management)

#### 5.3 GPU Overclocking 🔧 (in progress)
- [x] NVAPI SDK integration (NvapiService with P/Invoke)
- [x] Core clock offset slider (GpuCoreClockOffset property)
- [x] Memory clock offset slider (GpuMemoryClockOffset property)
- [x] Power limit adjustment (GpuPowerLimitPercent property)
- [ ] V/F curve editor (advanced)
- [x] **GPU Voltage/Current Graph** - Real-time V/C monitoring chart

#### 5.4 CPU Overclocking ✅
- [x] PL1/PL2 adjustment UI (CpuPl1Watts, CpuPl2Watts properties)
- [x] Turbo duration control (via performance modes)
- [x] Comprehensive warnings (built into UI descriptions)
- ✅ **Per-Core Undervolt** - Individual undervolt controls for each CPU core (UI implemented; hardware application/verification in progress)

---

### Phase 6: Polish & Release (May 2026)

#### 6.1 Linux GUI 🔧 (in progress)
- [x] Avalonia UI setup (OmenCore.Avalonia project created)
- [x] Main window with navigation sidebar
- [x] Dashboard view (temperatures, fans, usage, power)
- [x] Fan control view (presets, custom curves)
- [x] System control view (performance modes, GPU switching, keyboard lighting)
- [x] Settings view (startup, defaults, appearance)
- [x] Linux hardware service (sysfs/hwmon integration)
- [x] TOML configuration service
- [ ] Cross-platform testing
- [ ] Package for distribution (AppImage, Flatpak)

#### 6.2 Bloatware Manager ✅
- [x] App enumeration (AppX packages, Win32 apps, startup items, scheduled tasks)
- [x] Safe removal with backup support
- [x] Restoration capability for AppX and startup items
- [x] Risk level indicators (Low/Medium/High)
- [x] Category filtering and search
- [x] BloatwareManagerView.xaml with full UI

#### 6.3 UI/UX Polish ✅
- [x] Removed duplicate BooleanToVisibilityConverter
- [x] Added IsDeferredScrollingEnabled for smooth scrolling
- [x] Fixed async void methods with try-catch
- [x] Replaced emoji icons with vector Path elements in FanControlView
- [x] Improved TextTertiary contrast for WCAG AA compliance (#8D92AA → #9BA0B8)
- [x] Added AutomationProperties.Name for accessibility
- [x] Added F5 keyboard shortcut
- [x] Fixed OnPropertyChanged broadcast in MainViewModel

#### 6.4 Final Testing ⬜
- [ ] Full regression testing
- [ ] Performance benchmarking
- [ ] Documentation updates

---

## 📝 Changelog

### v2.0.0-alpha3 (January 1, 2026) ✅ Current

#### Added
- 🔧 Corsair Mouse DPI Control (5-stage profiles, save/load/delete)
- 🖱️ Corsair HID DPI research documentation

#### Fixed
- 🔄 Corsair HID stack overflow in `BuildSetColorReport`
- 🧪 Test parallelization conflicts (6 flaky tests fixed)
- 📊 26+ code analyzer warnings (IDE0052, IDE0059, IDE0060)

#### Technical
- 🧪 66 tests passing (up from 24)
- 📦 Clean build (0 warnings, 0 errors)
- 🔧 Removed dead code and obsolete methods

---

### v2.0.0-alpha2 (December 28, 2025) ✅

#### Added
- 🧩 RGB provider framework runtime wiring
- 🎛 Corsair provider: Preset application
- 🌈 Logitech provider: Brightness & Breathing effects
- 🔬 System RGB provider (experimental, RGB.NET)
- 📌 "Apply to System" action in Lighting UI
- 🔧 Corsair HID reliability improvements (retries, backoff, failed-device tracking)
- ⚙️ Settings: Corsair HID-only toggle

---

### v2.0.0-alpha1 (December 19, 2025) ✅

#### Added
- ❄️ Extreme fan preset (100% at 75°C for high-power systems)
- 🎛️ Extreme button in Fan Control GUI
- 🎨 **DarkContextMenu control** - Custom context menu with no white margins
- ⚡ **System Optimizer** - Complete Windows gaming optimization suite
  - Power: Ultimate Performance plan, GPU scheduling, Game Mode, foreground priority
  - Services: Telemetry, SysMain/Superfetch, Search Indexing, DiagTrack
  - Network: TCP NoDelay, ACK frequency, Nagle algorithm, P2P updates
  - Input: Mouse acceleration, Game DVR, Game Bar, fullscreen optimizations
  - Visual: Transparency, animations, shadows, performance presets
  - Storage: TRIM, last access timestamps, 8.3 names, SSD detection
- 🎯 One-click optimization presets (Gaming Max, Balanced, Revert All)
- 🔒 Registry backup and system restore point creation
- 🏷️ Risk indicators for each optimization (Low/Medium/High)

#### Changed
- 📄 Roadmap renamed from v1.6 to v2.0
- 🔄 TrayIconService refactored to use DarkContextMenu

#### Fixed
- ✅ White margins in system tray context menu eliminated
- ✅ Fixed large code corruption in `SystemControlViewModel.cs` that caused build failures; normalized per-core offset types (`int?[]`) and restored clean compilation
- ✅ All unit tests passing locally (24/24) (Logging hardening added; tests resilient to locked log files via `OMENCORE_DISABLE_FILE_LOG`)

---

### Upcoming Changes

*This section will be updated as features are implemented*

---

## 🎯 Implementation Strategy

### Why Start with System Tray + System Optimizer?

1. **System Tray Fix (Phase 1.1)** ✅ COMPLETE
   - Currently has visible bugs (white margins, icon issues)
   - Quick visual win that improves user perception
   - Low risk - doesn't affect core functionality
   - ~1-2 days of work

2. **System Optimizer (Phase 2)** ✅ COMPLETE
   - High user demand (you already have the batch script)
   - Complements existing features (Fan + Thermal + GPU Power + **System Tweaks**)
   - Self-contained - can be developed in parallel
   - Uses familiar C#/.NET APIs (Registry, WMI, ServiceController)
   - Provides immediate value to gaming users
   - ~2-3 weeks of work

3. **RGB Overhaul (Phase 3) - Next**
   - Requires external SDK dependencies
   - Need device images/logos (licensing considerations)
   - More complex with multiple vendor integrations
   - Best done after core stability

4. **Linux (Phase 4) - Later**
   - Requires new project structure
   - Different testing environment
   - Can be parallelized once Windows is stable

---

## 📊 Progress Summary

| Phase | Status | Progress |
|-------|--------|----------|
| 1. Foundation & Quick Wins | ✅ Complete | 16/20 |
| 2. System Optimizer | ✅ Complete | 35/35 |
| 3. RGB Overhaul | ✅ Complete | 22/24 |
| 4. Linux Support | ✅ Complete | 14/14 |
| 5. Advanced Features | ✅ Complete | 15/15 |
| 6. Polish & Release | 🔧 In Progress | 20/22 |

**Overall: 122/130 tasks (94%)**

---

### v2.0.1-beta (January 2, 2026) ✅ Current

#### Added
- 🐧 **Linux CLI Improvements**
  - `--version` flag with detailed version info
  - `battery` command (status, profile, charge threshold)
  - `--battery-aware` fan mode for automatic power-source switching
- 🔋 **Battery-Aware Fan Control** (FanCurveEngine)
  - Auto-quiet fans when on battery power
  - Configurable speed reduction (default 20%)
  - Critical temp override (85°C+)
- 🗑️ **Bloatware Manager** - Complete Windows bloatware management
  - AppX package scanning and removal
  - Win32 application detection via Registry
  - Startup item management
  - Scheduled task detection and control
  - Risk assessment (Low/Medium/High)
  - Restore previously removed packages
- 🐧 **Avalonia GUI Improvements**
  - Battery-aware fan toggle in Settings
  - Configurable battery speed reduction
  - Version display updated to 2.0.1-beta
- 📚 **Documentation**
  - LINUX_TESTING.md - Distro-specific testing guides
  - README.md - Updated with Linux support and v2.0.1 features

#### Fixed
- 🔧 Removed duplicate BooleanToVisibilityConverter
- 🔧 Added IsDeferredScrollingEnabled for smooth scrolling
- 🔧 Fixed async void exception handling
- 🔧 Replaced emoji icons with vector Path elements
- 🔧 Improved text contrast for accessibility
- 🔧 Added AutomationProperties for screen readers
- 🔧 Added F5 keyboard shortcut for refresh

---

## 🔗 Quick Links

- [ROADMAP_v2.0.md](ROADMAP_v2.0.md) - Full feature specifications
- [v1.5-stable branch](https://github.com/theantipopau/omencore/tree/v1.5-stable) - Bug fixes for current release
- [v2.0-dev branch](https://github.com/theantipopau/omencore/tree/v2.0-dev) - Active development

---

*Last Updated: January 2, 2026*
