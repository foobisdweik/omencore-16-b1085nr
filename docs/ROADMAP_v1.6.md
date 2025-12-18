# OmenCore v1.6 Roadmap

**Target Release:** Q2 2026  
**Status:** Planning  
**Last Updated:** December 18, 2025

---

## Overview

Version 1.6 focuses on:
- 🐧 **Linux Support** (Priority #1)
- 📺 **On-Screen Display (OSD)** improvements
- ⚡ **CPU/GPU Overclocking** (beyond undervolting)
- 🎮 **Advanced Game Integration**

### Research Sources

- [hp-omen-linux-module](https://github.com/pelrun/hp-omen-linux-module) - Linux kernel module for HP Omen WMI (220 stars)
- [omen-fan](https://github.com/alou-S/omen-fan) - Python-based Linux fan control utility (99 stars)
- [OmenHubLight](https://github.com/determ1ne/OmenHubLight) - Archived C# Omen utility (architecture reference)

---

## 🔴 Critical Priority: Linux Support

### 1. Linux CLI Tool (Phase 1)

**Target:** v1.6.0-alpha  
**Effort:** High  
**Impact:** Very High

Create a command-line utility for Linux that provides core functionality without a GUI.

#### Architecture

```
┌────────────────────────────────────────────────────────────┐
│                    omencore-cli                            │
├────────────────────────────────────────────────────────────┤
│  Commands:                                                 │
│    omencore fan --profile auto|silent|gaming|max           │
│    omencore fan --speed 50%                                │
│    omencore fan --curve "40:20,50:30,60:50,80:80,90:100"  │
│    omencore perf --mode balanced|performance               │
│    omencore keyboard --color FF0000                        │
│    omencore keyboard --zone 0 --color 00FF00               │
│    omencore status                                         │
│    omencore monitor                                        │
├────────────────────────────────────────────────────────────┤
│  Daemon mode:                                              │
│    omencore-daemon --config /etc/omencore/config.toml     │
│    systemctl enable omencore                               │
└────────────────────────────────────────────────────────────┘
```

#### Implementation Strategy

**Recommended: Hybrid Approach**
- Phase 1: Python CLI for basic fan control (quick win, inspired by omen-fan)
- Phase 2: Full .NET CLI with all features  
- Phase 3: Avalonia UI cross-platform GUI

#### EC Register Map (from omen-fan)

Based on [omen-fan/docs/probes.md](https://github.com/alou-S/omen-fan/blob/main/docs/probes.md):

```
Fan Control:
  0x34*   Fan 1 Speed Set     units of 100RPM  
  0x35*   Fan 2 Speed Set     units of 100RPM
  0x2E    Fan 1 Speed %       Range 0 - 100
  0x2F    Fan 2 Speed %       Range 0 - 100
  0xEC?   Fan Boost           00 (OFF), 0x0C (ON)
  0xF4*   Fan State           00 (Enable), 02 (Disable)

Temperature:
  0x57    CPU Temp            int °C
  0xB7    GPU Temp            int °C

BIOS Control:
  0x62*   BIOS Control        00 (Enabled), 06 (Disabled)
  0x63*   Timer               Counts down from 120 (0x78) to 0
                              Resets fan control when reaches 0

Power:
  0x95*   Performance Mode    0x30=Default, 0x31=Performance, 0x50=Cool
  0xBA**  Thermal Power       00-05 (power limit multiplier)
```

#### Linux Kernel Requirements

```bash
# Load EC module with write support
sudo modprobe ec_sys write_support=1

# Verify EC access
ls -la /sys/kernel/debug/ec/ec0/io

# HP WMI module (keyboard lighting, hotkeys)
modprobe hp-wmi
```

#### Files to Create

```
src/
  OmenCore.Linux/
    OmenCore.Linux.csproj          # .NET 8 cross-platform project
    Program.cs                      # CLI entry point
    Commands/
      FanCommand.cs
      PerformanceCommand.cs
      KeyboardCommand.cs
    Hardware/
      LinuxEcController.cs          # /sys/kernel/debug/ec/ec0/io access
      LinuxHwMonController.cs       # /sys/class/hwmon/* sensors
      LinuxKeyboardController.cs    # /sys/devices/platform/hp-wmi/*
```

---

### 2. Linux GUI (Phase 2)

**Target:** v1.6.0-beta  
**Effort:** Very High  

**Recommended: Avalonia UI**
- Reuse XAML knowledge from WPF
- Single codebase for Windows/Linux
- MVVM architecture compatible

---

### 3. Linux Systemd Integration

```ini
# /etc/systemd/system/omencore.service
[Unit]
Description=OmenCore Fan Control Daemon
After=multi-user.target

[Service]
Type=simple
ExecStart=/usr/bin/omencore-daemon
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

---

## 🟡 Important: On-Screen Display (OSD)

### 4. In-Game OSD Overlay

**Target:** v1.6.0  
**Effort:** High  
**Impact:** High

Display real-time performance metrics as an overlay during games.

```
┌──────────────────────────────┐
│ CPU: 65°C  4.8GHz  45W      │
│ GPU: 72°C  1890MHz 125W     │
│ RAM: 28.4/32 GB             │
│ FAN: 3500 / 4200 RPM        │
│ FPS: 142                    │
└──────────────────────────────┘
```

**Implementation Options:**
- **Windows:** RTSS (RivaTuner) integration
- **Linux:** MangoHud integration
- **Fallback:** Transparent overlay window (borderless games)

---

### 5. Mode Change OSD

Show brief notification when performance mode or fan profile changes.

```
┌─────────────────────────────────┐
│   🔥 Performance Mode          │
│      Turbo Engaged             │
└─────────────────────────────────┘
        (fades after 2 seconds)
```

---

## ⚡ Advanced: CPU/GPU Overclocking

### 6. CPU Overclocking (Intel)

**Target:** v1.6.0  
**Risk:** ⚠️ High - Can cause instability

**Features:**
- PL1/PL2 (Power Limits) adjustment
- Turbo duration (Tau) control
- Already have undervolt, add overvolt option (+0 to +100mV)

**Implementation:**
```csharp
// MSR_PKG_POWER_LIMIT (0x610)
public void SetPL1(int watts, int timeWindow);
public void SetPL2(int watts);
```

---

### 7. GPU Overclocking (NVIDIA)

**Target:** v1.6.0  
**Effort:** Medium  

**Features:**
- Core clock offset: -500MHz to +300MHz
- Memory clock offset: -500MHz to +1500MHz  
- Power limit slider (if not vBIOS locked)
- V/F curve editor (advanced)

**Implementation:**
- NVAPI SDK integration
- Similar to MSI Afterburner functionality

---

### 8. GPU Overclocking (AMD)

**Target:** v1.6.0  
**Effort:** Medium  

**Features:**
- Core/Memory frequency adjustment
- STAPM/Fast/Slow power limits
- RyzenAdj integration for mobile APUs

---

### 9. Overclocking Profiles

Save/load overclocking configurations per-game or per-use-case.

```json
{
  "name": "Gaming Profile",
  "cpu": { "pl1_watts": 55, "undervolt_mv": -100 },
  "gpu": { "core_offset_mhz": 150, "memory_offset_mhz": 500 }
}
```

---

## 🎮 Game Integration

### 10. Per-Game Profiles

Automatically apply settings when specific games launch.

**Detection Methods:**
- Process name monitoring
- Steam/GOG/Epic library integration
- Manual executable selection

---

### 11. Game Library View

```
┌─────────────────────────────────────────────────────────┐
│  Game Library                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 🎮 Cyberpunk 2077        [Turbo Gaming]         │   │
│  │    [▶ Launch] [⚙️ Settings]                      │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ 🎮 Elden Ring            [Balanced]             │   │
│  │    [▶ Launch] [⚙️ Settings]                      │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

## 🔧 Architecture Improvements (from v1.5 audit)

### Consolidated Hardware Polling

Currently FanService and HardwareMonitoringService run separate polling loops.

**Goals:**
- Single `ThermalDataProvider` shared service
- FanService subscribes to temperature events
- Reduce CPU overhead

---

### HPCMSL Integration for BIOS Updates

Replace HP API scraping with official HP Client Management Script Library.

**Benefits:**
- Authoritative BIOS version information
- Proper SoftPaq metadata

---

### Per-Fan Curve Support

Allow independent fan curves for CPU and GPU fans.

**Implementation:**
- "Link fans" toggle (default: linked)
- Separate CPU/GPU curve editors when unlinked

---

## Bug Fixes

### Auto-Update File Locking Issue

**Problem:** Auto-update download fails with `IOException` when computing SHA256 hash.

**Fix:**
- Ensure download stream disposed before hash computation
- Use `FileShare.Read` when opening for hash

---

## Implementation Timeline

| Phase | Version | Target | Features |
|-------|---------|--------|----------|
| Research | - | Jan 2026 | Linux hp-wmi, Avalonia feasibility |
| Alpha | 1.6.0-alpha | Feb 2026 | Linux CLI, Basic EC access |
| Beta | 1.6.0-beta | Mar 2026 | Linux daemon, OSD overlay |
| RC | 1.6.0-rc | Apr 2026 | GPU/CPU OC, Game profiles |
| Release | 1.6.0 | May 2026 | Full Linux GUI, All features |

---

## Technical Debt / Prerequisites

1. **Refactor Hardware Abstraction**
   - Create `IFanController`, `IPerformanceController` interfaces
   - Enable dependency injection for platform-specific code

2. **Configuration Migration**
   - Move from JSON to TOML (cross-platform standard)

3. **Logging Improvements**
   - Structured logging (Serilog)
   - Platform-appropriate log locations

---

## Risk Assessment

| Feature | Risk | Mitigation |
|---------|------|------------|
| Linux EC Access | Medium | Extensive testing, kernel version checks |
| CPU Overclocking | High | Comprehensive warnings, conservative defaults |
| GPU Overclocking | Medium | Use vendor APIs, respect vBIOS limits |
| OSD Overlay | Low | RTSS/MangoHud integration |

---

## Linux Testing Matrix

| Distro | Desktop | HP-WMI | Status |
|--------|---------|--------|--------|
| Ubuntu 24.04 | GNOME | TBD | |
| Fedora 40 | GNOME | TBD | |
| Arch | KDE | TBD | |
| Pop!_OS 24.04 | COSMIC | TBD | |

---

## References

- [hp-omen-linux-module](https://github.com/pelrun/hp-omen-linux-module) - Linux kernel WMI module
- [omen-fan](https://github.com/alou-S/omen-fan) - Python Linux fan control
- [OmenHubLight](https://github.com/determ1ne/OmenHubLight) - Archived C# implementation
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [MangoHud](https://github.com/flightlessmango/MangoHud) - Linux gaming overlay
- [NVAPI](https://developer.nvidia.com/nvapi) - NVIDIA GPU control API
- [RyzenAdj](https://github.com/FlyGoat/RyzenAdj) - AMD mobile power management

---

## Community Feedback Requests

We're looking for testers with:
- HP Omen laptops running Linux (any distro)
- AMD GPU Omen models
- Victus series laptops
- Omen laptops with per-key RGB keyboards

---

*Last Updated: December 18, 2025*
