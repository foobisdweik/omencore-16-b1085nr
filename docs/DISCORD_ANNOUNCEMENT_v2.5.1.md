## 🎉 OmenCore v2.5.1 - Fan "Max" Reliability & Diagnostics

**Download:** https://github.com/theantipopau/omencore/releases/tag/v2.5.1

---

## 🌀 FAN CONTROL IMPROVEMENTS

### ✅ **Max Preset Reliability**
- **Real-time verification**: UI shows 100% only after confirming fans are actually at max
- **Retry logic**: Automatic retries with BIOS-specific fallbacks
- **Accurate UI**: No false 100% indicators

### ✅ **Fan Reset on Exit**
- **BIOS restoration**: Fans return to Windows/BIOS defaults on app close
- **Clean shutdown**: Prevents stuck manual fan speeds

### ✅ **Enhanced Diagnostics**
- **Fan verification**: Diagnostic exports include max verification results
- **Per-fan control**: Individual fan troubleshooting

---

## 🛡️ SAFETY & RELIABILITY

### ✅ **Fan Curve Safety**
- **Thermal clamping**: Emergency 100% fans at 88°C
- **Safe minimums**: Progressive speeds at high temperatures

### ✅ **EDP Throttling**
- **MSR detection**: CPU registers for accurate throttling
- **Secure Boot**: Compatible with PawnIO MSR access

---

## 🐛 CRITICAL FIXES

- **Max preset accuracy**: Fixed false 100% display
- **WMI/EC reliability**: Retry loops and BIOS fallbacks
- **Temp freezing**: Fixed during drive sleep
- **RAM/CPU display**: Fixed 0/0 GB and 0°C issues

---

## 📦 DOWNLOADS

**Windows:** `OmenCoreSetup-2.5.1.exe` (105.67 MB) | `OmenCore-2.5.1-win-x64.zip` (108.96 MB)  
**Linux:** `OmenCore-2.5.1-linux-x64.zip` (60.94 MB)

---

## 🔒 VERIFY (SHA256)

```
FB7391404867CABCBAE14D70E4BD9D7B31C76D22792BB4D9C0D9D571DA91F83A  OmenCoreSetup-2.5.1.exe
05055ABAC5ABBC811AF899E0F0AFEE708FE9C28C4079015FAFE85AA4EFE1989F  OmenCore-2.5.1-win-x64.zip
AD07B9610B6E49B383E5FA33E0855329256FFE333F4EB6F151B6F6A3F1EBD1BC  OmenCore-2.5.1-linux-x64.zip
```

---

## ⚡ NEXT: v2.6.0

- 📊 Advanced monitoring dashboard
- 🎨 Enhanced RGB lighting
- 🔍 MSI Afterburner integration