# Fan Diagnostics UI Binding Regression

Summary
- The fan diagnostics history entries in the UI displayed literal binding tokens (e.g., "{Binding RequestedPercent}") instead of the actual values. This prevented users from seeing the verification results and diagnostic history.
- User report: last known working version was v2.3.2; regression observed in later releases.

Symptoms
- History items show raw binding strings instead of values.
- `Apply & Verify` may appear to do nothing because history entries are not readable; logs may still show verification runs.

Reproduction
1. Open OmenCore and navigate to System Diagnostics → Fan Diagnostics.
2. Run `Apply & Verify` for a fan or click `Refresh`.
3. Observe History — entries show literal binding expressions instead of numbers.

Root cause
- XAML used a single `TextBlock` with a literal text attribute containing binding expressions, which WPF treats as a literal string. The template needed inline `Run` elements bound to properties or a `MultiBinding`/`StringFormat`.

Fix applied
- Replaced the literal binding string with `Run` elements inside the `TextBlock` so WPF resolves the bindings correctly. See `src/OmenCoreApp/Views/FanDiagnosticsView.xaml`.

Workarounds
- None required after fix; before fix, users could inspect logs for verification results.

Regression tracking
- Marked as regression from v2.3.2. Added to `docs/CHANGELOG_v2.6.0.md` under Known issues/fixes and to the release fixes list.

Next steps
- Verify `Apply & Verify` interaction works end-to-end with live hardware (ensure `IFanVerificationService` returns expected results).
- If users still report non-functional verification, add logging in `ApplyAndVerifyAsync` to capture verifier errors and command execution state.
