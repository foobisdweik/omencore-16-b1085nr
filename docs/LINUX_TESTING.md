# OmenCore Linux Testing Guide

This guide provides instructions for testing OmenCore on various Linux distributions.

## Prerequisites

### Kernel Modules

OmenCore Linux requires the following kernel modules:

```bash
# Load EC module with write support (required for fan control)
sudo modprobe ec_sys write_support=1

# Verify EC access
ls -la /sys/kernel/debug/ec/ec0/io

# HP WMI module for keyboard lighting
sudo modprobe hp-wmi
```

### Persistent Module Loading

To persist module loading across reboots:

```bash
# /etc/modules-load.d/omencore.conf
ec_sys
hp-wmi
```

```bash
# /etc/modprobe.d/omencore.conf
options ec_sys write_support=1
```

---

## Ubuntu 24.04

### Installation

```bash
# Install .NET 8 runtime
sudo apt update
sudo apt install -y dotnet-runtime-8.0

# Download and extract OmenCore
wget https://github.com/theantipopau/omencore/releases/latest/download/omencore-linux.tar.gz
tar -xzf omencore-linux.tar.gz
cd omencore-linux

# Make CLI executable
chmod +x omencore-cli
```

### Model-Specific Notes

#### OMEN 16 wf0000n1 (2023, 13700HX)
This model uses the **hp-wmi** driver, NOT direct EC access:

```bash
# Load HP-WMI module (required for 2023+ models)
sudo modprobe hp-wmi

# Verify HP-WMI is available
ls /sys/devices/platform/hp-wmi/

# Check what's available
cat /sys/devices/platform/hp-wmi/thermal_profile 2>/dev/null
```

If hp-wmi doesn't expose fan control, the model may need the `hp-omen-quirks` kernel parameter or a newer kernel.

### Testing Checklist

- [ ] **CLI starts without errors**: `./omencore-cli --help`
- [ ] **Fan status works**: `sudo ./omencore-cli fan --status`
- [ ] **Fan profiles work**: `sudo ./omencore-cli fan --profile auto`
- [ ] **Keyboard lighting works**: `sudo ./omencore-cli keyboard --color FF0000`
- [ ] **System status works**: `./omencore-cli status --json`
- [ ] **Monitor mode works**: `./omencore-cli monitor --interval 2000`
- [ ] **Daemon installs**: `sudo ./omencore-cli daemon --install`
- [ ] **Daemon starts**: `sudo systemctl start omencore`

### Known Issues (Ubuntu)

- **AppArmor**: May block EC access. Check `dmesg` for denials.
- **Secure Boot**: May prevent loading unsigned kernel modules.

---

## Fedora 40

### Installation

```bash
# Install .NET 8 runtime
sudo dnf install -y dotnet-runtime-8.0

# Download and extract OmenCore
wget https://github.com/theantipopau/omencore/releases/latest/download/omencore-linux.tar.gz
tar -xzf omencore-linux.tar.gz
cd omencore-linux

# Make CLI executable
chmod +x omencore-cli
```

### SELinux Configuration

Fedora uses SELinux which may block EC access:

```bash
# Check for SELinux denials
sudo ausearch -m avc -ts recent

# Temporarily set permissive mode for testing
sudo setenforce 0

# Create SELinux policy module (production)
# sudo audit2allow -M omencore < /var/log/audit/audit.log
# sudo semodule -i omencore.pp
```

### Testing Checklist

- [ ] **CLI starts without errors**: `./omencore-cli --help`
- [ ] **Fan control works (check SELinux)**: `sudo ./omencore-cli fan --profile gaming`
- [ ] **Custom fan curve**: `sudo ./omencore-cli fan --curve "40:20,60:50,80:80,90:100"`
- [ ] **Performance modes**: `sudo ./omencore-cli perf --mode performance`
- [ ] **Systemd service works**: `sudo systemctl status omencore`

### Known Issues (Fedora)

- **SELinux**: Requires policy adjustment for EC access.
- **Firewalld**: May affect daemon socket communication.

---

## Arch Linux

### Installation

```bash
# Install .NET 8 runtime
sudo pacman -S dotnet-runtime-8.0

# Or use AUR helper
yay -S omencore-bin  # (if AUR package exists)

# Manual installation
wget https://github.com/theantipopau/omencore/releases/latest/download/omencore-linux.tar.gz
tar -xzf omencore-linux.tar.gz
cd omencore-linux
chmod +x omencore-cli
```

### Testing Checklist

- [ ] **CLI starts**: `./omencore-cli --help`
- [ ] **EC access**: `sudo ./omencore-cli fan --status`
- [ ] **hwmon sensors**: `./omencore-cli status`
- [ ] **Fan curves**: `sudo ./omencore-cli fan --curve "40:25,50:35,60:50,75:75,85:100"`
- [ ] **Keyboard zones**: `sudo ./omencore-cli keyboard --zone 0 --color 00FF00`
- [ ] **Daemon mode**: `sudo ./omencore-cli daemon --run`

### Known Issues (Arch)

- **Kernel updates**: May require module re-loading after kernel updates.
- **Multiple kernels**: Ensure modules are loaded for the running kernel.

---

## Pop!_OS 24.04

Pop!_OS is Ubuntu-based, so most Ubuntu instructions apply.

### Additional Steps

```bash
# Pop!_OS may need additional firmware
sudo apt install linux-firmware

# NVIDIA hybrid graphics (if applicable)
sudo system76-power graphics hybrid
```

### Testing Checklist

- [ ] **All Ubuntu checks apply**
- [ ] **GPU switching (if hybrid)**: Check dgpu/igpu modes
- [ ] **System76 Power compatibility**: Verify no conflicts

---

## Avalonia GUI Testing

The GUI requires additional dependencies:

```bash
# Ubuntu/Pop!_OS
sudo apt install libice6 libsm6 libx11-6 libxext6 libxrandr2 libxi6

# Fedora
sudo dnf install libice libSM libX11 libXext libXrandr libXi

# Arch
sudo pacman -S libice libsm libx11 libxext libxrandr libxi
```

### GUI Checklist

- [ ] **Window opens**: `./omencore-gui`
- [ ] **Navigation works**: Click through tabs
- [ ] **Dashboard displays**: Temperature/fan readings
- [ ] **Fan presets apply**: Click preset buttons
- [ ] **Settings persist**: Toggle options, restart app

---

## Troubleshooting

### EC Access Denied

```bash
# Check if EC module is loaded
lsmod | grep ec_sys

# Check write support
cat /sys/module/ec_sys/parameters/write_support

# Check debug filesystem
mount | grep debugfs
# If not mounted:
sudo mount -t debugfs debugfs /sys/kernel/debug
```

### 2023+ OMEN Models (wf0000, 13700HX, etc.)

Newer 2023 OMEN models use HP-WMI instead of direct EC access. The ec_sys module won't help - you need hp-wmi:

```bash
# Check if HP-WMI is loaded and what it exposes
lsmod | grep hp_wmi
ls -la /sys/devices/platform/hp-wmi/

# If hp-wmi isn't loaded:
sudo modprobe hp-wmi

# Check what interfaces are available
cat /sys/devices/platform/hp-wmi/thermal_profile 2>/dev/null

# If thermal_profile doesn't exist, your kernel may be too old
# Ubuntu 24.04 should have kernel 6.8+ which has good hp-wmi support
uname -r
```

**If nothing works on your 2023 OMEN:**

1. **Check kernel version**: Ubuntu 24.04 LTS ships with kernel 6.8, which should support most OMEN models
2. **Try kernel parameter**: Add `hp_wmi.fan_support=1` to GRUB cmdline
3. **Check BIOS version**: Some features require BIOS F.20+ 
4. **Report your model**: We need more testing data for 2023 models

### No Temperature Readings

```bash
# Check hwmon devices
ls /sys/class/hwmon/

# Find temperature sensors
for hwmon in /sys/class/hwmon/hwmon*/; do
  echo "$hwmon: $(cat $hwmon/name 2>/dev/null)"
  cat $hwmon/temp*_input 2>/dev/null | head -3
done
```

### Keyboard Lighting Not Working

```bash
# Check HP WMI module
lsmod | grep hp_wmi

# Check for keyboard backlight device
ls /sys/class/leds/ | grep kbd
ls /sys/devices/platform/hp-wmi/

# Try direct WMI call
cat /sys/devices/platform/hp-wmi/keyboard_backlight
```

### Daemon Not Starting

```bash
# Check systemd status
sudo systemctl status omencore

# View daemon logs
sudo journalctl -u omencore -f

# Check PID file
cat /var/run/omencore.pid
```

---

## Reporting Issues

When reporting Linux-specific issues, please include:

1. **Distribution and version**: `cat /etc/os-release`
2. **Kernel version**: `uname -r`
3. **Laptop model**: `sudo dmidecode -s system-product-name`
4. **EC module status**: `lsmod | grep ec`
5. **HP-WMI status**: `ls -la /sys/devices/platform/hp-wmi/ 2>/dev/null`
6. **OmenCore version**: `./omencore-cli --version`
7. **Full command output**: Include `--verbose` flag
8. **System logs**: `sudo dmesg | grep -i "hp\|omen\|wmi" | tail -30`

---

## Known Working Configurations

| Model | Kernel | Distro | Access Method | Notes |
|-------|--------|--------|---------------|-------|
| OMEN 15 2020 | 5.15+ | Ubuntu 22.04 | ec_sys | Full support |
| OMEN 16 2022 | 5.19+ | Fedora 38 | ec_sys | Full support |
| OMEN 16 2023 | 6.5+ | Ubuntu 24.04 | hp-wmi | Partial (thermal only) |
| OMEN 17 2021 | 5.15+ | Arch | ec_sys | Full support |

**Help us expand this list!** Report your working configuration on Discord or GitHub.

---

*Last Updated: January 11, 2026*
