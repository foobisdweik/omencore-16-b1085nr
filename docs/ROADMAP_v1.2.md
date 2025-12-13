# OmenCore v1.2 Roadmap

**Target Release:** Q1 2026  
**Status:** Planning  
**Last Updated:** December 13, 2025

---

## Overview

This document outlines planned improvements for OmenCore v1.2, based on analysis of:
- **OmenMon** - HP OMEN hardware monitoring tool (direct competitor/reference)
- **G-Helper** - ASUS laptop utility (UI/UX inspiration, adapted for HP OMEN)
- **OmenHubLighter** - Lightweight HP OMEN utility (Omen key and quick UI features)

> **Note:** G-Helper is designed for ASUS laptops. OmenHubLighter is for older HP OMEN models (4th gen). Features are adapted and reimplemented specifically for modern HP OMEN hardware using HP's WMI BIOS interface and EC access.

---

## 🔴 Critical Priority

### 1. Dynamic Tray Icon with Temperature Display

**Source:** OmenMon  
**Effort:** Medium  
**Impact:** High

Display the current maximum temperature directly in the system tray icon, with optional colored background indicating performance mode.

**Implementation:**
- Render temperature text dynamically on tray icon (e.g., "72°")
- Optional background colors:
  - Blue/Cool: Default/Silent mode
  - Orange/Warm: Performance/Turbo mode
- Update interval: Configurable (default 1 second)

**Tooltip Enhancement:**
```
CPU: 65°C ↑  Fan: 3500 RPM
GPU: 72°C    Fan: 4200 RPM
Mode: Performance
Power: 215W AC Connected
```

**Files to modify:**
- `App.xaml.cs` - Tray icon rendering
- New: `Utils/DynamicIconRenderer.cs`

---

### 2. Quick Mode Buttons (One-Click Switching)

**Source:** G-Helper (adapted for HP OMEN)  
**Effort:** Medium  
**Impact:** High

Replace dropdown selectors with prominent, color-coded buttons for instant mode switching.

**Performance Mode Buttons:**
```
┌─────────────────────────────────────────────────────────┐
│  Performance Mode                                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│  │  Silent  │ │ Balanced │ │  Turbo   │ │  Custom  │   │
│  │   🔵     │ │    🟢    │ │    🔴    │ │    ⚙️    │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │
└─────────────────────────────────────────────────────────┘
```

**GPU Mode Buttons:**
```
┌─────────────────────────────────────────────────────────┐
│  GPU Mode                                               │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│  │   Eco    │ │ Hybrid   │ │ Discrete │ │Optimized │   │
│  │  (iGPU)  │ │ (Auto)   │ │  (dGPU)  │ │  (Auto)  │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │
└─────────────────────────────────────────────────────────┘
```

**Visual feedback:**
- Active button highlighted with accent color
- Hover tooltips explaining each mode
- Transition animations

**Files to modify:**
- `Views/MainWindow.xaml` - Add button groups
- `ViewModels/MainViewModel.cs` - Mode switching commands
- `Styles/Buttons.xaml` - Mode button styles

---

### 3. Automation (AC/Battery Mode Switching)

**Source:** G-Helper (adapted for HP OMEN)  
**Effort:** Medium  
**Impact:** High

Automatically switch performance profiles based on power source.

**Features:**
- Auto Performance Mode:
  - On Battery: Silent/Balanced
  - On AC: Balanced/Turbo (user configurable)
- Auto GPU Mode:
  - On Battery: Eco (iGPU only)
  - On AC: Hybrid/Discrete (user configurable)
- Auto Refresh Rate:
  - On Battery: 60Hz (save power)
  - On AC: Max refresh rate

**Settings UI:**
```
┌─────────────────────────────────────────────────────────┐
│  Automation                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ☑ Auto-switch on power change                   │   │
│  │                                                  │   │
│  │   On Battery:  [Silent     ▼]  GPU: [Eco    ▼] │   │
│  │   On AC:       [Performance▼]  GPU: [Hybrid ▼] │   │
│  │                                                  │   │
│  │ ☑ Auto refresh rate (60Hz on battery)          │   │
│  │ ☑ Dim keyboard backlight on battery            │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

**Implementation:**
- Use `SystemEvents.PowerModeChanged` event
- Store user preferences in config
- Apply settings within 1 second of power change

**Files to create/modify:**
- New: `Services/PowerAutomationService.cs`
- `Models/AppConfig.cs` - Add automation settings
- `ViewModels/MainViewModel.cs` - Wire up automation

---

## 🟡 Important Priority

### 4. Visual Fan Curve Editor

**Source:** G-Helper (adapted for HP OMEN)  
**Effort:** High  
**Impact:** High

Interactive chart-based fan curve editor with drag-and-drop points.

**Features:**
- X-axis: Temperature (20°C - 100°C)
- Y-axis: Fan speed (0% - 100% or RPM)
- Separate curves for CPU and GPU fans
- 8 configurable points per curve (matching HP BIOS structure)
- Real-time preview of current temperature on curve
- Import/Export fan profiles

**Visual Design:**
```
Fan Speed %
100 ┤                              ●━━━━━━━●
 90 ┤                         ●━━━━┘
 80 ┤                    ●━━━━┘
 70 ┤               ●━━━━┘
 60 ┤          ●━━━━┘
 50 ┤     ●━━━━┘
 40 ┤●━━━━┘
 30 ┤
 20 ┼────┬────┬────┬────┬────┬────┬────┬────┬
    20   30   40   50   60   70   80   90  100°C
    
    [CPU Fan ●]  [GPU Fan ●]  [Reset] [Apply]
```

**Implementation:**
- Use LiveCharts2 or OxyPlot for WPF
- Draggable data points
- Curve smoothing/interpolation
- Validate curve monotonicity (fan speed should increase with temp)

**Files to create:**
- New: `Controls/FanCurveEditor.xaml(.cs)`
- New: `ViewModels/FanCurveViewModel.cs`
- Modify: `Views/FanControlView.xaml`

---

### 5. Battery Health & Charge Limit

**Source:** G-Helper (adapted for HP OMEN)  
**Effort:** Medium  
**Impact:** Medium

Control battery charge limit to extend battery lifespan.

**Features:**
- Charge limit slider (60% - 100%)
- "Full Charge" override button for travel
- Battery health/wear percentage display
- Charge/discharge rate display

**UI Design:**
```
┌─────────────────────────────────────────────────────────┐
│  Battery Health                                         │
│                                                         │
│  Charge Limit: ━━━━━━━━━━●━━━━━━ 80%                   │
│                60%              100%                    │
│                                                         │
│  [Full Charge Override]  (for travel, charges to 100%) │
│                                                         │
│  Health: 96.2%  │  Capacity: 83.1 / 86.5 Wh           │
│  Status: Charging at 65W  │  Time to full: 45 min     │
└─────────────────────────────────────────────────────────┘
```

**HP OMEN Implementation:**
- Use HP WMI BIOS battery charge control (if available)
- Fallback: EC register manipulation
- Read battery health via WMI `BatteryFullChargedCapacity` / `DesignCapacity`

**Files to create:**
- New: `Hardware/BatteryController.cs`
- New: `Views/BatteryView.xaml`
- New: `ViewModels/BatteryViewModel.cs`

---

### 6. Enhanced Context Menu

**Source:** OmenMon  
**Effort:** Low  
**Impact:** Medium

Rich right-click context menu on tray icon for quick access.

**Menu Structure:**
```
┌────────────────────────────────────┐
│ OmenCore v1.2.0                    │
├────────────────────────────────────┤
│ Performance Mode           ▶│ ○ Silent
│ GPU Mode                   ▶│ ● Balanced
│ Fan Control                ▶│ ○ Turbo
├────────────────────────────────────┤
│ ☑ Auto-switch on power            │
│ ☐ Start with Windows              │
├────────────────────────────────────┤
│ Show Window                        │
│ Check for Updates                  │
│ Exit                               │
└────────────────────────────────────┘
```

**Files to modify:**
- `App.xaml.cs` - Context menu setup
- New: `Utils/TrayContextMenu.cs`

---

## 🟢 Nice-to-Have

### 7. Omen Key Interception

**Source:** OmenMon, OmenHubLighter  
**Effort:** High  
**Impact:** Medium

Intercept the physical Omen key for custom actions.

**Features:**
- Toggle OmenCore window visibility
- Cycle through fan profiles
- Toggle performance modes
- Launch custom application
- **Remap to any key or hotkey combination** (from OmenHubLighter)
- **Execute custom programs with arguments** (from OmenHubLighter)

**Implementation:**
- Low-level keyboard hook
- WMI event subscription for Omen key
- Configurable action mapping

**Files to create:**
- New: `Services/OmenKeyService.cs`
- New: `Utils/KeyboardHook.cs`

---

### 8. Quick Popup UI (Omen Key Press)

**Source:** OmenHubLighter  
**Effort:** Medium  
**Impact:** Medium

Show a quick popup UI when Omen key is pressed for fast settings access.

**Features:**
- Compact popup near system tray
- Quick fan mode toggle (Auto/Performance/Max/Off)
- Quick performance mode toggle
- Current temperature and fan speed display
- Auto-dismiss after action or timeout

**UI Design:**
```
┌─────────────────────────────────┐
│  OmenCore Quick Settings        │
├─────────────────────────────────┤
│  CPU: 65°C    GPU: 72°C        │
│  Fans: 3500 / 4200 RPM         │
├─────────────────────────────────┤
│  [Auto] [Perf] [Max] [Off]     │
├─────────────────────────────────┤
│  [Silent] [Balanced] [Turbo]   │
└─────────────────────────────────┘
```

**Files to create:**
- New: `Views/QuickSettingsPopup.xaml`
- New: `ViewModels/QuickSettingsViewModel.cs`

---

### 9. Special Key Response

**Source:** OmenHubLighter  
**Effort:** Low  
**Impact:** Low

Respond to special function keys and display status.

**Features:**
- Trackpad lock key response (show OSD when toggled)
- Windows key lock response (show OSD when toggled)
- Keyboard backlight toggle response

**Implementation:**
- Hook WMI events for special key presses
- Show brief OSD notification

**Files to modify:**
- `Services/HotkeyService.cs` - Add special key handlers
- `Views/HotkeyOsdWindow.xaml` - OSD for special keys

---

### 10. Keyboard Backlight Presets

**Source:** OmenMon  
**Effort:** Medium  
**Impact:** Low

Save and load keyboard backlight color presets.

**Features:**
- Save current colors as named preset
- Quick-load from context menu
- Import/Export presets
- Per-zone color configuration (4 zones)

**Files to create:**
- New: `Models/KeyboardPreset.cs`
- New: `Services/KeyboardPresetService.cs`
- Modify: `Views/LightingView.xaml`

---

### 11. GPU Power Level Presets

**Source:** OmenMon  
**Effort:** Low  
**Impact:** Medium

Quick presets for GPU power configuration.

**Presets:**
- **Base Power** - TGP only (power saving)
- **Extra Power** - TGP + cTGP (balanced)
- **Maximum Power** - TGP + cTGP + Dynamic Boost (maximum performance)

**Files to modify:**
- `ViewModels/MainViewModel.cs`
- `Views/MainWindow.xaml`

---

## 🔵 Future Considerations (v1.3+)

### 12. OmenMon-Style Continuous Fan Programs

**Source:** OmenMon  
**Effort:** Very High  
**Impact:** High

Implement OmenMon's continuous fan program system that actively monitors temperature and adjusts fan speeds in real-time.

#### How OmenMon Fan Programs Work

OmenMon's fan programs are fundamentally different from static presets:

1. **Set Fan Mode** - Apply thermal policy (Performance `0x31`, Default `0x30`, Cool `0x50`) via WMI BIOS
2. **Continuous Monitoring** - Every `UpdateProgramInterval` seconds (default: 15s):
   - Read current max temperature from sensors
   - Find highest temperature level ≤ current temp
   - Set fan levels (in krpm) via `SetFanLevel` WMI command or EC writes
3. **Countdown Extension** - Continuously extend the 120-second timeout to prevent BIOS reverting to default mode
4. **GPU Power Control** - Optionally set GPU power level (Minimum/Medium/Maximum) with the program

#### Fan Level Values

Fan levels are specified in **krpm** (thousands of RPM):
- `20` = ~2000 RPM (minimum usable)
- `55` = ~5500 RPM (maximum for CPU fan)
- `57` = ~5700 RPM (maximum for GPU fan)
- `00` = Fan off (caution: at least one fan must run)

#### Example OmenMon Configuration

```xml
<FanPrograms>
    <Program Name="Power">
        <FanMode>Performance</FanMode>      <!-- 0x31 -->
        <GpuPower>Maximum</GpuPower>        <!-- cTGP + PPAB -->
        <Level Temperature="00"><Cpu>00</Cpu><Gpu>00</Gpu></Level>
        <Level Temperature="45"><Cpu>24</Cpu><Gpu>26</Gpu></Level>
        <Level Temperature="55"><Cpu>28</Cpu><Gpu>31</Gpu></Level>
        <Level Temperature="65"><Cpu>36</Cpu><Gpu>40</Gpu></Level>
        <Level Temperature="75"><Cpu>44</Cpu><Gpu>49</Gpu></Level>
        <Level Temperature="85"><Cpu>55</Cpu><Gpu>57</Gpu></Level>
    </Program>
    <Program Name="Silent">
        <FanMode>Default</FanMode>          <!-- 0x30 -->
        <GpuPower>Minimum</GpuPower>        <!-- Base TGP only -->
        <Level Temperature="00"><Cpu>00</Cpu><Gpu>00</Gpu></Level>
        <Level Temperature="60"><Cpu>25</Cpu><Gpu>25</Gpu></Level>
        <Level Temperature="75"><Cpu>40</Cpu><Gpu>40</Gpu></Level>
        <Level Temperature="85"><Cpu>55</Cpu><Gpu>57</Gpu></Level>
    </Program>
</FanPrograms>
```

#### Key Implementation Requirements

1. **Background Service** - Timer-based service running every 15 seconds
2. **WMI BIOS Commands**:
   - `SetFanMode` (0x1A): `{0xFF, mode, 0x00, 0x00}`
   - `SetFanLevel` (0x2E): `{cpuLevel, gpuLevel, 0x00, 0x00}`
3. **Countdown Extension** - Write to EC register `XFCD` (0x63) to reset 120s timer
4. **Alternative Program** - Auto-switch to battery program on AC power loss
5. **Suspend/Resume Handling** - Pause program during sleep, restore on wake

#### Why OmenCore Doesn't Currently Have This

OmenCore currently:
- Maps presets to thermal policies (lets BIOS control fans)
- Doesn't continuously adjust fan levels based on temperature
- Doesn't extend the countdown timer

This is why users report "fan not ramping" - the BIOS's built-in curves may not match user expectations.

#### Files to Create

- `Services/FanProgramService.cs` - Background monitoring service
- `Models/FanProgram.cs` - Program definition model
- `Models/FanProgramLevel.cs` - Temperature/fan level mapping
- `Views/FanProgramEditor.xaml` - Program editor UI

---

### 13. Per-Key RGB Keyboard Support

**Effort:** Very High  
**Impact:** Medium

Support for per-key RGB keyboards on newer OMEN models.

**Deferred Reason:** Requires reverse-engineering per-key protocol

---

### 14. Performance Overlay

**Source:** G-Helper  
**Effort:** High  
**Impact:** Medium

On-screen display showing real-time performance metrics.

**Deferred Reason:** Requires separate overlay window with game compatibility

---

## Implementation Notes

### HP OMEN-Specific Considerations

Unlike G-Helper (ASUS), OmenCore must use:
- **HP WMI BIOS Interface** (`hpqBIntM`) for system control
- **HP Embedded Controller** for low-level fan/sensor access
- **HP-specific thermal platform** (different mode IDs than ASUS)

### Testing Requirements

All v1.2 features must be tested on:
- [ ] OMEN 15 (2021-2023 models)
- [ ] OMEN 16 (2022-2024 models)  
- [ ] OMEN 17 (2022-2024 models)
- [ ] Victus series (if compatible)

### Dependencies to Add

```xml
<!-- For fan curve editor -->
<PackageReference Include="LiveChartsCore.SkiaSharpView.WPF" Version="2.x" />

<!-- For power events -->
<!-- Already available via Microsoft.Win32.SystemEvents -->
```

---

## Timeline

| Phase | Features | Target |
|-------|----------|--------|
| Alpha | Dynamic tray icon, Quick mode buttons | Jan 2026 |
| Beta | Automation, Context menu | Feb 2026 |
| RC | Fan curve editor, Battery health | Mar 2026 |
| Release | All features, documentation | Apr 2026 |

---

## References

- [OmenMon GitHub](https://github.com/OmenMon/OmenMon) - HP OMEN reference implementation
- [OmenMon Documentation](https://omenmon.github.io/) - Feature documentation
- [G-Helper GitHub](https://github.com/seerge/g-helper) - UI/UX inspiration (ASUS laptops)
- [OmenHubLighter GitHub](https://github.com/Joery-M/OmenHubLighter) - Omen key and quick UI features
- [OmenHubLight GitHub](https://github.com/determ1ne/OmenHubLight) - Original HP OMEN utility (archived)
- [HP WMI Documentation](./SDK_INTEGRATION_GUIDE.md) - HP BIOS interface reference
