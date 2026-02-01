# [RELEASE] OmenCore v2.6.0 - Self-Sufficient Architecture & OmenMon-Inspired Features

**Date:** February 2026  
**Download:** https://github.com/theantipopau/omencore/releases/tag/v2.6.0

---

## 🎯 What's New in v2.6.0

This is a major release focused on **self-sufficiency** (no external dependencies required) and **OmenMon-inspired features** for power users who want more control.

---

## 🏗️ Self-Sufficient Architecture

OmenCore v2.6.0 now works **completely standalone** using HP WMI BIOS:

- **No kernel driver required** for basic monitoring
- **New WmiBiosMonitor class** provides CPU/GPU temps and fan RPM
- **LibreHardwareMonitor is now optional** - only needed for enhanced metrics like GPU clocks and VRAM usage
- **Automatic fallback chain**: WMI BIOS → LibreHardwareMonitor (if available)
- **PawnIO now installs by default** - enables EC direct access and MSR/undervolt features

---

## 🌀 Fan Control Improvements

### New: Constant Speed Mode (OmenMon-Inspired)
Ever wanted to just lock your fans at a specific speed? Now you can:

- Set fans to **any fixed percentage** (0-100%)
- See **estimated RPM** before applying
- **One-click apply** - instant constant speed activation
- Complements existing Auto, Performance, Balanced, and Curve modes

### RPM Accuracy for V2 Systems (OMEN MAX 2025+)
- **Direct RPM reading** with automatic endianness detection
- **Sanity validation** - no more absurd 20297 RPM or 78 RPM readings
- **V2 command support** (0x38) for newest OMEN models

### EC Conflict Detection
- **OmenMon coexistence** - OmenCore now detects when OmenMon is running
- Automatic retry logic with graceful degradation
- Run both apps without crashes!

---

## 🌈 Temperature-Based RGB (OmenMon-Inspired)

Your keyboard can now **visualize your temps**:

- **Dynamic color changes** based on CPU/GPU temperature
- **Color gradient**: Blue (cool, <50°C) → Yellow (warm, 70°C) → Red (hot, >90°C)
- **2-second polling** for responsive updates
- Works with all 4-zone OMEN keyboards via WMI BIOS

---

## 🐛 Critical Fixes

| Issue | Fix |
|-------|-----|
| **RAM "0/0 GB" display** | Added WMI fallback when LibreHardwareMonitor fails |
| **Temperature freezing** | Enhanced stuck-temp detection with WMI fallback |
| **Ctrl+S hotkey conflict** | Changed to `Ctrl+Shift+Alt+A` (no more Photoshop/VSCode conflicts!) |
| **Fan verification loop** | Uses raw WMI reads instead of estimated values |

---

## ⚡ Performance Improvements

- **1 second faster startup** - WorkerStartupDelayMs reduced from 1500ms to 500ms
- Hardware worker connects quicker on cold starts

---

## 🔋 Power Limit Control

For undervolt/power limit enthusiasts:

- **Check if BIOS locked your power limits** - `IsPowerLimitLocked()`
- **Get detailed PL1/PL2 status** - enable states, lock bits, current values
- **Set both power limits atomically** with verification
- **Full MSR 0x610 support** - proper bit-field handling

---

## 📦 Download Options

**Windows:**
- **Installer**: `OmenCoreSetup-2.6.0.exe` - Full installation with PawnIO
- **Portable**: `OmenCore-2.6.0-win-x64.zip` - No installation needed

**Linux:**
- **x86_64**: `OmenCore-2.6.0-linux-x64.zip`

---

## 🔒 Verify Your Downloads

SHA256 checksums:

```
[Checksums will be added after build]
```

---

## ⏭️ What's Next (v2.7.0)

- 🎮 Game profile auto-switching
- 🔌 Per-key RGB lighting (HID backend)
- 📊 Historical temperature/fan graphs

---

## 🙏 Thanks

Huge thanks to everyone who:
- Reported the RAM 0/0 GB bug
- Tested EC conflict scenarios with OmenMon
- Requested the constant speed mode
- Provided V2 system logs for RPM fixes

Your feedback makes OmenCore better for everyone!

---

**Questions/Issues:** https://github.com/theantipopau/omencore/issues

**Discord:** [Join our community]

---

*OmenCore is a free, open-source alternative to HP OMEN Gaming Hub with more features and better performance.*
