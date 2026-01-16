# OmenCore v2.5.0 Roadmap

**Target Release:** Q2 2026  
**Status:** Planning  
**Created:** January 16, 2026

---

## Summary

v2.5.0 focuses on reliability hardening, deeper telemetry integration, improved testing/CI, and feature polish missed in v2.4.x. This release will: finalize MSI Afterburner integration, complete power-limit verification, add focused unit/integration tests for fan control/EC access, and improve Linux QA and automation.

---

## Carry-over (from v2.4.1)

These are items that were scoped for v2.4.1 but remain incomplete or need further work:

1. **Power Limits Investigation & Verification** (High)
   - Verify `PowerLimitController` behavior and EC registers (0xCE/0xCF) across affected models
   - Add read-back verification and a `PowerVerificationService`
   - Add unit tests and CI checks that mock `IEcAccess` and OGH/WMI responses
   - Acceptance: Apply CPU/GPU limits in-app → EC/WMI readback confirms expected values or logs a meaningful error

2. **Phase 2 RPM Validation & Calibration** (Medium)
   - Validate EC-read RPM vs LibreHardwareMonitor and Afterburner when available
   - Add optional model calibration stored in `config.json`
   - Acceptance: Provide calibration UI and ensure RPM→percent mapping stable across runs

3. **Preset Logic Finalization** (High)
   - Ensure `ApplyPreset()` behavior is consistent (delegate to `FanService` or temperature-evaluate consistently)
   - Add regression tests for preset transitions and hysteresis
   - Acceptance: Balanced preset never causes instant max, and hysteresis/ramp behavior validated by tests

4. **Linux Testing & Packaging Completion** (Medium)
   - Finish Linux testing checklist (CLI/daemon, EC access, systemd install, SELinux recommendations)
   - Provide a first-class Linux release artifact (tarball + checksums) and update docs/automation
   - Acceptance: Automated CI job that publishes `omencore-cli` linux artifacts and runs Linux smoke tests

5. **CI / Release Automation** (Medium)
   - Ensure pipeline generates checksums and attaches them to GitHub Releases automatically
   - Add strict warning-level build flags for PRs
   - Acceptance: CI fails PRs with high-severity warnings; artifacts + checksums posted on release

---

## New v2.5.0 Priorities

### 1) MSI Afterburner Full Integration (High)
- Goals:
  - Improve automatic detection and reliability of reading GPU telemetry from MSI Afterburner/RTSS
  - Provide optional GPU telemetry fallback to Afterburner when available
  - Add conflict mitigation guidance in UI (notify when Afterburner + OmenCore might conflict)
- Deliverables:
  - Robust `AfterburnerProvider` with shared-memory parsing and graceful fallback
  - UI: conflict warnings + telemetry source indicator
  - Tests: mocked shared memory unit tests
- Acceptance Criteria:
  - Afterburner running → `ConflictDetectionService` detects it and OmenCore reads GPU temps/RPMs from shared memory
  - No crashes when Afterburner absent or shared memory is locked

### 2) Unit & Integration Tests for Fan Control & EC Access (High)
- Goals:
  - Add deterministic unit tests for `FanController`, `FanVerificationService`, and `PowerLimitController`
  - Add integration-style tests that use fakes for EC access and Afterburner
- Deliverables:
  - New test project with test vectors (curve evaluation, RPM conversions, hysteresis timing)
  - CI job runs unit tests and reports coverage for critical modules
- Acceptance Criteria:
  - Key fan control behaviors covered by unit tests with >80% coverage on those components
  - CI fails on new regressions for fan control logic

### 3) Telemetry & Diagnostic UX (Medium)
- Goals:
  - Expand `DiagnosticLoggingService` to support scheduled captures and easy upload packaging
  - Add UI to export or attach diagnostics to GitHub issues via helper
  - Provide privacy-first opt-in telemetry (anonymized calibration data)
- Deliverables:
  - Diagnostic export zip with logs + EC dumps + system info
  - UI button for "Collect diagnostics" and "Copy issue template"
- Acceptance Criteria:
  - Support engineer can reproduce reported EC state from uploaded diagnostics

### 4) Performance & Monitoring (Medium)
- Goals:
  - Reduce CPU cost of monitoring loops (lower contention, coalesce sensor reads)
  - Add optional low-overhead mode for long-running daemon use
- Deliverables:
  - Optimized hardware monitoring loop with configurable intervals per provider
  - Telemetry showing CPU/time savings in benchmarks
- Acceptance Criteria:
  - Low overhead mode reduces monitoring CPU by >=30% in measurement

### 5) Documentation & QA (Low→Medium)
- Goals:
  - Expand Linux testing docs and release notes
  - Add "How to collect diagnostics" guide
  - Update FAQ with common EC model quirks and power-limit notes
- Deliverables:
  - `docs/LINUX_TESTING.md` updated and tested with volunteers
  - `docs/AUTO_UPDATE_ENHANCEMENTS.md` updated with edge-case tests
- Acceptance Criteria:
  - Testers confirm the checklist and report no blocking issues at release

---

## ⚪ Nice-to-have / Stretch Goals

These are lower-priority features we can include in v2.5.0 if capacity allows or defer to v2.6. Each includes deliverables and an acceptance criterion to make scope clear.

- **In-app PawnIO/driver installer & Secure Boot guidance**
  - Deliverables: Installer UI that bundles PawnIO helper, Secure Boot detection, and step-by-step guidance for signed driver installation or recommended alternatives.
  - Acceptance: Users can follow the in-app flow to enable PawnIO or receive actionable guidance without ambiguous errors.

- **Local HTTP API & CLI automation improvements**
  - Deliverables: A small, secure local HTTP socket or named pipe exposing status and control endpoints; CLI commands mapped to HTTP endpoints.
  - Acceptance: Automated scripts can query fan status and apply presets via the API/CLI reliably.

- **Per-game profile improvements (import/export & heuristics)**
  - Deliverables: Profile editor UI, import/export JSON, and auto-switch heuristics with process matching priorities.
  - Acceptance: Profiles can be exported/imported and auto-apply reliably when the matched process starts.

- **Plugin / Provider architecture for telemetry**
  - Deliverables: Stable provider interface with docs + example community provider (e.g., custom telemetry source).
  - Acceptance: An external provider can be implemented and loaded in dev mode to supply telemetry without core changes.

- **Opt-in anonymous telemetry for calibration uploads**
  - Deliverables: Privacy-first opt-in setting, anonymized payload format, uploader in diagnostics UI.
  - Acceptance: Calibration data can be uploaded and consumed for model calibration while respecting user consent and privacy.

- **Accessibility & Localization (i18n)**
  - Deliverables: Externalized string resources, at least one translated locale, keyboard and screen-reader checks.
  - Acceptance: Key UI flows are localized and basic accessibility checks pass.

- **Performance profiling & benchmark suite**
  - Deliverables: Lightweight profiling harness and metrics for monitoring loop CPU/time usage.
  - Acceptance: We can measure and confirm low-overhead mode reduces CPU usage by a target percentage.

---

## Roadmap Timeline & Milestones
- Week 1: Finalize spec and tests (unit tests for fan control, Afterburner parsing tests)
- Week 2-3: Implement `PowerVerificationService` and Afterburner provider
- Week 4: Add diagnostics export UI; CI pipeline updates for publishing checksums
- Week 5: Linux QA and packaging; community testing
- Week 6: Freeze, release candidate, final checks and publish

---

## Community & Testing Requests
- Volunteers needed to test power limit verification and RPM calibration on:
  - OMEN 16-b0xxx, 16t-ah000, 16-u0xxx, 17-ck2xxx families
- Linux QA volunteers for Debian/Ubuntu and Raspberry Pi ARM64

---

## Notes / Out-of-scope
- Removing `requireAdministrator` (audit suggestion) is a breaking change and deferred to v3.0 (architectural work required)

---

**If this looks good, I can open a draft GitHub issue board for v2.5.0 and break down the work into PR-sized tasks.**
