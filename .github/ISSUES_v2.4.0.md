# GitHub Issue Drafts — v2.4.0 (copy/paste-ready)

## CI: Strict build checks (critical)
Title: CI: Add strict build job (`TreatWarningsAsErrors`) to PR checks
Body:
```
**Describe the problem**
We have a large backlog of nullable reference warnings that can become runtime issues. We need a CI job that runs `dotnet build -p:TreatWarningsAsErrors=true` to fail PRs that introduce new warnings.

**Acceptance criteria**
- New CI job `build:strict` runs on PRs
- Failing PRs block merge
- Job is documented in CONTRIBUTING.md
```

---

## Quickfix: Null-forgiving placeholders (short-term)
Title: Quick: Apply `= null!` placeholders to non-nullable viewmodel fields
Body:
```
**Describe the task**
Apply `= null!` to non-nullable fields in large ViewModels (e.g., `MainViewModel`) where initialization is deferred and add `TODO` comments linking to follow-up refactor issues.

**Acceptance criteria**
- Build passes under `TreatWarningsAsErrors` job
- Each change includes a `TODO` comment referencing a refactor issue
```

---

## Feature: D3D11-based FPS counter (high)
Title: Feature: Implement D3D11 hook for accurate FPS counter
Body:
```
**Goal**
Replace current GPU-load-based FPS estimate with an accurate D3D11 frame counter.

**Acceptance criteria**
- Hook captures frame presentation events
- OSD FPS matches in-game frames in benchmark tests
- Hot-path performance overhead is <1% on test hardware
```

---

## Feature: OSD presets & editor (high)
Title: Feature: OSD layout editor and preset system
Body:
```
**Goal**
Allow users to edit OSD layouts and save/load presets (Minimal/Standard/Full/Custom).

**Acceptance criteria**
- Users can create a new layout, name it, and apply it immediately
- Presets persist across restarts and export/import as JSON
```

---

## Linux: ARM64 build and package (medium)
Title: Release: Build and publish linux-arm64 artifact
Body:
```
**Task**
Publish linux-arm64 self-contained binary, create ZIP artifact and compute SHA256; add to changelog and release assets.

**Acceptance criteria**
- Artifact exists under `artifacts/` and SHA recorded in `CHANGELOG_v2.4.0.md`
```

---

*(Add more issue drafts as needed.)*
