# Fan "Max" Diagnostic Guide

**Status:** Draft

---

## Summary 🔍
Users report that selecting the **"Max"** fan preset does not always increase fan RPMs despite logs showing the preset applied. Multiple cases since v2.3.2. The app often reports EC-based fan writes succeeded (via PawnIO/WinRing0), but we frequently lack RPM confirmation because LibreHardwareMonitor reports no fan sensors.

## Key observed log lines
- "Fan preset 'Max' applied via EC (WinRing0)" (shows UI/action executed)
- "Preset 'Max' using maximum fan speed (curve disabled)"
- "[Monitor] [FanDebug] No fan sensors found via LibreHardwareMonitor. Hardware: []" (no RPM data)
- "Read-back verification failed. Expected mode: 0x2, got: 0x0" (EC register read-back mismatch)
- "🔧 Entered fan diagnostic mode - curve engine suspended" (user used in-app diagnostic)

Relevant logs to include in diagnostics ZIP:
- `%LocalAppData%\OmenCore\OmenCore.log`
- `%LocalAppData%\OmenCore\HardwareWorker.log` (critical; contains low-level EC results)

---

## Reproduction steps for reporter 🧪
1. Turn on **Debug** logging: Settings → Logging → **Debug**.
2. Note local time and timezone.
3. In the app, apply **Fan Preset → Max** (or use hotkey/cmd to apply). Wait 10s, reapply once or twice.
4. If possible, perform a small CPU load (e.g., run a stress tool) to check if fans respond to thermal load.
5. Export diagnostics (Settings → Diagnostics → Export Diagnostics → **Compressed ZIP**) with these options enabled:
   - Hardware Monitoring Data
   - Application Logs (last 1000 entries)
   - Fan Calibration Data
6. Attach the ZIP or paste the `HardwareWorker.log` and `OmenCore.log` here.

Optional quick test: temporarily stop OGH services (`sc stop HPOmenCap` and kill `OmenCap`) then reapply **Max** and attach logs (warn user to restart OGH afterwards).

---

## Developer triage checklist ✅
- [ ] Inspect `HardwareWorker.log` for the exact EC calls made when applying Max (look for `SetMaxSpeed`, `SetFanSpeed(100)`, `SetFanMax(true)`) and their return values.
- [ ] Verify whether EC register writes (REG_FAN_BOOST, REG_OMCC, REG_FAN_STATE) are followed by read-back checks; log exact returned register values.
- [ ] If EC writes return success but RPMs don't change: determine whether the model requires a different register sequence (e.g., toggle `REG_FAN_BOOST` then set duty, or write both percent and boost registers).
- [ ] Add robust retry and verification sequence for Max mode (ex: SetFanMax(true) → wait 50–150ms → SetFanSpeed(100) → read RPMs → retry up to N times with backoff).
- [ ] Add explicit diagnostic mode that attempts SetFanMax while temporarily reducing OGH interference (or logs OGH status and advises user to stop OGH for a test).
- [ ] Add higher-fidelity logs with timestamps and attempt counters (e.g., `SetFanMax attempt 1 returned true; rpmRead=0; retrying...`).
- [ ] Add a telemetry event for failure-to-achieve-max (counts per model + presence of PawnIO/WinRing0/OGH) to triage model-specific issues.
- [ ] Unit tests / integration test scaffolding to simulate EC returning mismatched read-backs and verify retry logic.

---

## Immediate mitigation suggestions (user-facing)
- Recommend installing `PawnIO` for Secure Boot systems when WinRing0 is blocked (we already log this guidance).
- Recommend the sleep/resume workaround as a temporary workaround (users reported it sometimes restores fan control).
- Add a note in Settings → Diagnostics that instructs users how to stop OGH services for isolated testing.

---

## Sample log excerpts (for copy/paste)
- "2026-01-20T19:07:40.6526913-05:00 [INFO] Fan preset 'Max' applied via EC (WinRing0)"
- "2026-01-20T19:07:41.1139235-05:00 [INFO] [Monitor] [FanDebug] No fan sensors found via LibreHardwareMonitor. Hardware: []"
- "2026-01-21T08:34:33.4761588+08:00 [WARN] Read-back verification failed. Expected mode: 0x2, got: 0x90"
- "2026-01-20T19:44:28.7197650-05:00 [INFO] 🔧 Entered fan diagnostic mode - curve engine suspended"

---

## Representative case — Elros (attached `HardwareWorker.log`)
**Finding:** Elros' log shows **PawnIO CPU temp fallback** initialized and regular CPU/GPU temperature readings, but **no fan RPMs** and intermittent worker errors that may cause missed or ignored EC writes. Notable entries we saw:

- `2026-01-21T08:12:15.3148685+08:00 PawnIO CPU temp fallback initialized (available if WinRing0 blocked)` ✅
- `2026-01-21T07:23:11.9057847+08:00 Error updating Primary: Cannot access a disposed object. Object name: 'Microsoft.Win32.SafeHandles.SafeFileHandle'.` ⚠️
- `2026-01-21T07:24:19.7366536+08:00 [CPU Detected] ... Temp sensors (35): [Core Max=°C, Core Average=°C, ...]` (example of a corrupted/empty sample)

**Implications:**
- Worker lifecycle issues (parent process exits / disposed handle errors) may cause EC writes to be missed or verification reads to fail briefly. If a fan command coincides with a worker restart or dispose, the command may not be executed.
- Temp sensors are present (CPU + GPU), so missing fan RPMs are likely due to either HW not exposing RPMs via LibreHardwareMonitor or EC/OGH not returning RPM reads — we need both write verification and RPM read verification.

**Follow-up requested from user (Elros):**
1. Reproduce the issue with **Debug** logging enabled and re-run the in-app **Fan Diagnostic** mode. Note exact local timestamps of when you apply **Max** and when you run diagnostics.
2. Export Diagnostics (Compressed ZIP) and include `HardwareWorker.log` and `OmenCore.log` — upload here if possible.
3. If you can, repeat the Max apply while watching for worker restarts (or check Task Manager for `OmenCore.HardwareWorker.exe` restarts). Note times.

**Developer actions to investigate this case:**
- Add targeted logging around worker shutdown/dispose paths and guard EC writes to ensure they fail loudly if the worker is not alive.
- Detect and log corrupted/empty samples (samples whose sensors report `°C`) with timestamps and treat them as failures that trigger a short reinitialize + retry for the next Max apply attempt.
- If worker restarts are found to coincide with Max application, add a confirmation flow: apply Max → readback → if worker restart or readback invalid, retry apply up to N times with small backoff.

---

## Next steps for me (I can do):
- Add a short PR that improves Max mode robustness: add retries + read-back verification + extra debug logs (small, self-contained change). I’ll include unit tests for the retry behavior.
- Prepare a short template for reporters to fill out when they open tickets (time applied, OS, model, OGH installed/running, PawnIO/WinRing0 present).

---

If you want, I can draft the PR and include the new logs and tests; or I can prepare a short patch with only improved logging so we can gather clearer evidence first.

---

*File created on: 2026-01-21*