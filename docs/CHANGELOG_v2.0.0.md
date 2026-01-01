# Changelog v2.0.0

All notable changes to OmenCore v2.0.0 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.0-alpha3] - 2026-01-01

### Added
- **🔧 Corsair Mouse DPI Control** - Full DPI stage management via HID
  - 5-stage DPI profiles with per-stage configuration
  - DPI profile save/load/delete with overwrite confirmation
  - Stage names, angle snapping, lift-off distance settings
  - Tooltips explaining each DPI feature
- **🖱️ Corsair Mouse HID Research** - Extensive protocol documentation
  - Documented HID endpoints, report IDs, and packet structures
  - Mapped DPI read/write commands for Dark Core RGB PRO
  - Created [HID_DPI_RESEARCH.md](HID_DPI_RESEARCH.md) for future development

### Fixed
- **🔄 Corsair HID Stack Overflow** - Fixed recursion bug in `BuildSetColorReport`
  - Method was calling itself instead of using product-specific switch
  - Added mouse PID payload tests to prevent regression
- **🧪 Test Parallelization Conflicts** - Fixed 6 flaky tests
  - Added `[Collection("Config Isolation")]` to tests that modify `OMENCORE_CONFIG_DIR`
  - Tests now use unique temp directories to avoid cross-test pollution
- **📊 Code Quality** - Fixed 26+ analyzer warnings (IDE0052, IDE0059, IDE0060)
  - Removed unused private fields and unnecessary assignments
  - Fixed unused parameter warnings with proper discards
  - Modernized C# syntax (target-typed new, pattern matching, switch expressions)

### Technical
- **🧪 Test Suite Expanded** - 66 tests passing (up from 24)
  - Added DPI profile tests with save/load/overwrite scenarios
  - Added Corsair mouse PID payload tests
  - Added test collection isolation for config-sensitive tests
- **📦 Build System** - Clean compilation (0 warnings, 0 errors)
- **🔧 Code Cleanup** - Removed obsolete methods and dead code
  - Removed `CreateCpuTempIcon`, `TryAlternativeWmiWatcher`, `ApplyViaEc`
  - Simplified using statements throughout codebase

---

## [2.0.0-alpha2] - 2025-12-28

## [2.0.0-alpha1] - 2025-12-19

### Added
- **🎛️ System Optimizer** - Complete Windows gaming optimization suite
  - Power: Ultimate Performance plan, GPU scheduling, Game Mode, foreground priority
  - Services: Telemetry, SysMain/Superfetch, Search Indexing, DiagTrack
  - Network: TCP NoDelay, ACK frequency, Nagle algorithm, P2P updates
  - Input: Mouse acceleration, Game DVR, Game Bar, fullscreen optimizations
  - Visual: Transparency, animations, shadows, performance presets
  - Storage: TRIM, last access timestamps, 8.3 names, SSD detection
- **🎯 One-click optimization presets** (Gaming Max, Balanced, Revert All)
- **🔒 Registry backup and system restore point creation**
- **🏷️ Risk indicators** for each optimization (Low/Medium/High)
- **⚡ Extreme fan preset** (100% at 75°C for high-power systems)
- **🎨 DarkContextMenu control** - Custom context menu with no white margins
- **🛠️ Keyboard Lighting Diagnostics** - Diagnostics panel with device detection, test patterns, and log collection
- **🔄 Modern toggle switches** - Replaced checkboxes with iOS-style toggles in Settings
- **⚡ GPU Voltage/Current Graph** - Added GPU V/C monitoring chart to dashboard
- **🔧 Per-Core Undervolt** - Individual undervolt controls for each CPU core

### Changed
- **📄 Roadmap renamed** from v1.6 to v2.0
- **🔄 TrayIconService refactored** to use DarkContextMenu
- **📐 Sidebar width increased** from 200px to 230px for better readability
- **📊 LoadChart Performance** - Improved rendering smoothness from 10 FPS to 20 FPS
  - Reduced render throttling from 100ms to 50ms intervals
  - Implemented polyline reuse to avoid object recreation overhead
  - Added BitmapCache for better visual performance

### Fixed
- **💥 System Tray Crash** - Fixed crash when right-clicking tray icon
  - Bad ControlTemplate tried to put Popup inside Border
  - Replaced with simpler style-based approach
- **🔄 Tray Icon Update Crash** - Fixed "Specified element is already the logical child" error
  - Issue in UpdateFanMode/UpdatePerformanceMode when updating menu headers
  - Now creates fresh UI elements instead of reusing existing ones
- **❌ Right-click Context Menu** - Fixed tray icon right-click menu not appearing
  - Applied dark theme styling to regular ContextMenu (dark background, white text, OMEN red accents)
  - Temperature display and color changes restored to tray icon
- **✅ Platform compatibility warnings** - Added [SupportedOSPlatform("windows")] attributes
  - Fixed 57 compilation warnings for Windows-only APIs in System Optimizer
- **🐞 Adopted v1.5.0-beta4 bug fixes**
  - Fixed unexpected keyboard color reset on startup (auto-set to red)
  - Fixed incorrect quick profile state (panel vs active profile mismatch)
  - Fixed slow fan-profile transition/latency when switching profiles
  - Improved CPU temperature monitoring accuracy with better smoothing and sensor selection
- **⚙️ Fan persistence & restore** - Fixed startup restoration for saved fan presets
  - Built-in names like "Max", "Auto", "Quiet" now recognized and reapplied on boot
  - Added manual "Force reapply" command and non-blocking verification
- **⚙️ Fan smoothing & immediate apply** - Added configurable fan transition smoothing
  - Ramped increments reduce abrupt speed changes
  - "Immediate Apply" option for low-latency user-triggered changes
  - UI: Added Immediate Apply checkbox and smoothing duration/step inputs

### Technical
- **🏗️ MVVM Architecture** - Maintained clean separation of concerns
- **🔧 Service Layer** - RegistryBackupService, OptimizationVerifier, multiple optimizer classes
- **🎨 XAML Styling** - Modern UI with consistent theming (added `AccentGreenBrush`)
- **🧪 Unit Tests** - Test suite passing (base: 24 tests)
- **🛠️ Logging Improvements** - Hardened `LoggingService` to tolerate locked log files
  - Added fallback files and `OMENCORE_DISABLE_FILE_LOG` env toggle for tests
- **📦 Build System** - Clean compilation with no warnings

---

## [2.0.0-alpha2] - 2025-12-28

### Added
- **🧩 RGB provider framework runtime wiring** - `RgbManager` now registers and initializes providers at startup; providers present: Corsair, Logitech, RgbNetSystem.
- **🎛 Corsair provider: Preset application** - Supports `preset:<name>` semantics and applies named presets to Corsair devices (tests added).
- **🌈 Logitech provider: Brightness & Breathing** - `color:#RRGGBB@<brightness>` and `breathing:#RRGGBB@<speed>` syntax supported; unit tests added.
- **🔬 System RGB provider (experimental)** - `RgbNetSystemProvider` uses RGB.NET to apply static colors across supported desktop devices.
- **📌 Apply to System** - Lighting UI now exposes an "Apply to System" action to apply the selected color across available providers.
- **🔧 Keyboard full-zone HID writes** - Added full-device HID write payload support for Corsair keyboards (K70/K95/K100) so many keyboards can be controlled without iCUE.
- **🔬 K100 per-key stub** - Added a small per-key payload stub for K100 keyboards to reserve space for future per-key lighting support.
- **⚙️ Settings: Corsair HID-only toggle** - Added `CorsairDisableIcueFallback` and a Settings UI toggle to optionally disable iCUE fallback and run Corsair devices in HID-only mode (advanced users; restart required).
- **🔧 Corsair HID reliability** - Added write retries, backoff, failed-device tracking (`HidWriteFailedDeviceIds`), and diagnostic helpers to improve robustness when iCUE is not present; unit tests added.
- **🔬 Per-product HID payloads** - Use product and device-type heuristics to select appropriate HID payloads (keyboard full-zone writes, mice payloads); K100 includes a per-key stub for future expansion; tests validate payload selection and behavior.

### Changed
- **🧭 Startup behavior** - `MainViewModel` initializes `RgbManager` and registers providers so lighting actions are available earlier in startup.
- **⚙️ Settings & Config** - `CorsairDeviceService.CreateAsync` and `CorsairRgbProvider` now respect the `CorsairDisableIcueFallback` config flag and the new UI toggle; users can opt into HID-only mode via Settings.
- **🧪 Tests** - Added unit tests for `CorsairRgbProvider`, `LogitechRgbProvider`, `CorsairHidDirect` helpers, and `RgbNetSystemProvider`; all relevant tests pass in local runs.

### Technical
- **✅ Tests** - Relevant provider and Corsair HID tests added and are passing in `OmenCoreApp.Tests` (see `TestResults/test_results.trx`).
- **🔧 Minor refactors** - Improved provider initialization logging and safer surface updates in `RgbNetSystemProvider`.
- **🛠️ Corsair HID diagnostics** - Added diagnostic hooks (e.g., `HidWriteFailedDeviceIds`), test helpers, and an overridable low-level `WriteReportAsync` to enable robust unit testing and failure diagnostics.

---

## [1.6.0-alpha] - 2025-12-25

### Added
- **Rgb provider framework** (`IRgbProvider`, `RgbManager`) — extensible provider model to register multiple RGB backends and apply effects across them.
- **Corsair provider** (`CorsairRgbProvider`) — wraps `CorsairDeviceService` and adds support for `preset:<name>` and `color:#RRGGBB` semantics. Preset names are read from `ConfigurationService.Config.CorsairLightingPresets`.
- **Logitech provider** (`LogitechRgbProvider`) — wraps `LogitechDeviceService` and supports `color:#RRGGBB` static color application across discovered devices.
- **Razer provider** (`RazerRgbProvider`) — adapter that uses the existing `RazerService` Synapse detection and can map simple effects to the service (placeholder for full Chroma integration).
- **System generic provider (experimental)** (`RgbNetSystemProvider`) — uses `RGB.NET` to enumerate and attempt control of any RGB.NET-supported desktop devices. Supports `color:#RRGGBB` and is designed as a fallback for brands without bespoke integrations.
- **Provider wiring on startup** — Providers are created and registered at startup in priority: Corsair → Logitech → Razer → SystemGeneric, enabling a single entrypoint to apply system-wide lighting effects.
- **Unit tests** — Added `CorsairRgbProviderTests` and preliminary tests for RGB provider wiring and behavior.

### Changed
- **LightingViewModel** now accepts an `RgbManager` instance so UI commands can call the unified provider stack.
- **MainViewModel** initializes and registers RGB providers during service initialization.

### Notes & Limitations
- `RgbNetSystemProvider` is experimental — RGB.NET requires platform-specific drivers and may not control every device; robust error handling and fallbacks are included. Added tests to validate that initialization and invalid color inputs are handled gracefully.
- Razer Chroma SDK integration remains a future step (synapse detection already present; effect API is currently a placeholder).
- **Logitech provider** now supports `color:#RRGGBB@<brightness>` and `breathing:#RRGGBB@<speed>` effect syntax.
- Two unrelated fan tests are still being investigated (file-lock and re-apply behavior); these are tracked separately.

### Developer Notes
- Effect syntax for providers:
  - `color:#RRGGBB` — apply a static color to all supported devices in the provider.
  - `preset:<name>` — lookup a named preset in configuration (only implemented for Corsair provider in this spike).

### Next steps
1. Implement preset UI mapping and enable applying saved presets from the Lighting UI.
2. Extend Logitech provider with breathing/brightness effects and add tests.
3. Harden `RgbNetSystemProvider` and add integration tests that run in CI using emulated device layers when needed.
4. Add "Apply to System" action in the Lighting UI and a small Diagnostics view for RGB testing.

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

### Phase 3: RGB Overhaul (In Progress)
- [ ] Asset Preparation (device images, brand logos)
- [x] Enhanced Corsair SDK (iCUE 4.0, full device enumeration) — basic preset application implemented
- [x] Corsair HID direct control (K70/K95/K100 keyboards, Dark Core RGB PRO mouse)
- [x] Corsair Mouse DPI Control — 5-stage DPI profiles with full HID implementation
- [ ] Full Razer Chroma SDK (per-key RGB, effects library)
- [x] Enhanced Logitech SDK (G HUB integration, direct HID) — breathing/brightness effect support implemented
- [ ] Unified RGB Engine (sync all, cross-brand effects)
- [ ] Lighting View Redesign (device cards, connection status) — partial: "Apply to System" action added

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
- **Next Release:** v2.0.0-beta (when RGB overhaul is complete)

---

*Last Updated: January 1, 2026*