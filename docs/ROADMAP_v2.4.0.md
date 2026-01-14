# OmenCore Roadmap — v2.4.0

**Target**: v2.4.0 (Next feature release)

---

## Goal
Ship a stable, safety-first release that completes deferred UX improvements from v2.3.2 and addresses reliability, testing, packaging, and CI hardening.

---

## ✅ Deferred from v2.3.2 (carryover)
- OSD horizontal layout full XAML implementation
- OSD preset layouts (Minimal, Standard, Full, Custom)
- More robust storage exclusion and handling for drive sleep

---

## 🔴 Critical (blocker-level)
- Desktop safety & monitoring improvements (acceptance: desktop detection + no fan writes by default; tests) ✅
- Fan control reliability & diagnostics (HpWmiBios improvements + additional telemetry when fallbacks occur) — acceptance: reproduce known failing models and verify graceful fallback
- CI: Add `TreatWarningsAsErrors` build variant to catch nullable-reference regressions before merge

## 🐛 New bugs reported for v2.4.0 (post v2.3.2)

### Linux GUI issues (dfshsu - OMEN MAX 16z tester)
- **Memory display**: Shows percentage only, missing GB values (e.g., "33%" instead of "5.0 / 8.0 GB")
- **Version mismatch**: About screen shows v2.3.1 when running v2.3.2 binary
- **Permissions**: App requests root access even when already running with `sudo`
- **Profile changes ineffective**: Changing performance/fan profiles has no visible effect on hardware
  - Note: May be related to hp-wmi kernel driver limitation documented in v2.3.2 changelog
  - Need to distinguish between UI bug vs kernel driver support

### GitHub #47 - Fan thermal issue (kg290)
- **Symptom**: Laptop overheated to 75°C while watching movie on Quiet fan mode; fans didn't spike consistently
- **Root Cause**: Possible thermal protection bug or fan curve not aggressive enough in Quiet mode under sustained load
- **Priority**: High (safety-related)
- **Action**: Investigate thermal protection logic, minimum fan floor enforcement, and Quiet mode fan curve
- **Related**: v2.3.1 added 30% minimum fan floor when thermal protection releases — may need tuning

### GitHub #48 - UI/UX improvements (kg290 + its-urbi)
Multiple layout and navigation suggestions:
1. Move Fan Diagnostics, Keyboard Diagnostics to separate tab or Settings (rarely used; combine all diagnostics side-by-side)
2. Move CPU Undervolt to less prominent location (one-time change)
3. Print Screen button doesn't open Snipping Tool (Win+Shift+S works)
4. Add collapsible/minimizable logs window; make logs resizable and save preference
5. Scrolling feels stuttery (refresh rate issue)
6. Too much scrolling needed to reach specific modes (empty space in boxes)
7. Fan and Thermal Control mode set takes excessive space
8. Add Ctrl+Plus/Minus zoom support or zoom out default view
9. Utilize space better: add current CPU/GPU temp and fan speed to main view; remove from Advanced tab
10. Monitoring tab: can't see all graphs without scrolling
11. Settings tab: add sub-tabs for faster navigation instead of scrolling
12. (its-urbi comment): Hide update dialog when on latest version; make update bar clickable for release notes

**Priority**: Medium (quality-of-life improvements)
**Action**: Triage each item, create design mockups, implement in phases

### Reddit - UI Freeze after 20-30 min gameplay (CRITICAL)
- **Symptom**: OmenCore UI completely freezes after 20-30 minutes of gaming (Eternal Return via Steam)
- **Workaround**: Restart OmenCore
- **User Environment**: Also running MSI Afterburner + ThrottleStop
- **Potential Causes**:
  1. UI thread blocking from sensor polling
  2. Deadlock in WMI query loop
  3. Conflict with MSI Afterburner (both apps polling same sensors)
  4. ThrottleStop EC register conflict
  5. Memory leak causing hang
  6. Timer/event handler leak
- **Priority**: CRITICAL (affects usability during gaming)
- **Action**: 
  - Investigate sensor polling on background thread vs UI thread
  - Add timeout to WMI queries
  - Profile memory usage during 30+ min sessions
  - Test with/without MSI Afterburner
  - Add deadlock detection in debug mode

### Additional suggested fixes for v2.4.0

#### Code Quality & Reliability (High)
- **Fix 76 nullable reference warnings** — Apply proper constructor DI or `= null!` with TODO comments
- **Add comprehensive error boundaries** — Wrap critical operations in try-catch with user-friendly messages
- **Improve logging consistency** — Structured logging with severity levels for easier diagnostics
- **Memory leak investigation** — Profile long-running sessions for leaked event handlers or timers

#### Testing & CI (High)
- **Add unit tests for fan control logic** — Test HpWmiBios fallback, thermal protection, fan curve validation
- **Integration tests for OSD** — Verify refresh on mode change, hotkey registration, overlay positioning
- **CI: TreatWarningsAsErrors job** — Prevent new nullable warnings from merging
- **Automated regression tests** — Test installer, portable ZIP, Linux bundle on VMs

#### Performance (Medium)
- **Reduce sensor polling overhead** — Batch WMI queries, cache frequently-read values
- **Optimize UI rendering** — Virtual scrolling for long lists, deferred loading for heavy tabs
- **Background worker efficiency** — Profile CPU usage when minimized; ensure OSD truly stops when disabled

#### Diagnostics & Debugging (Medium)
- **Enhanced diagnostics export** — Include `dmesg`, WMI command history, failed operation logs
- **Telemetry opt-in system** — Anonymous crash reports and performance metrics (privacy-first)
- **Debug mode toggle** — Enable verbose logging without editing config files
- **Hardware detection report** — Auto-generate compatibility report for new laptop models

#### User Experience (Medium)
- **First-run wizard** — Detect hardware capabilities, suggest optimal settings, explain Limited Mode
- **Hotkey conflict detection** — Warn if chosen hotkey is already registered by another app
- **Fan curve templates** — Provide Silent/Balanced/Performance/Aggressive presets users can customize
- **Notification improvements** — Less intrusive, actionable (e.g., "Fans at 100%, click to adjust")

#### Documentation (Low)
- **Video tutorials** — Quick start, fan curve creation, troubleshooting common issues
- **FAQ expansion** — Cover more edge cases from Discord/GitHub issues
- **Developer guide** — Architecture overview, contribution guidelines, testing procedures
- **Localization support** — Framework for multi-language UI (community-driven translations)

#### Installer & Packaging (Low)
- **Portable mode detection** — Auto-configure paths when running from USB/external drive
- **Uninstaller improvements** — Ask to preserve config, export diagnostics before uninstall
- **Auto-update robustness** — Resume interrupted downloads, verify signatures before applying

### Response re: "vibe coded" question
Project uses AI assistance (GitHub Copilot) for some code generation and debugging, but all fixes are:
- Tested on real hardware (Windows) and by community testers (Linux)
- Reviewed for correctness and edge cases
- Verified against logs and user reports
Fast turnaround is due to focused bug triage and immediate testing feedback loop.

---

## 🔵 High priority
- Accurate FPS counter (D3D11 hook) — acceptance: in-game FPS equals observed frames in test harness
- OSD layout editor & preset system (save / load / apply) — acceptance: users can create, select, and persist OSD presets
- Per-game OSD profiles (apply by process name / game rom id)
- Properly initialize large ViewModels (fix nullable warnings by constructor injection) — acceptance: `dotnet build -p:TreatWarningsAsErrors=true` passes on CI

---

## 🟢 Medium priority
- Unit & integration tests for: HpWmiBios, OSD refresh on mode change, Linux GitHub button fallback, Avalonia DynamicResource loading
- Coverage target: +15% lines by v2.4.0
- Linux packaging: build and publish linux-arm64 artifact and verify on supported distros
- Improve diagnostics export format (structured JSON with dmesg, hp-wmi outputs) for bug reports

---

## ⚪ Low priority / Nice-to-have
- GUI micro-optimizations (reduce memory for low-overhead mode)
- Accessibility improvements for UI automation and keyboard navigation
- More release automation: auto-publish checksums and artifacts on release tag

---

## Collaboration & Kernel support
- Document steps to reproduce and a minimal patch to add the OMEN MAX 16z board ID to `hp-wmi` — include `dmesg` output examples and how to test
- Reach out to maintainers via patchwork and provide test hardware info (volunteers)

---

## Acceptance Criteria & Definition of Done
- All Critical tasks have tests (unit/integration) and pass CI with `TreatWarningsAsErrors` enabled
- Release artifacts published and SHA256 checksums included in changelog
- User-visible changes documented in `CHANGELOG_v2.4.0.md` and `README.md` where applicable

---

## Owners & Estimates (suggested)
- Core & CI: @theantipopau — 2w
- OSD & D3D11 Hook: @someone-graphics — 3w
- Linux & packaging: @linux-tester — 1w
- Testing & QA: @qa-team — 2w

---

## Next steps
1. Create triage issues for the 
   - CI change, 
   - Quick nullable fix PR, 
   - D3D11 hook spike, 
   - OSD editor spike, 
   - Linux ARM64 build
2. Add milestone `v2.4.0` and move the created issues into it
3. Track progress with project board columns: Backlog → In Progress → Review → Done

---

*File generated by automation. Please review and adjust owners/estimates as needed.*
