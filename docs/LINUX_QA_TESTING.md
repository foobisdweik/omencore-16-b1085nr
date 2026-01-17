# OmenCore Linux Testing & CI Guide for v2.5.0

**Purpose**: Ensure OmenCore Linux CLI and daemon work reliably across distribution versions and kernel configurations.

---

## Testing Checklist

### Prerequisites
- Linux x64 system (Ubuntu 22.04 LTS, Debian 12, Fedora 39+ recommended)
- Kernel 5.15+ (6.1+ strongly recommended for full EC support)
- HP OMEN laptop (2020+)
- Root/sudo access for EC operations

### Test Scenarios

#### 1. CLI Installation & Basic Commands
- [ ] Download Linux x64 artifact from release
- [ ] Extract and verify checksums: `sha256sum -c OmenCore-2.5.0-linux-x64.checksums`
- [ ] Run: `./omencore-cli status` → Should show system info and driver status
- [ ] Run: `./omencore-cli --help` → Should list all commands without error
- [ ] Run: `./omencore-cli fan --help` → Should show fan commands with valid modes

**Error Handling**:
- [ ] Test invalid command: `./omencore-cli invalid-cmd` → Clear error message
- [ ] Test with wrong args: `./omencore-cli fan -m invalid_mode` → Error lists valid modes
- [ ] Test without sudo for EC access: Should warn about insufficient privileges

#### 2. Fan Control (EC Access)
- [ ] Run as sudo: `sudo ./omencore-cli fan -m performance` → Set to performance mode
- [ ] Run: `sudo ./omencore-cli fan get` → Shows current fan state
- [ ] Run: `sudo ./omencore-cli fan -m quiet` → Fans should become quieter
- [ ] Run: `sudo ./omencore-cli fan -m balanced` → Fans return to balanced

**Kernel Compatibility Tests**:
- [ ] Ubuntu 22.04 (kernel 5.15): Test EC access (may warn about missing ec_sys module)
- [ ] Ubuntu 24.04 (kernel 6.8): Full EC support expected
- [ ] Debian 12 (kernel 6.1+): Full EC support expected
- [ ] Fedora 39+ (kernel 6.x): Full EC support expected

#### 3. Performance Mode Control
- [ ] `sudo ./omencore-cli perf -m balanced` → Performance mode set
- [ ] `sudo ./omencore-cli perf -m performance` → Performance mode set
- [ ] Valid modes: `default`, `balanced`, `performance`, `cool`
- [ ] Invalid mode test: `sudo ./omencore-cli perf -m turbo` → Error message lists valid modes

#### 4. Daemon Mode (Monitoring)
- [ ] Start daemon: `sudo ./omencore-cli daemon start`
- [ ] Check process: `ps aux | grep omencore` → Daemon running
- [ ] Stop daemon: `sudo ./omencore-cli daemon stop`
- [ ] Verify stopped: `ps aux | grep omencore` → No daemon process

**systemd Integration**:
- [ ] Daemon starts on boot: `sudo systemctl enable omencore-daemon`
- [ ] Daemon stops cleanly: `sudo systemctl stop omencore-daemon`

#### 5. HP WMI Integration (If Available)
- [ ] Check OGH availability: `sudo ./omencore-cli status` → Shows "OGH Proxy Available" if supported
- [ ] On supported models: Perf mode changes should reflect in HP BIOS settings

#### 6. Error Handling & Edge Cases
- [ ] Kernel < 5.15: Show clear message about limited EC support
- [ ] Missing ec_sys module: Suggest installation with clear command
- [ ] Non-OMEN laptop: Graceful error message (not a crash)
- [ ] Simultaneous CLI & daemon: Handle lock gracefully

---

## Artifact Verification

### Build Artifacts Produced
```
OmenCore-2.5.0-linux-x64/
├── omencore-cli                    # Main executable
├── LICENSE
├── README.md
└── checksums.sha256                # Verification file
```

### Checksum Verification
```bash
# Download and verify
wget https://github.com/theantipopau/omencore/releases/download/v2.5.0/OmenCore-2.5.0-linux-x64.tar.gz
wget https://github.com/theantipopau/omencore/releases/download/v2.5.0/OmenCore-2.5.0-linux-x64.checksums

# Verify
sha256sum -c OmenCore-2.5.0-linux-x64.checksums

# Expected output:
# OmenCore-2.5.0-linux-x64.tar.gz: OK
```

---

## CI Pipeline Configuration

### GitHub Actions Workflow
Create `.github/workflows/linux-qa.yml`:

```yaml
name: Linux QA & Artifact Build

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-22.04
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    
    - name: Build Linux CLI
      run: |
        dotnet publish src/OmenCore.Linux/OmenCore.Linux.csproj \
          -c Release -r linux-x64 \
          --self-contained -p:DebugType=none
    
    - name: Generate Checksums
      run: |
        cd src/OmenCore.Linux/bin/Release/net8.0/linux-x64
        sha256sum omencore-cli > checksums.sha256
        cat checksums.sha256
    
    - name: Run CLI Tests
      run: |
        chmod +x src/OmenCore.Linux/bin/Release/net8.0/linux-x64/omencore-cli
        ./src/OmenCore.Linux/bin/Release/net8.0/linux-x64/omencore-cli --help
        ./src/OmenCore.Linux/bin/Release/net8.0/linux-x64/omencore-cli --version
    
    - name: Create Artifact Package
      run: |
        mkdir -p dist/OmenCore-2.5.0-linux-x64
        cp src/OmenCore.Linux/bin/Release/net8.0/linux-x64/omencore-cli dist/OmenCore-2.5.0-linux-x64/
        cp LICENSE dist/OmenCore-2.5.0-linux-x64/
        cp src/OmenCore.Linux/bin/Release/net8.0/linux-x64/checksums.sha256 dist/OmenCore-2.5.0-linux-x64/
        cd dist && tar -czf OmenCore-2.5.0-linux-x64.tar.gz OmenCore-2.5.0-linux-x64/
    
    - name: Upload Artifact
      uses: actions/upload-artifact@v3
      with:
        name: linux-artifacts
        path: dist/OmenCore-2.5.0-linux-x64.tar.gz
```

---

## Error Messages Improvement

### Current → Improved Examples

**Current**: `Failed to set performance mode: performance`  
**Improved**: 
```
✗ Failed to set performance mode: performance
  Valid modes: default, balanced, performance, cool
  💡 Tips: 
    - Ensure you're running as root: sudo omencore-cli perf -m performance
    - Some OMEN models require kernel 6.1+ for full EC support
    - Check: uname -r
```

**Current**: `Error: Cannot access EC`  
**Improved**:
```
✗ Cannot access EC (Embedded Controller)
  This system may not have EC_SYS module loaded or EC access is blocked.
  
  To enable EC access:
    1. Load ec_sys module: sudo modprobe ec_sys write_support=1
    2. Make persistent: echo "ec_sys" | sudo tee /etc/modules-load.d/ec_sys.conf
    3. Enable write support: echo "options ec_sys write_support=1" | sudo tee /etc/modprobe.d/ec_sys.conf
    4. Reboot or: sudo modprobe -r ec_sys && sudo modprobe ec_sys write_support=1
```

**Current**: `OGH not available`  
**Improved**:
```
⚠️ OGH Proxy not available on this model/kernel combination
  Your model: OMEN 16-c0xxx
  Kernel: 5.15.0 (< 6.0 recommended minimum)
  
  Workarounds:
    - Upgrade kernel to 6.1+ for better HP WMI integration
    - Use EC-based fan control (requires ec_sys module)
    - Use BIOS settings to control fans if CLI control unavailable
```

---

## Known Issues & Mitigations

### Issue: EC_SYS Module Not Available
**Symptoms**: Cannot read/write EC registers  
**Solution**: `sudo modprobe ec_sys write_support=1`

### Issue: Kernel Version Too Old (< 6.0)
**Symptoms**: OGH proxy returns errors, limited EC register support  
**Solution**: Upgrade to Ubuntu 24.04 or Debian 12.1+

### Issue: OGH Service Missing
**Symptoms**: "OGH service not found" on some OMEN Transcend models  
**Solution**: Use WMI BIOS mode via WPF app, or update HP BIOS

---

## Next Release (v2.6.0) Roadmap
- [ ] Daemon mode with auto-restart on crash
- [ ] Linux-native GUI (GTK/Qt alternative)
- [ ] Systemd service file in installer
- [ ] SELinux policy for EC access
