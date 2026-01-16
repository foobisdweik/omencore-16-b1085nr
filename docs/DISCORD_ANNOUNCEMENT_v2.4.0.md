## 🎉 OmenCore v2.4.0 - Major Stability & Safety Release

**Download:** https://github.com/theantipopau/omencore/releases/tag/v2.4.0

---

## 🚨 CRITICAL FIXES

**Fan Runaway (GitHub #49):** Fans accelerated beyond 100% → Fixed with 3-layer protection (validation, clamping, hardware limits)

**UI Freeze Gaming:** 20-30 min freezes → Fixed with WMI timeouts (5s) + dispatcher throttling

**EC Address Blocking:** 0x2C errors on older Omen models → Fixed with fallback logic

---

## ✨ OTHER IMPROVEMENTS

✅ Quiet Mode thermal tuning (better cooling)  
✅ Linux RAM display fixed ("8.2 / 16.0 GB" format)  
✅ Settings reorganized into 5 logical tabs  
✅ Dedicated Diagnostics tab  
✅ Collapsible logs panel  

---

## 📦 DOWNLOADS

**Windows:** `OmenCoreSetup-2.4.0.exe` (100.51 MB) | `OmenCore-2.4.0-win-x64.zip` (103.78 MB)  
**Linux:** `OmenCore-2.4.0-linux-x64.zip` (66.24 MB) | `OmenCore-2.4.0-linux-arm64.zip` (35.80 MB)

---

## 🔒 VERIFY DOWNLOADS (SHA256)

```
91DAF951A8E357B90359E7C1557DC13EF3472F370F0CB30073C541244FCAE32C  OmenCoreSetup-2.4.0.exe
18CEB337EB9FA99604F96A352E48744601703046DEA54528BDDFD666E35F0DE1  OmenCore-2.4.0-win-x64.zip
6C13F67F377D7140ECE28DABAC77C9C0267636BE732E87512AED466D7B0DE437  OmenCore-2.4.0-linux-x64.zip
60BF36CCECC576642830DC8E85AD747A9D534E491984A5445E3BDB9A2AFE5408  OmenCore-2.4.0-linux-arm64.zip
```

**Verify:** `certUtil -hashfile <file> SHA256` (Windows) | `sha256sum <file>` (Linux/Mac)

---

## ⚡ WHAT'S NEXT

**v2.5.0 Planned Features:**
- 🔍 MSI Afterburner auto-detection
- 🧪 Unit tests for fan control
- 📊 Improved CI/CD with strict warnings

---

**Questions?** Report issues: https://github.com/theantipopau/omencore/issues  
**Need help?** Check the docs: https://github.com/theantipopau/omencore#readme
