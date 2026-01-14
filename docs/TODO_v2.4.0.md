# v2.4.0 — Triage TODOs

This file lists actionable tasks to triage and create GitHub issues from.

## Critical
- [ ] Add CI job `build:strict` that runs `dotnet build -p:TreatWarningsAsErrors=true` and attach to PR checks. (owner: @theantipopau)
- [ ] Quick `= null!` placeholders PR for viewmodels to make build pass under strict build. (owner: @team)
- [ ] Improve HpWmiBios fallback and add tests for known failing OMEN Max/17-ck and OMEN MAX 16z. (owner: @core)

## High
- [ ] Spike: D3D11 hook for accurate FPS counter (POC + perf validation). (owner: @graphics)
- [ ] OSD layout editor & presets - design and API for presets. (owner: @ui)
- [ ] Per-game OSD profiles: store, match, apply. (owner: @game-profiles)

## Medium
- [ ] Add unit and integration tests for OSD, fan control, and Linux GitHub fallback. (owner: @qa)
- [ ] Rebuild linux-arm64 artifact, test on common distros, add SHA to changelog. (owner: @linux)
- [ ] Add structured diagnostics export for Linux bug reports (include dmesg, hp-wmi info). (owner: @core)

## Low
- [ ] Accessibility pass (keyboard nav, aria, contrast). (owner: @ux)
- [ ] Release automation: auto-generate checksums & artifacts on release. (owner: @infra)

---

## Suggested process
1. Create individual GitHub issues for each bullet above (copy/paste from `.github/ISSUES_v2.4.0.md`).
2. Assign priority label (critical/high/medium/low) and the `v2.4.0` milestone.
3. Create small PRs for quick wins (CI & `= null!` placeholders) to unblock slower refactors.
4. Track progress in project board.

*Adjust owners and priorities as needed.*
