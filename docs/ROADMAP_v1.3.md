# OmenCore v1.3 Roadmap

**Target Release:** Q1 2025  
**Goal:** Complete standalone replacement for HP OMEN Gaming Hub - all features built-in, no external dependencies, Windows Defender safe, fully functional on all supported hardware.

---

## 🎯 Vision

OmenCore v1.3 will be a **complete, self-contained gaming laptop control center** that requires no additional software:
- ✅ No OMEN Gaming Hub needed
- ✅ No third-party undervolt tools (ThrottleStop, XTU)
- ✅ No external monitoring apps
- ✅ No separate RGB software for keyboard
- ✅ Fully signed, Windows Defender safe
- ✅ Works on Secure Boot systems via PawnIO

---

## � Features from OmenMon to Integrate

### 1. **Continuous Fan Programs** (OmenMon's Core Feature)
OmenMon's fan programs are fundamentally different from static presets - they **actively monitor temperature and adjust fan speeds in real-time**.

**How it works:**
1. Set thermal policy (Performance `0x31`, Default `0x30`, Cool `0x50`)
2. Every 15 seconds:
   - Read current max temperature
   - Find matching temperature level in curve
   - Set fan levels (in krpm) via WMI `SetFanLevel` or EC writes
3. Continuously extend 120-second timeout to prevent BIOS reset

**Fan Level Values (krpm):**
- `20` = ~2000 RPM (minimum)
- `40` = ~4000 RPM (medium)
- `55` = ~5500 RPM (CPU max)
- `57` = ~5700 RPM (GPU max)
- `00` = Fan off (caution!)

**Implementation:**
```xml
<Program Name="Power">
    <FanMode>Performance</FanMode>
    <GpuPower>Maximum</GpuPower>
    <Level Temperature="45"><Cpu>24</Cpu><Gpu>26</Gpu></Level>
    <Level Temperature="65"><Cpu>36</Cpu><Gpu>40</Gpu></Level>
    <Level Temperature="85"><Cpu>55</Cpu><Gpu>57</Gpu></Level>
</Program>
```

### 2. **EC Direct Fan Level Setting** (FanLevelUseEc)
- Use Embedded Controller instead of WMI BIOS call for fan levels
- Fallback when WMI `SetFanLevel` doesn't work on some models
- **This is likely why users report only MAX preset works!**

### 3. **Fan Countdown Extension** (FanCountdownExtendAlways)
- Continuously extend HP BIOS 120-second fan timer
- Keep custom fan settings permanent without active program
- Write to EC register `XFCD` (0x63) to reset timer

### 4. **Multiple Temperature Sensors**
- Support up to 9 EC sensors: `CPUT`, `GPTM`, `RTMP`, `TMP1`, `TNT2-5`
- Plus BIOS temperature sensor
- Show trend indicators (↑ ascending, ↓ descending)
- Configure which sensors contribute to "max temp" calculation

### 5. **Advanced Optimus Fix**
- Detect when NVIDIA switches GPU mode
- Reapply color profile (fixes color issues)
- Restart Explorer shell (fixes stutter)
- Restart NVIDIA Display Container service

### 6. **Omen Key Interception & Custom Actions**
- Intercept physical Omen key press
- Options:
  - Show/hide main window
  - Toggle fan program on/off
  - Cycle through all fan programs
  - Execute custom command (e.g., turn off display)
  - Remap to any key combination

### 7. **Display Off (While System Runs)**
- Turn off display and keyboard backlight
- System continues running (for downloads, music, etc.)
- Uses `SendMessage` with `SC_MONITORPOWER`

### 8. **Refresh Rate Presets**
- Quick switch between high (165Hz) and low (60Hz) rates
- Configurable preset values
- From tray menu

### 9. **Color Profile Reload**
- Reload default display color profile
- Fixes profile being dropped when display settings change

### 10. **GPU Mode Switching Without Reboot Menu**
- Toggle Discrete/Optimus from context menu
- Equivalent to BIOS setting change
- Prompts for reboot

---

## 🟡 Features from OmenHubLighter to Integrate

### 1. **Quick Popup UI** (Omen Key Press)
Compact popup near tray for fast settings:
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

### 2. **Trackpad Lock Key Response**
- Detect trackpad lock toggle
- Show OSD notification when toggled

### 3. **Windows Key Lock Response**  
- Detect Windows key lock toggle
- Show OSD notification

### 4. **Fan Speed Text Display**
- Show actual RPM values alongside fan duty %

### 5. **Remap Omen Key to Any Key**
- Map to single key or hotkey combination
- Or execute custom programs with arguments

---

## �🐛 Priority Bug Fixes (v1.2.2)

### Critical

1. **Installer Post-Install Launch Error (Code 740)**
   - **Issue:** First launch after install shows "CreateProcess failed; code 740" (elevation required)
   - **Cause:** Inno Setup Run section doesn't request elevation for post-install launch
   - **Fix:** Add `Verb: runas` to the Run section for elevated launch
   - **Status:** ✅ Fixed in installer

2. **Fan Presets Not Working (Only MAX Works)**
   - **Issue:** Users report only MAX preset affects fans; other presets don't align with curves
   - **Cause:** HP BIOS thermal policy overrides custom fan levels on many models
   - **Investigation Needed:**
     - SetFanLevel WMI command may not be supported on all models
     - SetFanMode sets thermal policy but doesn't allow granular speed control
     - Some models only support Max/Auto, not intermediate levels
   - **Potential Solutions:**
     - Detect thermal policy version and available features
     - Fall back to EC direct access (via PawnIO) for true custom curves
     - Use SetFanTable WMI command if supported (CMD 0x32)
     - Document which models support which features

3. **High CPU Usage / ACPI.sys DPC Spikes**
   - **Issue:** Some users report CPU spikes related to ACPI.sys
   - **Cause:** Frequent WMI queries can trigger ACPI overhead
   - **Solutions:**
     - Reduce polling frequency when app is minimized
     - Implement adaptive polling (less frequent when temps stable)
     - Cache WMI results and only refresh on demand
     - Add "Low Resource Mode" toggle

---

## ✨ New Features

### 1. 🖥️ In-Game OSD (Overlay)

**Priority:** High  
**Complexity:** High

A real-time overlay showing system stats during gaming:

```
┌─────────────────────────┐
│ CPU: 75°C  GPU: 68°C    │
│ FPS: 144   Load: 45%    │
│ Fan: 65%   RAM: 8.2 GB  │
└─────────────────────────┘
```

**Implementation Options:**
- **Option A: Transparent WPF Window** (Simplest)
  - Always-on-top borderless window
  - Works with all games (windowed/borderless)
  - Doesn't work with exclusive fullscreen
  
- **Option B: DirectX/Vulkan Hook** (Complex)
  - Inject overlay into game rendering pipeline
  - Works with exclusive fullscreen
  - Requires careful implementation to avoid anti-cheat issues
  - May trigger false positives in some games

- **Option C: RTSS Integration** (Recommended)
  - Use RivaTuner Statistics Server API
  - Well-established, game-compatible
  - Requires RTSS to be running
  - Can display custom OmenCore stats

**Features:**
- Toggle hotkey (e.g., F12)
- Position: Corner selection (top-left, top-right, etc.)
- Transparency slider
- Custom metrics selection
- Show/hide specific values
- Throttling warning indicator

### 2. ⚡ Complete Undervolting Suite

**Priority:** High  
**Complexity:** Medium

#### Intel Undervolting (Existing - Enhance)
- [ ] Detect locked/unlocked BIOS automatically
- [ ] Per-core P-state modification
- [ ] Turbo ratio limits adjustment
- [ ] Power limit (PL1/PL2) modification
- [ ] Better stability testing integration
- [ ] Profile auto-switch based on workload

#### AMD Ryzen Undervolting (New)
- [ ] **Curve Optimizer** via PawnIO SMU
  - Per-core curve offset (-30 to +30)
  - Best-core detection
  - Auto-tuning wizard
- [ ] **PPT/TDC/EDC Limits**
  - Package Power Tracking limit
  - Thermal Design Current
  - Electrical Design Current
- [ ] **Precision Boost Overdrive (PBO)**
  - Enable/disable toggle
  - Scalar adjustment (1x-10x)
  - Max boost clock offset
- [ ] **CO Auto-Tuner**
  - Run stability tests per core
  - Find optimal negative offset
  - Save stable profile

**UI Enhancements:**
- Undervolt stability indicator
- Crash detection and auto-revert
- Per-application undervolt profiles
- Export/import settings

### 3. 🎮 GPU Control Suite

**Priority:** High  
**Complexity:** Medium

#### NVIDIA GPU Control (via NVAPI)
- [ ] **GPU Core Clock Offset** (+/- MHz)
- [ ] **Memory Clock Offset** (+/- MHz)
- [ ] **Power Limit** (% of TDP)
- [ ] **Temperature Target** (°C)
- [ ] **Fan Curve Override** (if supported)
- [ ] **Voltage/Frequency Curve Editor**
  - Visual V/F curve like MSI Afterburner
  - Drag points to adjust voltage at each frequency step

#### AMD GPU Control (via ADL)
- [ ] **GPU Clock Range** (min/max)
- [ ] **Memory Clock**
- [ ] **Power Limit** (watts)
- [ ] **Fan Curve**

**Per-Game GPU Profiles:**
- Automatically apply OC/UV when game launches
- Revert to default when game closes
- Profile sharing/export

### 4. 🌀 Advanced Fan Control

**Priority:** High  
**Complexity:** Medium

- [ ] **Per-Fan Custom Curves**
  - Separate curves for CPU and GPU fans
  - Different response curves per thermal zone
  
- [ ] **Hysteresis Settings**
  - Prevent fan speed oscillation
  - Configurable temperature dead-zone
  - Ramp-up/ramp-down rates
  
- [ ] **Fan Table Upload**
  - Use SetFanTable (CMD 0x32) for permanent custom curves
  - Persist across reboots (BIOS-level)
  - Requires compatible BIOS
  
- [ ] **EC Direct Access Fallback**
  - When WMI SetFanLevel doesn't work
  - Use PawnIO for direct EC register writes
  - Full custom curve support

- [ ] **Acoustic Profiles**
  - "Silent" - Prioritize noise over temps
  - "Balanced" - Normal operation
  - "Performance" - Prioritize cooling over noise
  - "Custom" - User-defined

### 5. 🔋 Battery Management

**Priority:** Medium  
**Complexity:** Medium

- [ ] **Charge Limit**
  - Set max charge to 80% for battery longevity
  - HP BIOS may support this via WMI
  
- [ ] **Battery Health Report**
  - Design capacity vs current capacity
  - Cycle count
  - Wear level percentage
  - Health trend over time
  
- [ ] **Battery Calibration Wizard**
  - Guide through full discharge/charge cycle
  - Improve battery gauge accuracy

- [ ] **Power Plan Auto-Switch**
  - Different Windows power plan on AC vs battery
  - Sync with OmenCore performance modes

### 6. 🖥️ Display Control (Enhanced)

**Priority:** Medium  
**Complexity:** Low

- [ ] **Refresh Rate Profiles**
  - Quick switch: 60Hz / 144Hz / 165Hz / etc.
  - Auto-switch based on power source
  
- [ ] **G-Sync/FreeSync Toggle**
  - Enable/disable variable refresh rate
  
- [ ] **Brightness Control**
  - Slider with keyboard shortcuts
  - Auto-brightness based on time of day
  
- [ ] **Color Profile Switching**
  - sRGB / Adobe RGB / Native
  - Per-game color profiles

### 7. 🌐 Network Optimization

**Priority:** Low  
**Complexity:** Medium

- [ ] **Game Traffic Prioritization**
  - Windows QoS rules for game processes
  - Ping optimization
  
- [ ] **Network Latency Monitor**
  - Real-time ping to game servers
  - Packet loss indicator
  
- [ ] **WiFi Optimization**
  - Disable power saving for adapter
  - Roaming aggressiveness

### 8. 🎨 RGB Keyboard (Complete)

**Priority:** Medium  
**Complexity:** Medium

- [ ] **All HP Keyboard Zones**
  - Support 1-zone, 4-zone, per-key (if available)
  
- [ ] **More Effects**
  - Rainbow wave
  - Reactive typing
  - Audio visualizer
  - Screen color sync
  
- [ ] **Effect Speed/Brightness**
  - Adjustable animation speed
  - Brightness levels
  
- [ ] **Game Integration**
  - HP/Discord/game status colors
  - Health bar sync (via SDK)

### 9. 🔌 Peripheral Support (SDK Integration)

**Priority:** Low  
**Complexity:** High

#### Corsair iCUE
- [ ] Full iCUE SDK integration
- [ ] Device lighting control
- [ ] DPI stage configuration
- [ ] Macro playback

#### Logitech G HUB
- [ ] Full Logitech SDK integration
- [ ] Device lighting sync
- [ ] DPI/sensitivity control

#### Razer Chroma
- [ ] Chroma SDK integration
- [ ] Sync with keyboard lighting

### 10. 📊 Advanced Monitoring

**Priority:** Medium  
**Complexity:** Low

- [ ] **Sensor History Export**
  - CSV export of temperature/load data
  - Session statistics
  
- [ ] **Benchmark Mode**
  - Log peak temps/clocks during gaming
  - Stress test integration
  
- [ ] **Notification Alerts**
  - Temperature threshold warnings
  - Throttling detection notifications
  - Low battery alerts

---

## 🔒 Security & Compatibility

### Windows Defender Compatibility
- [ ] **Code Signing Certificate**
  - Sign all executables and DLLs
  - EV certificate for SmartScreen trust
  
- [ ] **Safe Driver Implementation**
  - PawnIO is already signed and safe
  - Remove WinRing0 dependency completely
  - Document security model

### Secure Boot Support
- [ ] **Full functionality via PawnIO**
  - EC access without disabling Secure Boot
  - SMU access for AMD undervolt
  
- [ ] **WMI-only mode**
  - Graceful fallback when no driver available
  - Document feature limitations

---

## 🏗️ Architecture Improvements

### Code Quality
- [ ] Increase unit test coverage to 80%+
- [ ] Add integration tests for hardware access
- [ ] Performance profiling and optimization
- [ ] Memory leak detection and fixes

### Modularity
- [ ] Plugin system for future extensions
- [ ] Hardware abstraction layer improvements
- [ ] Configuration schema versioning

### Documentation
- [ ] API documentation for developers
- [ ] Hardware compatibility database
- [ ] Troubleshooting guides

---

## 📅 Release Schedule

### v1.2.2 (Hotfix) - December 2024
- ✅ Installer elevation fix
- Fan preset investigation
- CPU usage optimization

### v1.3.0-beta1 - January 2025
- OSD overlay (transparent window)
- Advanced fan control with hysteresis
- AMD PBO basic support

### v1.3.0-beta2 - February 2025
- GPU clock offset (NVIDIA)
- Battery charge limit
- Display refresh profiles

### v1.3.0 - March 2025
- Full feature release
- Documentation complete
- Stability tested

---

## 🤝 Community Requests

Tracking feature requests from GitHub Issues and community feedback:

| Request | Votes | Status | Notes |
|---------|-------|--------|-------|
| In-game OSD | ⭐⭐⭐⭐⭐ | Planned v1.3 | Top requested |
| AMD Curve Optimizer | ⭐⭐⭐⭐ | Planned v1.3 | Via PawnIO SMU |
| Battery charge limit | ⭐⭐⭐ | Planned v1.3 | If BIOS supports |
| GPU overclocking | ⭐⭐⭐ | Planned v1.3 | NVAPI integration |
| Per-key RGB | ⭐⭐ | Investigating | Hardware dependent |
| Custom fan tables | ⭐⭐ | Planned v1.3 | WMI SetFanTable |

---

## 📝 Notes

### Known Hardware Limitations

1. **Fan Control Granularity**
   - Some HP models only support Max/Auto modes via WMI
   - True custom curves may require EC direct access
   - SetFanLevel command not functional on all BIOS versions

2. **GPU Mode Switching**
   - Requires reboot on most models
   - Some models require BIOS setting change

3. **Intel Undervolt**
   - Locked on 11th gen+ mobile CPUs
   - Requires BIOS unlock or older firmware

4. **AMD Undervolt**
   - PBO limits may be locked by HP BIOS
   - Curve Optimizer requires SMU access via PawnIO

---

## 💡 Contributing

Want to help with v1.3? Here's how:
1. **Testing:** Report bugs and compatibility issues
2. **Development:** Submit PRs for features/fixes
3. **Documentation:** Help with guides and wiki
4. **Hardware Info:** Share EC register maps for your laptop model

GitHub: https://github.com/theantipopau/omencore
