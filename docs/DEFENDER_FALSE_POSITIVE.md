# Windows Defender VulnerableDriver.WinNT/Wiring0 False Positive

## Issue Summary

Windows Defender flags the following as threats:
- `VulnerableDriver.WinNT/Wiring0` (detected in LibreHardwareMonitor-net472)
- `VulnerableDriver.WinNT/Wiring0` (detected in OmenCore.HardwareWorker testhost)

**Status**: This is a **known false positive**. Both LibreHardwareMonitor and WinRing0 are legitimate, open-source projects used by thousands of developers globally.

---

## Why This Happens

### Root Cause
Windows Defender flags drivers and libraries that have low-level hardware access capabilities (port I/O, direct memory access, EC register manipulation). These capabilities are:
- **Necessary** for legitimate hardware monitoring and control
- **Potentially Dangerous** if misused by malware (hence the flag)
- **Properly Sandboxed** in OmenCore with safety allowlists

### Affected Libraries
1. **WinRing0** (`WinRing0.sys`)
   - Low-level driver for CPU/EC access via port I/O
   - Used by: Intel XTU, CoreTemp, OmenCore, LibreHardwareMonitor
   - Open-source: https://github.com/libudis86/udis86
   - Not blocked, but flagged for monitoring

2. **LibreHardwareMonitor-net472**
   - Open-source monitoring library: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
   - Used by: OmenCore, many enterprise monitoring tools
   - Contains WinRing0 wrapper code

---

## Mitigation Strategies

### ✅ Option 1: Use PawnIO (Recommended)
**Best for**: Secure Boot enabled systems

PawnIO is a modern, professionally maintained EC/MSR access driver:
- **Secure Boot Compatible**: Digitally signed and verified
- **Better Vetted**: Commercial product with security audit trail
- **Performance**: More optimized for modern systems
- **Cost**: Free for end-users (https://pawnio.eu/)

**How to use**:
1. Download from https://pawnio.eu/
2. Install as per instructions
3. OmenCore automatically prioritizes PawnIO over WinRing0
4. No more false positives

### ✅ Option 2: Add Exclusions to Windows Defender
**Best for**: Existing WinRing0 users

Add these folders/files to Windows Defender exclusions:

**PowerShell (as Administrator)**:
```powershell
# Add program exclusions
Add-MpPreference -ExclusionPath "C:\Program Files\OmenCore" -ErrorAction SilentlyContinue
Add-MpPreference -ExclusionPath "$env:LOCALAPPDATA\OmenCore" -ErrorAction SilentlyContinue

# Add process exclusions
Add-MpPreference -ExclusionProcess "OmenCore.exe" -ErrorAction SilentlyContinue
Add-MpPreference -ExclusionProcess "OmenCore.HardwareWorker.exe" -ErrorAction SilentlyContinue
Add-MpPreference -ExclusionProcess "LibreHardwareMonitor.exe" -ErrorAction SilentlyContinue

# Verify
Get-MpPreference | Select-Object ExclusionPath, ExclusionProcess
```

**Or via Settings UI**:
1. Open Windows Security → Virus & threat protection
2. Manage settings → Add exclusions
3. Add folder: `C:\Program Files\OmenCore`
4. Add folder: `%LOCALAPPDATA%\OmenCore`

### ✅ Option 3: Run as Administrator
**Best for**: One-time verification

Run OmenCore as Administrator to allow WinRing0 initialization without Defender intervention.

---

## Why OmenCore is Safe

### Code Safety Measures

1. **EC Register Allowlist**
   ```csharp
   // Only safe fan control and power registers allowed
   private static readonly HashSet<ushort> AllowedWriteAddresses = new()
   {
       0x2C, 0x2D, 0x2E, 0x2F,  // Fan speed registers
       0xCE, 0xCF,              // Performance mode registers
       0xC0-0xC5                // CPU/GPU power limits
   };
   ```
   - Prevents writes to critical hardware (battery, keyboard, audio)
   - Validated against HP OMEN EC documentation

2. **Read-Only Verification**
   - After each write, values are read back to verify
   - Logs warnings if writes don't take effect
   - No silent failures

3. **Open Source**
   - Full source code available: https://github.com/theantipopau/omencore
   - Community auditable
   - No obfuscation or hidden functionality

4. **No Network Activity**
   - Runs offline (except GitHub auto-update checks)
   - No telemetry or data collection (opt-in only)
   - No remote command execution

---

## Verification

### Verify OmenCore Source
```bash
# Clone and inspect
git clone https://github.com/theantipopau/omencore.git
cd omencore

# All EC access code is here
cat src/OmenCoreApp/Hardware/WinRing0EcAccess.cs | grep AllowedWriteAddresses -A 20

# All writes are logged
cat src/OmenCoreApp/Hardware/FanController.cs | grep "WriteDuty"
```

### Run Antivirus Scan
All releases are available for download and can be scanned:
- https://github.com/theantipopau/omencore/releases
- Upload to VirusTotal: https://www.virustotal.com/

---

## Reporting to Microsoft

If you want to report the false positive to Microsoft:

1. **Via Windows Security**:
   - Settings → Privacy & security → Windows Security
   - Virus & threat protection → Manage settings
   - Find the flagged item and click "Restore"
   - Click "I don't think this is a threat"

2. **Via VirusTotal**:
   - https://www.virustotal.com/gui/home/submit
   - Upload OmenCore executable
   - If negative, submit false positive report to Microsoft

---

## Timeline & v2.5.0 Changes

**Current Status (v2.4.1)**:
- WinRing0 support maintained
- PawnIO recommended but not forced
- Defender warnings documented

**v2.5.0 Improvements**:
- ✅ Automatic PawnIO detection
- ✅ UI displays active backend (PawnIO/WinRing0/WMI/OGH)
- ✅ Settings show "Consider PawnIO" when WinRing0 detected
- ✅ New document: This guide

**v2.6.0 Planning**:
- Consider bundling PawnIO installer
- Deprecate WinRing0 by default (keep for compatibility)

---

## FAQ

**Q: Is OmenCore a virus?**  
A: No. OmenCore is open-source and widely used. The flag is on legitimate hardware access libraries, not OmenCore-specific code.

**Q: Will this harm my computer?**  
A: No. Windows Defender does not actively block OmenCore; it only monitors it. You can safely use OmenCore with these mitigations.

**Q: Should I worry?**  
A: Not necessary. This is a common false positive for hardware monitoring tools. Adding exclusions is the standard fix.

**Q: Why doesn't OmenCore use a different approach?**  
A: Low-level hardware access requires privileged drivers. This is how all hardware monitoring tools work (CPU-Z, GPU-Z, CoreTemp, HWInfo, etc.).

**Q: Can I contribute to fix this?**  
A: Yes! OmenCore uses PawnIO when available. If you have Secure Boot, PawnIO works perfectly without warnings.

---

## Support

- **GitHub Issues**: https://github.com/theantipopau/omencore/issues
- **Reddit**: r/OmenLaptops, r/HPOmen
- **Discord**: OmenCore community server

For false positive reports, include:
- Windows version and build
- OmenCore version
- LibreHardwareMonitor version
- Full Defender warning message
