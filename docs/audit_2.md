# OmenCore Full Application Audit (audit_2)

## Critical Fixes (Architecture & Stability)

1.  **Resolve Codebase Bifurcation (Missing Shared Core)**
    *   **Issue:** The project effectively maintains two separate applications: `OmenCoreApp` (Windows/WPF) and `OmenCore.Avalonia` (Linux). They share almost no logic. Hardware implementations (`LibreHardwareMonitorImpl` vs `LinuxHardwareService`), configuration (`AppConfig` vs Dictionary), and ViewModels are duplicated or completely divergent.
    *   **Impact:** Bug fixes in Windows logic (e.g., fan curve algorithms) do not propagate to Linux. Maintenance cost is doubled.
    *   **Recommendation:** Create a `OmenCore.Core` (or `.Shared`) library targeting .NET 8 / Standard. Move common models (`AppConfig`), business logic (Fan Control Loops), and interfaces (`IHardwareService`) into this library. Both UI heads should consume this shared core.

2.  **Eliminate Global Admin Requirement (`requireAdministrator`)**
    *   **Issue:** `app.manifest` forces the entire WPF application to run as Administrator. This is a security anti-pattern (increases attack surface for RCE/DLL hijacking) and a UX failure (breaks file drag-and-drop, causes UAC prompts on every boot, complicates autostart).
    *   **Impact:** Any security vulnerability in the UI (like image parsing or update checking) becomes a system-level compromise.
    *   **Recommendation:** Adhere to the Principle of Least Privilege. Split the application into:
        *   **UI Client:** Runs as Standard User.
        *   **Privileged Service/Worker:** A persistent background service (Windows Service) running as System/Admin that handles WinRing0/PawnIO/WMI operations.
        *   Communicate via secure IPC (Named Pipes/gRPC).

3.  **Refactor "God Object" ViewModels (Separation of Concerns)**
    *   **Issue:** `src/OmenCoreApp/ViewModels/MainViewModel.cs` is over 3,000 lines and manually instantiates nearly every service in the application. It acts as a controller, service locator, and view model simultaneously.
    *   **Impact:** The application is tightly coupled and virtually untestable. A failure in one service initialization can destabilize the entire app startup. Memory usage is high because services are often eagerly loaded.
    *   **Recommendation:** Implement Dependency Injection (Microsoft.Extensions.DependencyInjection) for `OmenCoreApp`. Break `MainViewModel` into smaller, focused composed ViewModels (e.g., `FanPageViewModel`, `LightingPageViewModel`) that request only the services they need.

## Medium-Priority Improvements (Maintainability & Quality)

1.  **Unify Configuration Strategy**
    *   **Issue:** Windows uses a strongly-typed JSON model (`AppConfig.cs`) with custom "Repair" logic. Linux/Avalonia uses a loose TOML dictionary.
    *   **Why extend:** Users cannot easily port configs or switch UIs. The "Repair" logic in Windows is fragile and mixes IO with validation.
    *   **Recommendation:** Adopt a single configuration serialization format (JSON is recommended for consistency with .NET ecosystem). Define a shared `AppConfig` model in the Core library. Use `IOptions<T>` patterns for robust validation and reloading.

2.  **Remove Static Global State (`App.Static`)**
    *   **Issue:** `App.xaml.cs` exposes static properties like `Logging` and `Configuration`. Services and ViewModels access these statics directly (`App.Logging.Info(...)`).
    *   **Why extend:** This prevents unit testing (tests cannot swap out the logger or config) and hides dependencies.
    *   **Recommendation:** Inject `ILogger<T>` and `IConfigurationService` into components via constructors. Remove reliance on global static entry points.

3.  **Abstract Platform-Specific P/Invokes**
    *   **Issue:** `MainViewModel` contains direct P/Invoke definitions for `user32.dll` (Window focus management).
    *   **Why extend:** This logic crashes or behaves unpredictably on non-Windows platforms (blocking future cross-platform WPF or Avalonia unification).
    *   **Recommendation:** Wrap window management APIs in an `IWindowService` or `INativeHost` abstraction. Implement Windows-specifics in a dedicated service, keeping ViewModels platform-agnostic.

## Future Opportunities (Strategic Evolution)

1.  **Client-Server "Headless" Mode**
    *   **Concept:** Since the app requires a privileged worker (Critical #2), expose this worker via a local REST or gRPC API.
    *   **Why:** Enables a "Headless" mode where the backend runs without any UI (saving resources). Allows building remote control interfaces (e.g., a mobile app on the same Wi-Fi, or integration with Home Assistant) to monitor temps and control fans.

2.  **Plugin Architecture for Device Support**
    *   **Concept:** Currently, Corsair, Logitech, and Razer support are hardcoded into the main app references.
    *   **Why:** Adding support for new devices (Asus, Alienware peripherals) requires recompiling the core app.
    *   **Value:** Move device implementations to standard plugin interfaces (`IDeviceProvider`). Load DLLs dynamically. This encourages community contributions for new hardware support without risking core stability.

3.  **Automated Hardware Profiling (AI Tuning)**
    *   **Concept:** Instead of manual fan curves, introduce a "Calibration & Auto-Tune" feature.
    *   **Why:** The app already collects granular data (Temps, Loads, Fan RPM).
    *   **Value:** Use this data to automatically generate the optimal noise-to-performance curve for the specific machine's thermal paste condition and ambient temperature, offering a simpler "one-click optimize" experience for non-technical users.
