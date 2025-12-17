# OmenCore Engineering Audit (17 Dec 2025)

Scope constraints (per maintainer request):
- No code or file modifications were made during this audit.
- This document is intended as a v1.5+ hardening roadmap and should be “commit-ready” as an internal engineering note.

Audit focuses:
- Fan curves & thermals
- Performance modes & GPU boosting
- Resource usage
- BIOS lookup & update detection
- Claimed feature ↔ backend mapping

---

## Overview

OmenCore is a WPF/.NET 8 application that combines:
- Sensor monitoring via LibreHardwareMonitor
- HP OMEN control via HP WMI BIOS (CIM / `root\wmi` hpqBIntM interface)
- Optional EC/MSR access via PawnIO or WinRing0
- Optional fallback control via OMEN Gaming Hub (OGH) service proxy

At a high level:
- Fan control is implemented as “policy selection + optional continuous curve enforcement”.
- “Performance modes” currently blend Windows power plans, optional EC register writes, and fan thermal policy changes.
- GPU power boost is driven through HP WMI BIOS (with OGH proxy fallback).
- BIOS update checking is implemented via HP support “wcc-services” lookups + a support-page fallback link.

Key theme: the project is functional, but some labels/claims imply stronger hardware control than what is always applied on real systems (especially when EC access is unavailable or blocked by Secure Boot).

---

## Confirmed Working Behavior (From Code Review)

### Fan & thermal
- Continuous monitoring loop runs in `FanService` and periodically applies the active curve (15s cadence) while refreshing UI telemetry.
- Default fan presets and their curve points are defined in `config/default_config.json`.
- A thermal protection override exists:
  - Warn threshold: 80°C → forces fan speed ≥ ~70% and scales upward
  - Emergency threshold: 88°C → forces 100% fan speed immediately
  - Release hysteresis: release below 75°C
- HP WMI BIOS backend (`HpWmiBios`) includes a 60s heartbeat intended to keep WMI commands “unlocked” on some 2023+ models.
- HP BIOS fan “countdown extension” exists in `WmiFanController` to re-extend the 120s firmware timeout by calling `ExtendFanCountdown()` every 90s when in non-default modes.

### GPU power boost
- GPU power boosting is implemented via HP WMI BIOS command `CMD_GPU_SET_POWER (0x22)` and readback via `CMD_GPU_GET_POWER (0x21)`.
- UI selects among: Minimum / Medium / Maximum / Extended. Extended maps to a higher PPAB byte, but readback can’t distinguish “Maximum vs Extended” because the current telemetry surface is only boolean flags.
- Startup reapplication exists via `SettingsRestorationService` and includes verification via readback flags.

### BIOS detection and update-check UX
- Current BIOS version shown to user is sourced from WMI `Win32_BIOS.SMBIOSBIOSVersion`.
- Update checking is currently performed via `BiosUpdateService.CheckForUpdatesAsync(SystemInfo)`.
  - Attempts HP `support.hp.com/wcc-services/drivers/bySerial?sn=...` (hardcoded `cc=us&lang=en`).
  - If structured data is not available, falls back to a support URL: `https://support.hp.com/drivers?serialnumber=...`.
- The UI opens the returned URL via `Process.Start(... UseShellExecute=true)` (no automatic flashing).

---

## Claimed Hardware Control Mapping (Verify, Don’t Assume)

The README claims the following mapping:

| Feature | README claim | What the code actually does today |
|---|---|---|
| Fans / Thermals | WMI | Primarily HP WMI BIOS via `HpWmiBios` + optional EC via PawnIO/WinRing0 + optional OGH proxy fallback. |
| Performance modes | WMI | Windows power plan + CPU boost setting; optional EC register write (`0xCE`) when EC backend exists; fan thermal policy via WMI/OGH/EC wrappers. |
| GPU power | WMI | HP WMI BIOS `SetGpuPower` with OGH proxy fallback. |
| Intel undervolt | PawnIO (capability-gated) | Capability detection gates MSR features; settings restoration uses `MsrAccessFactory`. (Audit did not deeply validate undervolt correctness beyond gating.) |
| AMD Curve Optimizer | PawnIO | Not fully validated in this pass (not in the primary focus files read), but capability detection treats PawnIO as the Secure Boot-friendly path for advanced CPU features. |
| Zone RGB | WMI | Keyboard lighting routes through WMI BIOS on supported models; EC lighting writes are flagged as risky and are opt-in in PawnIO backend. |
| Per-key RGB | HID/OpenRGB | Not validated in this pass. |
| EC fallback | PawnIO (opt-in) | PawnIO EC backend exists and uses allowlists; WinRing0 EC backend also exists. |

Main correctness gap: “Performance modes via WMI” is only partially true. Fan thermal policy selection uses WMI, but actual CPU/GPU power limit writes are EC-based (and only happen if an EC backend is available).

---

## Issues Found

### 1) FanService assumes `SetFanSpeed(0)` means “return BIOS control”
In `FanService.CheckThermalProtection()`, when thermal protection releases and no preset exists, the code does:
- `SetFanSpeed(0)` with a comment “0 = let BIOS control”.

But the WMI fan controller implementation treats 0% as a literal fan level write:
- `WmiFanController.SetFanSpeed(int percent)` clamps percent and writes `SetFanLevel(0,0)`.

This is a potential safety bug:
- On some firmware, `SetFanLevel(0,0)` may mean “minimum/stop” rather than “release to auto”.
- The *actual* API for “return to auto” exists as `IFanController.RestoreAutoControl()`, but FanService does not call it here.

Impact:
- In edge cases (especially after emergency protection), the system may be left in an unexpected fan control mode.

### 2) Default preset “Max” forces GPU power to Maximum
`WmiFanController.ApplyPreset()` for Max preset:
- Sets fan mode Performance
- Enables max fan speed
- Also calls `_wmiBios.SetGpuPower(GpuPowerLevel.Maximum)` “for maximum cooling”

This is counterintuitive:
- Higher TGP/PPAB increases heat generation; it’s not inherently “cooling”.
- Users selecting “Max fans” may be trying to reduce temps/noise ramp-up, not increase GPU power.

Risk:
- Could increase sustained thermals on battery/AC depending on platform behavior.

### 3) Continuous fan loop polls relatively aggressively and always reads fan speeds
Defaults:
- `monitoringIntervalMs` is 750ms in `default_config.json`.
- FanService adaptive polling may slow to 5s after stability, but in the “not stable” phase it can wake often.
- FanService reads temps and also calls `_fanController.ReadFanSpeeds()` every loop iteration.

Concerns:
- LibreHardwareMonitor updates can induce DPC latency.
- Clearing and re-adding telemetry collections each loop increases GC/UI churn.
- The comment “Read fan speeds less frequently” does not match current implementation.

### 4) Performance mode semantics are inconsistent with UI/README language
`PerformanceModeService.Apply()`:
- Always sets Windows power plan.
- Writes EC limits only if `PowerLimitController` exists.
- Still logs “Power limits applied: CPU=XW, GPU=YW” when EC is available, but when EC is unavailable it logs “Windows power plan only”.

Where this bites:
- On Secure Boot systems without PawnIO/WinRing0, EC writes won’t happen.
- Users may believe CPU/GPU wattage limits are applied even when they are not.

Additionally:
- `PowerLimitController` uses EC register `0xCE` and allowlist includes it, but the file itself warns these addresses are hardware-specific.

### 5) BIOS update check has fragile/limited “latest version” truthiness
`BiosUpdateService`:
- Best-case: parses structured JSON and extracts a BIOS version.
- Common fallback: returns “HP’s API doesn’t provide direct BIOS lookup…”, and sets `DownloadUrl` to a support URL.

Risks:
- Hardcoded `cc=us&lang=en` is region-biased.
- Response structure is variable; parsing can silently fail.
- When structured data does exist, the returned `downloadUrl` may be a direct SoftPaq download rather than a human driver page.

UX mismatch risk:
- UI label “Download BIOS Update” may take users to a direct executable rather than the exact HP BIOS download page with release notes.

### 6) Two BIOS-update services exist with overlapping responsibilities
- `BiosUpdateService` (used by Settings UI)
- `HpCmslService` (HPCMSL PowerShell module orchestrator)

The CMSL path is closer to “official toolchain” and likely more reliable, but it appears not to be wired into the Settings BIOS check UI. This increases maintenance cost and can confuse future contributors.

---

## Fan & Thermal Analysis

### Default curves (from config/default_config.json)
- Quiet:
  - 40°C → 20%
  - 70°C → 45%
  - 85°C → 65%
- Balanced:
  - 40°C → 30%
  - 70°C → 60%
  - 90°C → 80%
- Performance:
  - 40°C → 40%
  - 70°C → 75%
  - 95°C → 100%
- Max: 100% at all temps

Sensible/safe?
- The curves themselves are generally conservative at mid temps and aggressive at the top.
- The thermal protection override (80/88°C) effectively supersedes “Quiet/Balanced” once the system is hot, which is good for safety.

Sustained-load behavior:
- Curve application cadence is 15s. This is relatively slow for fast thermal transients.
- Thermal protection helps prevent worst-case slow response, but you can still see:
  - oscillation around 80°C if workload hovers near threshold
  - stepwise fan changes due to “nearest lower point” curve selection rather than interpolation

Oscillation/noise risks:
- Hysteresis exists, but the curve itself updates only every 15s.
- The thermal protection has a 5°C release hysteresis, which helps avoid chatter.

Critical safety observation:
- Using `SetFanSpeed(0)` as “restore BIOS control” is not guaranteed to be safe on WMI backend.

Recommendations (non-breaking):
- Treat `RestoreAutoControl()` as the canonical path to exit manual override.
- Consider curve interpolation between points (optional) or at least clamp rate-of-change for acoustics.
- Reduce UI churn by updating telemetry only when values meaningfully change.

---

## Performance Mode & GPU Power Analysis

### Does Performance Mode increase CPU/GPU power limits?
- Sometimes.
- If EC access is available (PawnIO/WinRing0), `PowerLimitController` attempts to write EC register `0xCE` (simplified mode).
- If EC access is not available (common on Secure Boot systems without PawnIO), there is no direct CPU/GPU wattage programming; only Windows power plan + processor boost index changes.

### Alignment with HP OMEN Gaming Hub
- Fan thermal policy selection via HP WMI BIOS is plausibly close to OMEN Hub behavior.
- GPU “Dynamic Boost / PPAB” control via WMI BIOS is plausible and matches common community tooling.
- Direct EC power-limit writes are **not** guaranteed to match OMEN Hub across models.

### Safe apply/revert
- Apply:
  - WMI GPU boost calls are applied immediately.
  - Startup restoration verifies GPU boost via readback flags.
- Revert:
  - There is no explicit “revert to OEM default on app exit/uninstall”.
  - Some HP firmware resets settings on sleep/reboot; the UI already warns about this.

Major concern:
- “Max cooling” preset enabling maximum GPU power is likely not aligned with user intent.

Recommended guardrails:
- Make “Max cooling” strictly a fan behavior modifier; do not change GPU power unless user explicitly opts into coupling.
- For performance modes, clearly communicate in UI and logs when EC power control is unavailable so users don’t assume wattage enforcement.

---

## BIOS Lookup & Update Detection Review

### Current behavior
- Current BIOS version:
  - WMI: `Win32_BIOS.SMBIOSBIOSVersion`
- Product/model identifiers:
  - `Win32_BaseBoard.Product` stored as `SystemInfo.ProductName`
  - `Win32_ComputerSystemProduct.SKUNumber` stored as `SystemInfo.SystemSku` (fallback to IdentifyingNumber)
  - Serial number from `Win32_BIOS.SerialNumber`
- Update lookup:
  - Attempts `support.hp.com/wcc-services/drivers/bySerial?sn=...&cc=us&lang=en`
  - Parses a subset of possible JSON schemas
  - If not available: returns a support-page URL for the user

### Risks
- Fragile parsing:
  - HP response schemas vary; code tries a couple of paths, but this is not future-proof.
- Regional mismatch:
  - Hardcoded `cc=us&lang=en` can cause wrong or incomplete results.
- Misleading UX:
  - Returning `downloadUrl` could be a direct SoftPaq binary link; the UI suggests “Download BIOS Update”, which may bypass release notes context.
- Safety/trust:
  - Opening HP URLs is generally safe, but any direct executable link should be treated carefully.

### Recommended safer approach (documentation + guardrails > automation)

Target UX (matches maintainer spec):
1) Show detected current BIOS version (e.g., F14)
2) On “Check for BIOS updates”:
   - Prefer an official, structured method
   - If not available, provide a stable support-page link
3) If newer BIOS exists:
   - Alert: “BIOS update available: F15”
   - Link to the exact HP driver page (not a direct flashing action)
   - Warnings and guidance

Recommended implementation strategy for v1.5+ hardening:
- Prefer HPCMSL when installed:
  - Use `Get-HPBIOSUpdates -Platform <ProductId>` as the authoritative latest BIOS query.
  - Return SoftPaq ID + release date + HP support page link.
- Without HPCMSL:
  - Do not attempt brittle scraping.
  - Provide `https://support.hp.com/drivers?serialnumber=...` and label button “Open HP Support Page” (not “Download”).
- Normalize BIOS version comparisons:
  - Compare within the same family (e.g., F.xx) if possible.
  - Avoid pure string inequality checks.

Security posture:
- Never auto-download or auto-run BIOS update packages.
- If SoftPaq download is offered, it should be explicit and clearly labeled as an HP executable.

---

## Performance & Resource Concerns

### FanService vs HardwareMonitoringService duplication
- `HardwareMonitoringService` already implements caching, change detection, and low-overhead mode.
- `FanService` runs its own loop and calls temperature read APIs separately.

Potential impact:
- More frequent LibreHardwareMonitor updates than necessary.
- Increased wakeups and UI churn.

Suggested non-breaking direction:
- Use a shared source of “latest temps” (e.g., last monitoring sample) for fan logic.
- Or increase FanService’s effective polling cadence and only read fan speeds less often.

### UI churn
- Fan telemetry is rebuilt every loop by clearing and re-adding.
- Consider only updating if values changed beyond a threshold.

### Timer lifecycle
- `HpWmiBios` heartbeat timer and `WmiFanController` countdown timer should be audited for disposal paths.
  - WmiFanController disposes its timer.
  - Ensure HpWmiBios disposes heartbeat on Dispose (verify in the remaining portion of HpWmiBios).

---

## Suggested Fixes (Non-Breaking)

(These are recommendations only; no code changes were applied.)

1) Make “restore auto fan control” explicit
- In FanService, replace any “`SetFanSpeed(0)` means auto” assumptions with `RestoreAutoControl()`.
- Add a small “cooldown” after emergency max-fan to avoid oscillation.

2) Decouple Max fan preset from GPU power
- Keep Max = fan behavior only.
- Preserve user-selected GPU power boost level instead of forcing Maximum.

3) Clarify performance mode semantics
- When EC power control is unavailable, UI should say:
  - “Performance mode: Windows power plan + fan policy only (no EC wattage control).”
- Avoid logging “Power limits applied” unless verified.

4) Reduce FanService overhead
- Read fan speeds every N loops or every few seconds rather than every tick.
- Apply UI updates only when values change beyond a threshold.
- Consider raising default monitoringIntervalMs for fan loop if it doesn’t harm user-perceived responsiveness.

5) BIOS update check: prefer stable support link UX
- Rename “Download BIOS Update” action to “Open HP Support Page” unless the URL is confirmed to be a driver page.
- Add explicit warning text if a direct SoftPaq executable link is returned.
- Prefer HPCMSL if installed; otherwise avoid brittle scraping.

---

## Code Optimisation & Cleanup Opportunities

- Consolidate duplicate “hardware polling loops” (fan loop + monitoring loop) so only one subsystem forces LibreHardwareMonitor updates.
- Reduce allocations:
  - Avoid per-loop `.ToList()` and repeated clear/add patterns.
  - Avoid frequent creation of `ThermalSample` if UI isn’t updating.
- Normalize backend selection messaging:
  - Some comments imply “OGH Proxy preferred for 2023+” while runtime selection often prefers WMI BIOS.
  - Ensure documentation matches actual selection logic.

---

## Feature Opportunities (Roadmap-Level)

- Per-fan curves (CPU fan vs GPU fan):
  - Current WMI control tends to apply the same level to both fans.
  - If firmware supports independent fan levels, expose split curves.
- “Curve apply verification”:
  - v1.5 changelog mentions closed-loop verification; the primitives exist (`TestCommandEffectiveness`, `VerifyCommandEffect`), but FanService does not yet consume them.
  - Integrate verification to detect ineffective WMI models and prompt switching backend.
- BIOS update experience hardening:
  - A dedicated “BIOS update” panel that always provides the HP support page link and optionally shows SoftPaq metadata.

---

## Testing & Validation Suggestions

Fan & thermals:
- Idle stability test:
  - Verify no oscillation around 75–85°C thresholds and that fans don’t chatter.
- Sustained load test (30–60 min):
  - CPU-only (Cinebench loop)
  - GPU-only (FurMark / Unigine)
  - Combined (game + CPU load)
  - Confirm thermal protection triggers and releases correctly.
- Safety test for “restore”:
  - Enter thermal protection and verify the system returns to BIOS auto control.

Performance modes:
- On EC-capable systems:
  - Validate `0xCE` effects with telemetry (CPU package power, GPU power) and stability.
- On Secure Boot + no EC:
  - Confirm UI clearly indicates “Windows power plan only”.

GPU power:
- Verify that readback flags (`customTgp`, `ppab`) match UI state.
- Validate “Extended” does not mislead users when it’s indistinguishable from Maximum in readback.

BIOS updates:
- Test with:
  - Serial present and absent
  - US and non-US locales (VPN or Windows region)
  - HP API returning HTML vs JSON

---

## Quick Wins (High Value / Low Risk)

1) Replace `SetFanSpeed(0)` auto-control assumption with `RestoreAutoControl()`
2) Stop forcing GPU power to Maximum in “Max fan” preset
3) Reduce fan telemetry update churn (update only on meaningful change)
4) Rename BIOS action to “Open HP Support Page” unless a driver-page URL is guaranteed
5) Clearly annotate performance mode as “EC wattage control available/unavailable” based on backend

---

## Appendix: Key Files Reviewed

- Fan logic:
  - `src/OmenCoreApp/Services/FanService.cs`
  - `config/default_config.json`
  - `src/OmenCoreApp/Hardware/WmiFanController.cs`
  - `src/OmenCoreApp/Hardware/HpWmiBios.cs`
  - `src/OmenCoreApp/Hardware/FanControllerFactory.cs`
- Performance/power:
  - `src/OmenCoreApp/Services/PerformanceModeService.cs`
  - `src/OmenCoreApp/Services/PowerPlanService.cs`
  - `src/OmenCoreApp/Hardware/PowerLimitController.cs`
- BIOS detection/updates:
  - `src/OmenCoreApp/Services/SystemInfoService.cs`
  - `src/OmenCoreApp/Services/BiosUpdateService.cs`
  - `src/OmenCoreApp/Services/HpCmslService.cs`
  - `src/OmenCoreApp/ViewModels/SettingsViewModel.cs`
- Startup restoration:
  - `src/OmenCoreApp/Services/SettingsRestorationService.cs`
- Monitoring:
  - `src/OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs`
  - `src/OmenCoreApp/Services/HardwareMonitoringService.cs`
