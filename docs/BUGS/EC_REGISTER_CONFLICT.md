# EC Register Contention / Inter-process Conflicts

Summary
- On some systems, when multiple user-space tools concurrently access the embedded controller (EC) registers (for example, OmenCore + OmenMon), fan telemetry becomes intermittent or incorrect. Typical symptoms: temporary 0 RPM readings, occasional absurdly large RPM values, and fan control operations that do not apply reliably.

Evidence
- See attached logs (OmenCore_20260124_110019.log) showing alternating valid EC reads and 0/invalid RPM values when other tools are active.

Reproduction steps
1. Start OmenCore and let hardware worker initialize.
2. Start OmenMon (or other EC-accessing tool) while OmenCore is running.
3. Observe fan telemetry in OmenCore: RPM may drop to 0, spike, or stop updating; fan-control presets may not take effect.

Impact
- Incorrect telemetry in UI and diagnostics
- Fan control commands may be ignored or applied inconsistently

Suggested mitigations
- Add inter-process coordination for EC access (named mutex or OS-level lock) in the hardware worker so only one process performs direct EC register read/writes at a time.
- When contention is detected, expose diagnostic logging and user-visible warning indicating another EC-using application is running.
- Use a retry/backoff strategy on EC read failures and a small jitter to avoid simultaneous retries from multiple processes.
- Provide an optional "exclusive EC access" debug mode for support to force single-process access and gather troubleshooting logs.

Next steps for developers
- Implement a named mutex around EC read/write operations in `src/OmenCoreApp/Hardware/*` EC access layer.
- Add detection of common conflicting apps (e.g., OmenMon, OEM utilities) and log their presence.
- Add a runtime opt-in telemetry flag to record contention counts for later analysis.

Notes
- This is a draft bug report added to the repository for tracking. See `docs/CHANGELOG_v2.6.0.md` Known issues for a short note.
