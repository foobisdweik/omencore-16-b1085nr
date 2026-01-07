# OmenCore Full Application Audit (audit_1)

Scope: whole-repo audit with Windows focus (WPF app + installer), with awareness of cross-platform components (Avalonia + Linux CLI). This is recommendations-only (no code changes).

## Critical Fixes (do these first)

1) **Fix installer permissions in `{app}` (Program Files) to prevent local privilege escalation**
   - **Why it’s critical:** The installer sets `PrivilegesRequired=admin`, creates a scheduled task with `/rl highest`, and also grants `users-modify` on `{app}\config` and `{app}\logs`. Any non-admin user who can modify files that an elevated process later reads (config) or writes (logs) under Program Files can potentially influence elevated behavior.
   - **Where:** `installer/OmenCoreInstaller.iss` (`[Dirs]` section and scheduled task creation in `[Run]`).
   - **Recommendation:**
     - Remove `users-modify` ACLs under `{app}`; treat Program Files as read-only for standard users.
     - Store config/logs strictly under per-user locations (e.g., `%LOCALAPPDATA%\OmenCore\...`) and ensure the elevated-start path does not consume mutable configuration from `{app}`.
     - Re-evaluate autostart: prefer a per-user task (no `/rl highest`) unless elevated access is strictly required for startup.

2) **Harden the hardware worker IPC (named pipe) against cross-user/local DoS and spoofing**
   - **Why it’s critical:** The worker exposes a fixed named pipe (`OmenCore_HardwareWorker`) with a `SHUTDOWN` command. Client and server currently show no authentication/handshake and no explicit access restriction. A different local process/user could connect and send `SHUTDOWN` (or spam requests), degrading monitoring reliability.
   - **Where:** `src/OmenCore.HardwareWorker/Program.cs` (pipe server + command handling), `src/OmenCoreApp/Hardware/HardwareWorkerClient.cs` (pipe client, sends `SHUTDOWN`).
   - **Recommendation:**
     - Restrict the pipe to the current user/session (e.g., Windows-only options like `PipeOptions.CurrentUserOnly` where available, or explicit `PipeSecurity` ACL to the current SID).
     - Add a lightweight authentication mechanism (random per-run token passed via command-line to the worker and required in each request) and/or remove the externally-triggerable `SHUTDOWN` command.
     - Consider per-process unique pipe names (include parent PID + random suffix) to prevent collisions and unwanted attachment.

3) **Stop “download-and-run as admin” flows without strong integrity validation**
   - **Why it’s critical:** There are paths that download third-party binaries (LibreHardwareMonitor zip; PawnIO ecosystem references; updates when release notes lack hash) and then execute or instruct execution. A network/MITM, compromised upstream, or tampered local file scenario can become code execution—especially dangerous when elevation is involved.
   - **Where:**
     - `src/OmenCoreApp/App.xaml.cs` (`DownloadAndInstallLibreHardwareMonitor()` downloads `LibreHardwareMonitor-net472.zip` and executes `LibreHardwareMonitor.exe` elevated).
     - `installer/download-librehw.ps1` and `build-installer.ps1` (downloads LHM for bundling without checksum/signature validation).
     - `src/OmenCoreApp/Services/AutoUpdateService.cs` (verifies SHA256 only if a hash is present in release notes; otherwise warns and proceeds).
   - **Recommendation:**
     - Require a cryptographic verification step for every download (e.g., pinned SHA256 per version in-repo, or verified Authenticode signature for executables where applicable). Fail closed when verification is missing.
     - Avoid executing downloaded binaries as admin; prefer bundling vetted binaries at build time or using OS-level package/signature validation.
     - For auto-update, treat “missing hash” as a hard failure (or require a signed release manifest).

## Medium-Priority Fixes (next iteration)

1) **Fix worker lifecycle edge cases (orphaned worker processes and multi-spawn behavior)**
   - **Why it matters:** `StartAsync()` starts the worker process and returns `false` if connection retries fail, but it doesn’t clearly guarantee cleanup of the launched process on failure. This can leave stray worker processes, increase resource usage, and complicate subsequent reconnect attempts.
   - **Where:** `src/OmenCoreApp/Hardware/HardwareWorkerClient.cs` (`StartAsync()`, connection retry loop).
   - **Recommendation:** Ensure failed connection attempts terminate the just-started worker and dispose pipe resources deterministically before returning.

2) **Replace `async void` initialization in monitoring integration with a task-based lifecycle**
   - **Why it matters:** `async void` makes it harder to observe failures, coordinate shutdown, and prevent races (especially during app exit). The current pattern relies on polling flags and can produce hard-to-reproduce startup/shutdown issues.
   - **Where:** `src/OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs` (`InitializeWorker()` is `async void`; `ReadSampleAsync()` waits on `_workerInitializing`).
   - **Recommendation:** Move to `Task`-returning initialization with a single shared initialization task, explicit cancellation, and clear error propagation.

3) **Audit all `Process.Start` / external command execution paths for quoting, elevation, and user-controlled inputs**
   - **Why it matters:** This app controls system-level settings; any unsafe process invocation can become a reliability or security issue (argument injection, wrong binary resolution, accidental elevation paths).
   - **Where:** Multiple call sites (example: `src/OmenCoreApp/ViewModels/SystemOptimizerViewModel.cs` uses `shutdown /r /t 0`; other `Process.Start` occurrences exist throughout the app).
   - **Recommendation:** Centralize process launching behind a single helper that enforces:
     - absolute paths where feasible,
     - strict argument construction (no string concatenation from user content),
     - clear elevation boundaries (only elevate for narrowly-scoped operations).

## Future Opportunities (strategic improvements)

1) **Introduce a proper privilege-separation model (UI as standard user; hardware actions in a constrained elevated component)**
   - **Why:** Today’s design (admin installer + highest-privilege autostart) increases the blast radius of bugs and config mistakes.
   - **Opportunity:** A small Windows service / broker process that performs only EC/MSR operations with a narrow IPC surface; keep the main UI unprivileged.

2) **Formalize a “trusted artifacts” supply chain**
   - **Why:** OmenCore depends on third-party components (hardware monitoring, drivers/modules) that are security-sensitive.
   - **Opportunity:** Maintain a signed/pinned manifest of bundled binaries, enforce checksum verification during build and runtime, and (where applicable) verify Authenticode signatures before execution.

3) **Consolidate configuration + diagnostics across WPF/Avalonia/Linux into a shared core**
   - **Why:** The repo spans multiple front-ends and platforms.
   - **Opportunity:** Define a shared config schema, versioned migrations, and consistent structured logging (plus optional, privacy-respecting crash reporting) to reduce support load and improve reliability across all entry points.
