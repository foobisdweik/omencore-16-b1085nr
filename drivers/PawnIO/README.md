# PawnIO Driver Files

This directory contains files needed for PawnIO-based EC access on systems with Secure Boot enabled.

## What is PawnIO?

PawnIO is a **digitally-signed kernel driver** that provides low-level hardware access while being compatible with Secure Boot. It's the modern replacement for WinRing0 which doesn't work when Secure Boot is enabled.

## Installation

### Option 1: Install PawnIO (Recommended)
1. Download the official PawnIO installer from https://pawnio.eu/
2. Run the installer - this will install the signed driver
3. OmenCore will automatically detect and use PawnIO for EC access

### Option 2: Manual Module Placement
If PawnIO is already installed but modules aren't in the default location:
1. Download `LpcACPIEC.amx` from https://github.com/namazso/PawnIO.Modules/releases
2. Place it in this `drivers` directory
3. Restart OmenCore

## Why PawnIO?

| Feature | WinRing0 | PawnIO |
|---------|----------|--------|
| Secure Boot | ❌ Blocked | ✅ Works |
| Signed Driver | ❌ No | ✅ Yes |
| Antivirus Issues | ⚠️ Often flagged | ✅ Clean |
| HVCI Compatible | ❌ No | ✅ Yes |

## Benefits

- **Secure Boot Compatible**: Works on systems where WinRing0 is blocked
- **Signed Driver**: No need to enable test signing mode
- **Clean AV Record**: Won't trigger false positives like WinRing0
- **Modern Design**: Scriptable, modular architecture

## Module: LpcACPIEC

The `LpcACPIEC.amx` module provides access to the ACPI Embedded Controller through standard ports 0x62 (data) and 0x66 (command/status). This enables:

- Fan speed control
- Keyboard backlight control
- Performance mode switching
- Temperature sensor reading

## Licensing

PawnIO is available under multiple licenses:
- **Official Binary**: Proprietary (redistribution of installer allowed)
- **Open Source**: GPL 2 with exception (driver), LGPL 2.1 (library)

Users should download PawnIO from https://pawnio.eu/ for the best experience.

## Troubleshooting

### PawnIO Not Detected
1. Ensure PawnIO is installed from https://pawnio.eu/
2. Check that the PawnIO service is running
3. Verify `%ProgramFiles%\PawnIO\PawnIOLib.dll` exists

### Module Load Failed
1. Download latest `LpcACPIEC.amx` from GitHub releases
2. Place in `%ProgramFiles%\PawnIO\modules\` or OmenCore's `drivers` folder
3. Restart OmenCore

### Access Denied Errors
1. Run OmenCore as Administrator
2. Ensure no other software is using the EC (close other fan control apps)
