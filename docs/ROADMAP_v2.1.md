# OmenCore v2.1.0 Roadmap

**Target Release:** Q1 2026  
**Status:** In Progress  
**Last Updated:** January 3, 2026

---

## Overview

Version 2.1.0 focuses on **completing advanced features** that were stubbed or planned in v2.0:

| # | Feature | Priority | Effort | Status |
|---|---------|----------|--------|--------|
| 1 | GPU Overclocking (NVAPI) | 🔴 High | Medium | ✅ **Fully Implemented** |
| 2 | Desktop RGB Support | 🔴 High | High | ✅ **Implemented** |
| 3 | Audio-Reactive RGB | 🟡 Medium | Medium | ✅ **Implemented** |
| 4 | Screen Color Sampling | 🟡 Medium | Medium | ✅ **Fully Implemented** |
| 5 | Linux GUI (Avalonia) | 🟡 Medium | Very High | ✅ **Implemented** |
| 6 | SSD Temperature | 🟢 Low | Low | ✅ **Implemented** |
| 7 | Independent Fan Curves | 🔴 High | Medium | ✅ **Implemented** |

---

## 1. GPU Overclocking (NVAPI) 🔴

**Status:** ✅ Implemented  
**Files:** `src/OmenCoreApp/Hardware/NvapiService.cs` (825 lines)

### Completed Features

- ✅ NVAPI SDK P/Invoke integration
- ✅ `SetCoreClockOffset()` - Core clock offset (-500 to +300 MHz)
- ✅ `SetMemoryClockOffset()` - Memory clock offset (-500 to +1500 MHz)
- ✅ `SetPowerLimit()` - Power limit adjustment
- ✅ GPU capability detection
- ✅ Error handling and logging

### Implementation Plan

#### Phase 1: NVAPI SDK Integration
- [x] Download and integrate NVAPI SDK headers
- [x] Implement `NvAPI_GPU_SetPstates20` for clock offsets
- [x] Implement `NvAPI_GPU_SetPowerPoliciesStatus` for power limits
- [x] Add proper error handling and GPU capability detection

#### Phase 2: UI Enhancements
- [x] Add core clock offset slider (-500 to +300 MHz)
- [x] Add memory clock offset slider (-500 to +1500 MHz)
- [x] Add power limit slider (50% to 120% TDP)
- [x] Add "Apply on startup" toggle for OC settings
- [x] Add safety warnings and confirmation dialogs

#### Phase 3: Advanced Features
- [ ] V/F curve editor (per-point voltage/frequency)
- [ ] OC profiles (save/load/switch)
- [ ] Game profile OC integration
- [ ] Stability test integration (optional)

### Technical Details

```csharp
// NvAPI_GPU_SetPstates20 - Set GPU clock offsets
[DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr NvAPI_QueryInterface(uint interfaceId);

// Interface IDs
const uint NvAPI_GPU_SetPstates20_ID = 0x0F4DAE6B;
const uint NvAPI_GPU_GetPstates20_ID = 0x6FF81213;
const uint NvAPI_GPU_SetPowerPoliciesStatus_ID = 0xAD95F5ED;
```

### Safety Considerations
- ⚠️ OC can cause crashes, artifacts, or hardware damage
- Require explicit user acknowledgment before enabling
- Auto-revert on crash detection (watchdog timer)
- Limit offsets to safe ranges based on GPU model

---

## 2. Desktop RGB Support 🔴

**Status:** ✅ Implemented  
**Files:** `src/OmenCoreApp/Hardware/OmenDesktopRgbService.cs` (553 lines)

### Completed Features

- ✅ OMEN desktop model detection (45L, 40L, 35L, 30L, 25L, Obelisk)
- ✅ WMI-based zone detection
- ✅ USB HID fallback detection
- ✅ Per-zone color control
- ✅ Effect modes (static, breathing, color cycle, rainbow)
- ✅ Zone enumeration (fans, strip, logo, front panel)

### User Request

> "OMEN desktop lighting - RGB fan control, interior strip light control, omen logo lights - is that planned?"

### Supported Hardware

OMEN Desktops with RGB:
- OMEN 45L / 40L / 35L / 30L / 25L
- OMEN Obelisk series
- Components:
  - RGB case fans (up to 6)
  - Interior LED strip
  - OMEN logo lighting
  - Front panel accents

### Research

**OpenRGB Support:**
- Partial support exists for some OMEN desktop RGB
- Uses USB HID or SMBus communication
- Reference: [OpenRGB Supported Devices](https://openrgb.org/devices)

**HP WMI Methods (potential):**
```
HPWMI\DesktopRGB
  - SetLedColor(zone, r, g, b)
  - SetLedMode(zone, mode, speed)
  - GetLedZoneCount()
```

### Implementation Plan

#### Phase 1: Hardware Detection
- [ ] Detect OMEN desktop models via WMI/BIOS
- [ ] Enumerate RGB zones (fans, strip, logo)
- [ ] Identify communication protocol (USB HID vs WMI)

#### Phase 2: RGB Control
- [ ] Implement `OmenDesktopRgbService`
- [ ] Per-zone color control
- [ ] Effect modes (static, breathing, color cycle, rainbow)
- [ ] Sync with peripheral RGB

#### Phase 3: UI
- [ ] Desktop RGB section in Lighting view
- [ ] Zone visualization (case diagram)
- [ ] Individual zone color pickers
- [ ] Effect selector per zone

### Files to Create

```
src/OmenCoreApp/Hardware/
  OmenDesktopRgbService.cs       # Main service ✅ DONE
  OmenDesktopRgbProvider.cs      # IRgbProvider implementation
  
src/OmenCoreApp/Models/
  DesktopRgbZone.cs              # Zone model (fan, strip, logo)
```

---

## 3. Audio-Reactive RGB 🟡

**Status:** ✅ Implemented  
**Files:** `src/OmenCoreApp/Services/AudioReactiveRgbService.cs` (685 lines)

### Completed Features

- ✅ WASAPI loopback capture (no external dependencies)
- ✅ FFT for frequency analysis
- ✅ Beat detection algorithm (bass threshold)
- ✅ Audio level normalization
- ✅ Multiple visualization modes:
  - **Pulse** - Flash on beat
  - **Spectrum** - Color based on frequency
  - **VU Meter** - Brightness based on volume
  - **Wave** - Ripple effect on beat
- ✅ Configurable sensitivity and color palettes
- ✅ Works with all RGB providers (HP, Corsair, Logitech, Razer)

### Feature Description

RGB lighting that reacts to system audio in real-time:
- Bass hits trigger color pulses
- Volume controls brightness
- Frequency spectrum maps to color wheel
- Works across all RGB providers (HP, Corsair, Logitech, Razer)

### Implementation Plan

#### Phase 1: Audio Capture
- [x] Use NAudio/CSCore for WASAPI loopback capture
- [x] Implement FFT for frequency analysis
- [x] Beat detection algorithm (bass threshold)
- [ ] Audio level normalization

#### Phase 2: Effect Engine
- [ ] Create `AudioReactiveEffect` class
- [ ] Map frequencies to RGB values
- [ ] Configurable sensitivity and color mapping
- [ ] Multiple visualization modes:
  - **Pulse** - Flash on beat
  - **Spectrum** - Color based on frequency
  - **VU Meter** - Brightness based on volume
  - **Wave** - Ripple effect on beat

#### Phase 3: Integration
- [ ] Add to unified RGB engine
- [ ] Enable per-device audio-reactive toggle
- [ ] Settings for sensitivity, mode, color palette
- [ ] Performance optimization (limit update rate)

### Technical Details

```csharp
// NAudio WASAPI loopback capture
using var capture = new WasapiLoopbackCapture();
capture.DataAvailable += (s, e) => {
    var samples = ConvertToFloatArray(e.Buffer, e.BytesRecorded);
    var fft = PerformFFT(samples);
    var bassLevel = GetBassLevel(fft, 20, 200); // 20-200 Hz
    UpdateRgbFromAudio(bassLevel, fft);
};
```

### Files to Create

```
src/OmenCoreApp/Services/
  AudioCaptureService.cs         # WASAPI loopback + FFT
  AudioReactiveRgbService.cs     # Maps audio to RGB commands
  
src/OmenCoreApp/Models/
  AudioVisualizationMode.cs      # Pulse, Spectrum, VU, Wave
```

---

## 4. Screen Color Sampling 🟡

**Status:** ✅ Fully Implemented (Core service + UI)  
**Files:** `src/OmenCoreApp/Services/ScreenColorSamplingService.cs` (629 lines)

### Feature Description

Ambient lighting that samples screen colors and applies them to RGB devices:
- Edge sampling for bias lighting (like Philips Ambilight)
- Average screen color for simple ambient
- Game-aware color extraction
- Works with all RGB providers

### Completed Features

- ✅ GDI+ screen capture with downscaling for performance
- ✅ Edge zone sampling (top, bottom, left, right)
- ✅ Configurable zones per edge (1-20)
- ✅ Color averaging per zone
- ✅ HSV saturation boost
- ✅ Color smoothing between frames
- ✅ Multiple capture modes (EdgeZones, AverageColor, DominantColor)
- ✅ IRgbProvider integration
- ✅ Event-based color updates

### Implementation Plan

#### Phase 1: Screen Capture ✅
- [x] Use GDI+ for efficient capture
- [x] Implement region sampling (edges, center, full)
- [x] Color averaging with weighted zones
- [x] Target 30-60 FPS capture rate

#### Phase 2: Color Processing ✅
- [x] Edge zone calculation (top, bottom, left, right)
- [x] Color smoothing/interpolation
- [x] Saturation boost option
- [x] Configurable zone count and size

#### Phase 3: RGB Mapping ✅
- [x] Map screen zones to RGB device zones
- [x] Smooth transitions between colors
- [x] Latency optimization (<50ms target)
- [ ] Multi-monitor support (future enhancement)

#### Phase 4: UI Integration ✅
- [x] Add toggle in Settings view
- [x] Add settings sliders (saturation, FPS)
- [ ] Add zone preview visualization (future enhancement)

### Zone Layout

```
┌─────────────────────────────────────────┐
│  T1  │  T2  │  T3  │  T4  │  T5  │  T6  │  ← Top zones → Keyboard zones
├──┬───┴──────┴──────┴──────┴──────┴───┬──┤
│L1│                                   │R1│
├──┤                                   ├──┤
│L2│         Screen Content            │R2│  ← Side zones → Mouse pad
├──┤                                   ├──┤
│L3│                                   │R3│
├──┴───┬──────┬──────┬──────┬──────┬───┴──┤
│  B1  │  B2  │  B3  │  B4  │  B5  │  B6  │  ← Bottom zones → Desk strip
└─────────────────────────────────────────┘
```

### Technical Details

```csharp
// DXGI Desktop Duplication for efficient capture
using var output = factory.GetAdapter1(0).GetOutput(0);
using var output1 = output.QueryInterface<Output1>();
using var duplication = output1.DuplicateOutput(device);

// Sample edge regions
var topColors = SampleRegion(frame, 0, 0, width, sampleHeight);
var bottomColors = SampleRegion(frame, 0, height - sampleHeight, width, sampleHeight);
```

### Files to Create

```
src/OmenCoreApp/Services/
  ScreenCaptureService.cs        # DXGI duplication
  AmbientLightingService.cs      # Zone calculation + RGB mapping
  
src/OmenCoreApp/Models/
  ScreenZone.cs                  # Zone definition
  AmbientLightingConfig.cs       # Settings model
```

---

## 5. Linux GUI (Avalonia) 🟡

**Status:** ✅ Implemented  
**Files:** `src/OmenCore.Avalonia/` (26+ files)

### Completed Features

- ✅ Full Avalonia UI project structure
- ✅ Dashboard view (temps, fan speeds, CPU/GPU/RAM usage)
- ✅ Fan Control view (profiles, custom curves with visual editor)
- ✅ System Control view (performance modes, GPU switching)
- ✅ Settings view (startup options, polling interval, theme)
- ✅ Linux hardware service (sysfs/hwmon temperature and fan reading)
- ✅ TOML configuration support
- ✅ Dark OMEN theme matching Windows aesthetic

### Current State

- ✅ OmenCore.Linux CLI working
- ✅ Linux hardware services implemented
- ✅ Systemd daemon support
- ✅ Full graphical interface (Avalonia)

### Implementation Plan

#### Phase 1: Project Setup
- [ ] Create `OmenCore.Avalonia.Desktop` project
- [ ] Configure cross-platform build (Windows + Linux)
- [ ] Set up shared ViewModels from existing WPF app
- [ ] Implement platform-specific service injection

#### Phase 2: Core Views
- [ ] Dashboard view (temps, fan speeds, status)
- [ ] Fan Control view (profiles, custom curves)
- [ ] Performance view (modes, TDP)
- [ ] Settings view (config, startup options)

#### Phase 3: Platform Integration
- [ ] Linux tray icon (AppIndicator/StatusNotifier)
- [ ] Desktop notifications
- [ ] Autostart via XDG autostart
- [ ] Theme detection (light/dark)

#### Phase 4: Distribution
- [ ] AppImage packaging
- [ ] Flatpak support
- [ ] Debian/RPM packages
- [ ] AUR package (Arch Linux)

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Shared Code                          │
│  ┌─────────────────────────────────────────────────┐   │
│  │              ViewModels (MVVM)                   │   │
│  │   DashboardViewModel, FanControlViewModel, etc  │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │              Services (Interfaces)               │   │
│  │   IHardwareService, IFanService, IRgbProvider   │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
          │                              │
          ▼                              ▼
┌─────────────────────┐      ┌─────────────────────┐
│   OmenCoreApp       │      │  OmenCore.Avalonia  │
│   (Windows WPF)     │      │  (Cross-platform)   │
│                     │      │                     │
│  - WPF XAML Views   │      │  - Avalonia XAML    │
│  - Windows Services │      │  - Linux Services   │
│  - Win32 APIs       │      │  - /sys/class/hwmon │
└─────────────────────┘      └─────────────────────┘
```

### Files to Create

```
src/OmenCore.Avalonia.Desktop/
  OmenCore.Avalonia.Desktop.csproj
  Program.cs
  App.axaml
  App.axaml.cs
  Views/
    MainWindow.axaml
    DashboardView.axaml
    FanControlView.axaml
    SettingsView.axaml
  Styles/
    ModernTheme.axaml
```

---

## Timeline

```
January 2026:
  Week 1-2: GPU Overclocking (NVAPI implementation)
  Week 3-4: Desktop RGB (research + basic implementation)

February 2026:
  Week 1-2: Audio-Reactive RGB
  Week 3-4: Screen Color Sampling

March 2026:
  Week 1-4: Linux GUI (Avalonia)
  
April 2026:
  Testing, polish, release v2.1.0
```

---

## Dependencies

### NuGet Packages

```xml
<!-- Audio capture -->
<PackageReference Include="NAudio" Version="2.2.1" />

<!-- Screen capture -->
<PackageReference Include="SharpDX.DXGI" Version="4.2.0" />

<!-- Avalonia UI -->
<PackageReference Include="Avalonia" Version="11.2.0" />
<PackageReference Include="Avalonia.Desktop" Version="11.2.0" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.0" />
```

### External SDKs

- NVIDIA NVAPI SDK (GPU overclocking)
- OpenRGB SDK (optional, for desktop RGB fallback)

---

## Risk Assessment

| Feature | Risk | Mitigation |
|---------|------|------------|
| GPU OC | Hardware damage | Safety limits, warnings, watchdog |
| Desktop RGB | Limited hardware access | OpenRGB fallback, community testing |
| Audio-Reactive | Performance impact | Configurable update rate, async processing |
| Screen Sampling | Game compatibility | GPU capture fallback, exclude fullscreen option |
| Linux GUI | Platform differences | Extensive cross-platform testing |

---

## Success Metrics

- [ ] GPU OC: Support 80%+ of NVIDIA GPUs (GTX 10xx+)
- [ ] Desktop RGB: Support OMEN 45L/25L minimum
- [ ] Audio RGB: <50ms latency, <5% CPU usage
- [ ] Screen Sampling: 30+ FPS capture, <100ms latency
- [ ] Linux GUI: Feature parity with Windows (core features)

---

## Changelog

### v2.1.0-alpha (Target: Jan 2026)
- GPU Overclocking NVAPI integration
- Desktop RGB basic support

### v2.1.0-beta (Target: Feb 2026)
- Audio-Reactive RGB
- Screen Color Sampling

### v2.1.0 (Target: Apr 2026)
- Linux Avalonia GUI
- Full testing and polish
