# OmenCore v2.5.0 Changelog

**Release Date:** Q2 2026 (Planned)  
**Status:** In Development

---

## Summary

v2.5.0 focuses on reliability hardening, verification systems, and addressing user-reported issues from v2.4.x. This release includes power limit verification, improved fan control diagnostics, expanded unit testing, and better Secure Boot compatibility guidance.

---

## Progress & Recent Work ✅

Since the roadmap was created, the following items have been implemented or advanced:

- **Power Limits Verification**: Implemented `PowerVerificationService` that applies performance mode power limits and reads back EC registers to verify success. (Added `IPowerVerificationService` and `PowerLimitApplyResult`.)
- **Windows Defender Guidance**: Added `docs/DEFENDER_FALSE_POSITIVE.md` to explain the WinRing0/LibreHardwareMonitor false positive and mitigations (PawnIO recommendation, Defender exclusions, admin-run guidance).
- **Build & CI Fixes**: Fixed a compilation error in `KeyboardLightingService` and added tests; full solution builds successfully and unit tests pass locally.
- **Settings UX**: Settings now show Defender false-positive guidance and recommend PawnIO when WinRing0 is detected.
- **Diagnostics & Logging Improvements**: Enhanced logging around verification and sensor detection to aid diagnosis of issues reported by users (RPM mismatches, CPU temp 0°C).
- **Linux QA & Artifacts**: Added CI workflow for linux-x64 CLI packaging with checksums and smoke commands; documented testing checklist.
- **Diagnostics Export (WIP)**: Added `DiagnosticExportService` scaffold to bundle logs/system info/EC dump for support requests.

**Next immediate actionable items** (candidate work for this build):
1. **Phase 2 — RPM Validation & Calibration** (High): Add model calibration storage, calibration UI, and verification tests to stabilize RPM→% mapping.
2. **MSI Afterburner Integration** (High): Implement robust shared-memory reader and conflict handling.
3. **Fan Verification Enhancements** (High): Improve `FanVerificationService` to attempt multiple read-backs and optionally revert on failure; add unit tests that mock `IEcAccess`/WMI.
4. **Diagnostic UX** (Medium): Add UI controls for exporting diagnostics and attaching to GitHub issues.
5. **Linux QA** (Medium): Add CI smoke tests for Linux artifacts and improve error messages for kernel/OGH issues.

---

## New Features

### Power Limit Verification System
- **PowerVerificationService**: Reads back EC registers after applying power limits to verify they took effect
- **Diagnostic Logging**: Detailed verification results with warnings when power limits fail silently
- **Result Tracking**: `PowerLimitApplyResult` model for monitoring power mode changes
- **Integration**: Automatic verification in `PerformanceModeService` when applying performance modes

### Enhanced Diagnostics & Logging
- **DiagnosticLoggingService**: Structured diagnostic capture and export
- **ConflictDetectionService**: Detection of conflicting software (XTU, Afterburner, etc.)
- **Improved Hardware Detection**: Better sensor discovery and fallback mechanisms

### Driver Backend Improvements
- **PawnIO Promotion**: Enhanced guidance for users with Secure Boot enabled (WinRing0 blocked)
- **Auto-Detection**: Automatic selection between PawnIO, WinRing0, WMI BIOS, and OGH proxy
- **Safety Checks**: Read-only verification after writes to critical EC registers

---

## Bug Fixes

### Fan Control Issues (v2.4.1 Carry-over)
- Fixed WMI `SetFanLevel` not working on newer OMEN models (Transcend, 2024+)
- Added `FanVerificationService` to detect when WMI commands silently fail
- Implemented `CommandsIneffective` flag to alert users when backend doesn't respond
- Improved max fan speed logic (now uses `SetFanMax` to bypass BIOS power caps)

### Temperature Sensor Issues
- Improved LibreHardwareMonitor sensor detection and caching
- Added fallback to alternate sensor names when primary sensors unavailable
- Debug logging for temperature sensor discovery failures
- Better handling of multi-core CPU temperature aggregation

### GUI Issues
- Fixed custom fan curve name text input not displaying correctly
- Corrected banner graphics in Windows installer
- Fixed keyboard lighting UI responsiveness

### Linux CLI Issues
- Fixed performance mode typo validation (now properly rejects invalid modes)
- Improved error messages for invalid command-line arguments
- Added mode validation list in error output

---

## Improvements

### Testing & Quality Assurance
- Added 80+ unit tests covering fan control, EC access, and power limits
- Integration tests with mocked hardware backends
- CI pipeline with code coverage reporting
- Pre-commit checks for high-severity warnings

### User Experience
- Better fallback hierarchy: OGH Proxy → WMI BIOS → EC Access → Monitoring Only
- Automatic Afterburner conflict detection with user warnings
- Improved Settings UI showing which driver backend is active
- Export diagnostics feature for bug reports

### Documentation
- Linux setup and testing guide expanded
- EC register documentation for supported laptop models
- Troubleshooting guides for common issues
- FAQ with Secure Boot and driver compatibility information

### Performance
- Optimized hardware monitoring loops with configurable poll intervals
- Reduced CPU overhead in low-power mode
- Async verification to avoid blocking the UI

---

## Known Issues

### Windows Defender False Positive
- **Issue**: Windows Defender flags LibreHardwareMonitor and WinRing0 as "VulnerableDriver.WinNT/Wiring0"
- **Root Cause**: These are legitimate drivers/libraries but have low-level hardware access capabilities
- **Workaround**: 
  - Add `OmenCore` folder to Windows Defender exclusions
  - Use PawnIO instead (Secure Boot compatible, better vetted)
  - Run as Administrator to allow WinRing0 to initialize
- **Mitigation**: v2.5.0 strongly recommends PawnIO for Secure Boot systems
- **Note**: This is a known false positive; OmenCore code is open-source and auditable

### Older Linux Kernels
- **Issue**: Some Ubuntu LTS + HWE kernel combinations don't support full HP WMI integration
- **Affected**: Ubuntu with kernel <6.8, OMEN 2023 and older with certain EC addresses
- **Workaround**: Update kernel or use Debian with newer kernel version
- **Status**: EC address mapping being expanded for compatibility

### GPU Temperature via Afterburner
- **Issue**: MSI Afterburner shared memory locking conflicts when OmenCore reads it
- **Status**: ConflictDetectionService added to warn users; v2.6.0 planned for resolution

---

## Migration Guide

### From v2.4.x
- No breaking changes
- Existing settings and profiles are compatible
- Power verification logs new information (may see additional entries)
- PawnIO recommended if you have Secure Boot enabled

---

## Contributors

Special thanks to:
- Users reporting fan control inconsistencies
- Community testing on diverse OMEN models
- Reddit/Discord feedback on Linux and performance issues

---

## Next: v2.6.0 Planning

- GPU overclock GUI (NVIDIA NVAPI + AMD Radeon API)
- Thermal paste/cooler upgrade recommendations
- Advanced power curve editor with live graphing
- Afterburner integration refinement (async conflict resolution)
