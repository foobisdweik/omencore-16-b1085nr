# v2.1.0 Bug Report Tracker

**Report Date:** January 3, 2026  
**Source:** Reddit user feedback  
**Version:** 2.1.0  
**Last Updated:** January 3, 2026

## Critical Bugs (App Crashes/Non-Functional)

### 1. ✅ Fan Preset 'Max' Blocked by EC Safety Allowlist
**Status:** 🟢 FIXED  
**Reported:** January 3, 2026  
**Symptom:** Fan preset 'Max' fails to apply with error "EC write to address 0x2C is blocked for safety"  
**Root Cause:** Missing EC registers 0x2C/0x2D from safety allowlist. These are XSS1/XSS2 registers (Fan 1/2 set speed %) used by OmenMon-style fan control on newer OMEN models (2022+)  
**Fix Applied:** Added 0x2C and 0x2D to `AllowedWriteAddresses` in both `PawnIOEcAccess.cs` and `WinRing0EcAccess.cs`  
**Files Modified:**
- `src/OmenCoreApp/Hardware/PawnIOEcAccess.cs`
- `src/OmenCoreApp/Hardware/WinRing0EcAccess.cs`

**Technical Details:**
```
EC Register Map (from omen-fan project):
0x2C - Fan 1 set speed % (XSS1) - OmenMon-style, newer models
0x2D - Fan 2 set speed % (XSS2) - OmenMon-style, newer models
0x2E - Fan 1 speed % (legacy, older models)
0x2F - Fan 2 speed % (legacy, older models)
```

The `FanCleaningService` uses registers 0x2C/0x2D for max fan control on newer models, but these weren't in the safety allowlist, causing `UnauthorizedAccessException`.

**Error Log:**
```
2026-01-03T13:58:59.7919193+02:00 [ERROR] Failed to apply preset: EC write to address 0x2C is blocked for safety. Only approved addresses can be written. Allowed: 0x2E, 0x34, 0x35, 0x44, 0x45, 0x46, 0x4A, 0x4B, 0x4C, 0x4D, 0xB0, 0xB1, 0xCE, 0xCF, 0x96: System.UnauthorizedAccessException: EC write to address 0x2C is blocked for safety.
```

---

### 2. ✅ Linux CLI Crashes on Any Command
**Status:** 🟢 FIXED  
**Reported:** January 4, 2026  
**Symptom:** Running any omencore-cli command (battery status, fan, etc.) crashes with `ArgumentException: An item with the same key has already been added. Key: --version`  
**Root Cause:** System.CommandLine automatically adds `--version` option to `RootCommand`. We had duplicate option conflicts:
1. Global `--json`/`-j` conflicted with StatusCommand's local `--json`/`-j`
2. Verbose `-v` could conflict with version `-V` in some parsing scenarios
3. Manual `--version` handling worked but the parser initialization still tried to register the option

**Fix Applied:** 
- Removed duplicate global `--json` option (StatusCommand has its own)
- Simplified verbose to `--verbose` only (removed `-v` alias to avoid confusion with `-V`)
- Kept manual `--version`/`-V` handling before parsing to provide custom version output

**Files Modified:**
- `src/OmenCore.Linux/Program.cs`

**Error Log:**
```
Unhandled exception. System.ArgumentException: An item with the same key has already been added. Key: --version
   at System.Collections.Generic.Dictionary`2.TryInsert(TKey, TValue, InsertionBehavior)
   at System.Collections.Generic.Dictionary`2.Add(TKey, TValue)
   at System.CommandLine.Parsing.StringExtensions.ValidTokens(Command)
   at System.CommandLine.Parsing.StringExtensions.Tokenize(IReadOnlyList`1, CommandLineConfiguration, Boolean)
   at System.CommandLine.Parsing.Parser.Parse(IReadOnlyList`1, String)
   ...
```

---

## Testing Notes

After fix:
- Fan presets should now work correctly on newer OMEN models (2022+)
- Both OmenMon-style (0x2C/0x2D) and legacy (0x2E/0x2F) fan registers are now supported
- No impact on other EC operations - only fan control registers added

## Affected Models

**Primary:** 2022+ OMEN laptops using OmenMon-style EC registers  
**Also Benefits:** All models - adds redundancy with both legacy and new register support
