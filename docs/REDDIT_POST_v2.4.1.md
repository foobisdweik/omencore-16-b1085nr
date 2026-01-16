# [RELEASE] OmenCore v2.4.1 - Critical Bug Fix & Improvements

**Date:** January 16, 2026  
**Download:** https://github.com/theantipopau/omencore/releases/tag/v2.4.1

---

## 🎯 What's New in v2.4.1

This release focuses on critical reliability fixes and improvements from the v2.4.1 roadmap.

### ⚠️ CRITICAL FIXES

- **WinRing0 reliability**: Corrected IOCTL codes and implemented ACPI EC protocol (ports 0x62/0x66) for robust EC access
- **Diagnostics**: Added `DiagnosticLoggingService` to capture raw EC register snapshots and exports for troubleshooting
- **MSI Afterburner detection**: `ConflictDetectionService` added and updated UI to reflect telemetry when MAB is present
- **RPM accuracy**: Fan RPM now read from EC registers (0x34/0x35) with RpmSource indicator in UI

---

## 📦 Download Options

**Windows:**
- **Installer**: `OmenCoreSetup-2.4.1.exe` - Full installation
- **Portable**: `OmenCore-2.4.1-win-x64.zip` - No installation needed

**Linux:**
- **x86_64**: `OmenCore-2.4.1-linux-x64.zip`
- **ARM64**: `OmenCore-2.4.1-linux-arm64.zip`

---

## 🔒 Verify Your Downloads

SHA256 checksums:

```
B5BFFFBD0BA75B1AA27508CDBF6F12B5C7A4A506877484308ABD20BA181AD36F  OmenCoreSetup-2.4.1.exe
F6BB04CAF67E45D984BF8D1F852600808326A13682F5A052E131CBF5A91BDC71  OmenCore-2.4.1-win-x64.zip
E24EB0A8956F62C731488BEE21037F424789B3550BF56A3481CF9CF9AF135947  OmenCore-2.4.1-linux-x64.zip
56BA1FB1499BAB9854FD083A66494F1D9E96E5D89E783C27C1080EC19BBD53D9  OmenCore-2.4.1-linux-arm64.zip
```

---

## ⏭️ What's Next (v2.5.0)

- 🔍 MSI Afterburner integration improvements
- 🧪 More unit tests for fan curve evaluation and EC access
- 📈 Performance improvements and telemetry enrichment

---

**Questions?**: https://github.com/theantipopau/omencore/issues

Thanks to everyone who tested and reported bugs — your help keeps OmenCore safe and reliable!