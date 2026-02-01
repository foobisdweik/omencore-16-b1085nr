# v2.6.0 - Fixes (Draft)

This file categorizes the fixes implemented for the v2.6.0 release and outlines verification steps and roll-back guidance.

Diagnostics / UI
- Fan Diagnostics history bindings fixed so values render correctly in the UI (see `src/OmenCoreApp/Views/FanDiagnosticsView.xaml`).
- Added `docs/BUGS/FAN_DIAGNOSTICS_UI_BINDING.md` describing regression, cause, and repro steps.

EC / Fan Backend Stability
- Hardened fan backend selection and fallbacks to reduce 0 RPM reports on OMEN Max 16 and similar models.
- Documented intermittent EC register contention; mitigation planned: implement inter-process mutex and retry/backoff logic.
- Added `docs/BUGS/EC_REGISTER_CONFLICT.md` with reproduction and mitigations.

RGB / Keyboard Lighting
- Logged keyboard lighting backend failures; added diagnostic notes in changelog. Further model-specific investigation pending.

Tests / CI
- Updated unit test doubles to implement `SetFanSpeeds(int,int)` so tests compile.

Developer notes / Rollback guidance
- Primary code changes: `FanControllerFactory.cs` (fan backend selection), `Views/FanDiagnosticsView.xaml` (UI fix), test files in `src/OmenCoreApp.Tests/**`.
- If issues are reported after rolling this release, roll back to the previous release branch and re-apply individual fixes while verifying unit tests and manual verification steps.

Verification checklist
- Run `dotnet build --configuration Release` and `dotnet test` for test projects.
- Manually verify Fan Diagnostics UI displays history values and `Apply & Verify` updates history with realistic values.
- Reproduce EC contention by running OmenMon concurrently and verify contention logging appears.
