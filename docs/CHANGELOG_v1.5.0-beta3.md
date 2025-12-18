# OmenCore v1.5.0-beta3 Changelog

**Release Date:** December 18, 2025  
**Status:** Beta 3  
**Previous Version:** v1.5.0-beta2

---

## ✨ UI/UX Improvements

### Fan Profile Tooltips
**Files Changed:** `Views/FanControlView.xaml`

Added descriptive tooltips to all fan profile cards explaining what each mode does:
- **Max:** "Forces all fans to maximum RPM. Use for heavy gaming or stress tests."
- **Gaming:** "Aggressive fan curve that ramps up quickly. Good for gaming sessions."
- **Auto:** "BIOS-controlled balanced curve. Recommended for everyday use."
- **Silent:** "Prioritizes quiet operation. Fans stay low until temps rise significantly."
- **Custom:** "Apply your custom fan curve defined in the editor below."

---

### Curve Active Indicator
**Files Changed:** `Views/FanControlView.xaml`, `ViewModels/FanControlViewModel.cs`

Added a visual indicator showing when a custom fan curve is actively being applied by the FanService. Shows a green "🔄 Curve Active" badge next to the active mode.

---

### Undervolt Preset Tooltips
**Files Changed:** `Views/SystemControlView.xaml`

Added risk-level tooltips to undervolt quick presets:
- **Conservative (-60 mV):** "Safe starting point for most CPUs. Low risk of instability."
- **Moderate (-100 mV):** "Good balance of power savings. Test with stress tests before daily use."
- **Aggressive (-140 mV):** "⚠️ High risk! May cause crashes or BSODs. Only for silicon lottery winners."

---

### Aggressive Undervolt Confirmation Dialog
**Files Changed:** `Views/SystemControlView.xaml`, `ViewModels/SystemControlViewModel.cs`

The "Aggressive (-140 mV)" undervolt preset now shows a confirmation dialog warning about:
- Blue screen crashes (BSOD) risk
- Application crashes or freezes
- System instability under load
- Recovery information (undervolt resets on reboot)

Users must confirm before the aggressive offset is applied.

---

### Undervolt Slider Visual Improvements
**Files Changed:** `Views/SystemControlView.xaml`

- Added tick marks to undervolt sliders (`TickPlacement="BottomRight"`)
- Added tick labels at key values (-200, -150, -100, -50, 0) for easier reference

---

### TCC Slider Direction Labels
**Files Changed:** `Views/SystemControlView.xaml`

Added "← Higher Temps" and "Lower Temps →" labels below the TCC offset slider to clarify the reversed slider direction.

---

## 🔧 Bug Fixes

### Critical: NVML Crash During GPU Benchmarks
**Files Changed:**
- `Hardware/LibreHardwareMonitorImpl.cs`
- `App.xaml.cs`

**Problem:** OmenCore crashes when running GPU benchmarks (e.g., Heaven Benchmark). Windows Event Log shows:
```
Exception: System.AccessViolationException
at LibreHardwareMonitor.Interop.NvidiaML.NvmlDeviceGetPowerUsage(NvmlDevice)
```

**Root Cause:** NVIDIA's NVML library (`nvml.dll`) can crash with an access violation when querying GPU power/sensors during high GPU load. This is a driver bug in NVML, not OmenCore.

**Fix:**
- Added `TryUpdateGpuHardware()` method that wraps GPU sensor updates in try-catch
- Tracks consecutive GPU monitoring failures (`_nvmlFailures`)
- After 3 consecutive failures, automatically disables GPU monitoring to prevent crash loops
- Enhanced crash dialog specifically detects NVML crashes and recommends driver updates

**Note:** The primary fix is now the Out-of-Process Hardware Worker (see Architecture section below).

---

## 🏗️ Architecture Changes

### Out-of-Process Hardware Worker
**Files Changed:**
- NEW: `OmenCore.HardwareWorker/` - Separate worker process
- `Hardware/HardwareWorkerClient.cs` - IPC client and process manager
- `Hardware/LibreHardwareMonitorImpl.cs` - Worker mode integration
- `ViewModels/MainViewModel.cs` - Enable worker by default
- `build-installer.ps1` - Build and publish worker
- `installer/OmenCoreInstaller.iss` - Stop worker on uninstall

**Problem:** NVML's `AccessViolationException` is a Corrupted State Exception (CSE) that cannot be caught in .NET 8. When NVML crashes, the entire OmenCore process terminates regardless of try-catch blocks.

**Solution:** Hardware monitoring now runs in a separate process (`OmenCore.HardwareWorker.exe`). If NVML crashes, only the worker process dies - the main app continues running and automatically restarts the worker.

**Architecture:**
```
OmenCore.exe (Main App - WPF UI)
    │
    │ Named Pipes IPC ("OmenCore_HardwareWorker")
    │ Protocol: JSON over pipes
    │   - PING → PONG (heartbeat)
    │   - GET → HardwareSample JSON
    │   - SHUTDOWN → OK (graceful exit)
    ▼
OmenCore.HardwareWorker.exe (Worker Process - Hidden)
    │
    │ LibreHardwareMonitor API calls
    ▼
Hardware/Drivers (NVML, etc.)
```

**Features:**
- Worker process starts automatically when OmenCore launches
- Worker runs hidden (no console window visible)
- If worker crashes, main app auto-restarts it (max 5 attempts, 5s cooldown)
- Main app continues with cached sensor values during worker restart
- Worker logs errors to `OmenCore_worker.log` for diagnostics
- Worker is killed on app exit and uninstall

**Impact:** Users can now run GPU benchmarks without OmenCore crashing. If NVML fails, only temperature/power readings pause briefly during worker restart.

---

### Intel Arc GPU Temperature Not Detected
**Files Changed:**
- `Hardware/LibreHardwareMonitorImpl.cs`

**Problem:** OMEN 45L desktops with Intel Arc GPUs show 0°C for GPU temperature.

**Root Cause:** Intel Arc is a dedicated GPU but was being treated as integrated (fallback-only). The code only read Intel GPU temps when no other GPU was detected, but Arc should be the primary GPU.

**Fix:**
- Intel Arc GPUs (detected by name containing "Arc") are now treated as dedicated GPUs
- Arc GPUs get full sensor support: temperature, load, power, clock speed
- Uses `TryUpdateGpuHardware()` for safe updates (same protection as NVIDIA/AMD)
- Debug logging when Arc temp readings fail to help diagnose issues

---

### Critical: Version Display Fixed
**Files Changed:**
- `OmenCoreApp.csproj`

**Problem:** About section showed "Version: 1.4.0" instead of "1.5.0".

**Root Cause:** Assembly version in csproj was not updated to 1.5.0.

**Fix:** Updated `<AssemblyVersion>` and `<FileVersion>` to 1.5.0.

---

### Critical: Custom Fan Curves Only Applying Temporarily
**Files Changed:**
- `Services/FanService.cs`
- `Hardware/WmiFanController.cs`

**Problem:** Custom fan curves would apply initially but then revert back to BIOS control. Setting 100% via curve would spin fans up then slow down again. (GitHub Issue #12)

**Root Cause:** Multiple issues:
1. **Missing countdown extension** - `SetFanSpeed()` didn't start the countdown extension timer. HP BIOS has a 120-second timeout that resets fan control to auto mode.
2. **Callback condition bug** - The countdown extension callback only ran when `_lastMode != Default`, but custom curves don't change `_lastMode` - they use `IsManualControlActive` instead.
3. **No forced re-application** - Curve only re-applied when target percentage changed, but BIOS could reset it silently.

**Fix:**
- `WmiFanController.SetFanSpeed()` now starts countdown extension timer
- Countdown callback now extends for any manual control mode (`IsManualControlActive || non-Default mode`)
- Added force-refresh mechanism: curve re-applies every 60 seconds even if target percentage unchanged
- Reduced curve update interval from 15s to 10s for faster response

---

### Critical: Custom Curve 100% Only Reaching ~5500 RPM vs Max Preset 5900 RPM
**Files Changed:**
- `Hardware/WmiFanController.cs`

**Problem:** Max preset reaches 5900 RPM, but custom curve at 100% only reaches ~5500 RPM. (GitHub Issue #12)

**Root Cause:** 
- Max preset uses `SetFanMax(true)` - a special BIOS command that bypasses power limits
- Custom curve used `SetFanLevel(55)` which is subject to BIOS thermal/power limits

**Fix:** `SetFanSpeed(100)` now uses `SetFanMax(true)` to achieve true maximum RPM:
```csharp
if (percent >= 100)
{
    success = _wmiBios.SetFanMax(true);
    // Fallback to SetFanLevel(55) if SetFanMax fails
}
else
{
    _wmiBios.SetFanMax(false); // Disable max mode first
    // Then apply normal SetFanLevel
}
```

This ensures custom curves requesting 100% get the same max RPM as the Max preset.

---

### Hotkey Logging Accuracy
**Files Changed:**
- `ViewModels/MainViewModel.cs`

**Problem:** Log showed "Global hotkeys enabled" even when hotkeys were disabled in settings.

**Fix:** `InitializeHotkeys()` now checks `_config.Monitoring.HotkeysEnabled` before registering hotkeys and logging.

---

### Log Display Improvements
**Files Changed:**
- `ViewModels/MainViewModel.cs`

**Problem:** System log in Settings view had excessive newline padding and appeared delayed.

**Root Cause:** `HandleLogLine()` was using `AppendLine()` which adds double newlines, and trimming wasn't applied.

**Fix:** 
- Skip empty log entries
- Use single newline separator
- Trim whitespace from entries
- More efficient string joining for truncation

---

### UI Status Indicators Sync on AC/DC Change
**Files Changed:**
- `Services/FanService.cs`
- `Services/PerformanceModeService.cs`
- `ViewModels/MainViewModel.cs`

**Problem:** When power automation switched presets on AC attach/detach, the sidebar and tray indicators didn't update. They only updated on manual changes.

**Fix:**
- Added `PresetApplied` event to `FanService`
- Added `ModeApplied` event to `PerformanceModeService`
- MainViewModel subscribes to these events and updates:
  - `CurrentFanMode` and `CurrentPerformanceMode` properties
  - Dashboard ViewModel
  - TrayIconService (via property change)

---

### Fan Control Restored on Exit
**Files Changed:**
- `Services/FanService.cs`
- `App.xaml.cs`

**Problem:** Quitting OmenCore from system tray left fans at their current speed instead of returning to BIOS auto control.

**Fix:**
- `FanService.Dispose()` now calls `_fanController.RestoreAutoControl()`
- `App.OnExit()` explicitly disposes MainViewModel before shutdown

---

### Dual Startup Entry Fix
**Files Changed:**
- `ViewModels/SettingsViewModel.cs`
- `installer/OmenCoreInstaller.iss`

**Problem:** OmenCore created multiple startup entries:
1. Registry entry at `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
2. Startup folder shortcut (installer)
3. Scheduled task (app settings)

This caused OmenCore to fail to start or start multiple times.

**Fix:**
- **App:** Now uses only Task Scheduler for startup (required for elevated privileges)
- **App:** `CleanupOldStartupMethods()` removes registry and startup folder entries
- **Installer:** No longer creates startup folder shortcut
- **Installer:** Creates scheduled task if user selects autostart
- **Installer:** Removes scheduled task on uninstall

---

### EC Keyboard Safety Warning Enhanced
**Files Changed:**
- `ViewModels/SettingsViewModel.cs`

**Problem:** User reported EC keyboard mode causing screen brightness issues (black screen).

**Fix:** Enhanced warning dialog with:
- Stop icon (🛑) instead of warning
- Explicit mention of brightness/display issues
- Recovery instructions if screen goes black
- Clearer risk acknowledgment

---

### Temperature Sensor Selection Optimized
**Files Changed:**
- `Hardware/LibreHardwareMonitorImpl.cs`

**Problem:** Fan curve was using CPU Package temp and GPU Hotspot temp, which fluctuate significantly and cause fans to constantly ramp up and down.

**Root Cause:** 
- CPU Package temp includes brief spikes from turbo boost that don't represent actual thermal load
- GPU Hotspot (junction temp) spikes during brief load bursts

**Fix:**
- **CPU:** Now prioritizes Core #1 temp over Package temp (more stable, smoothed reading)
- **GPU:** Explicitly uses "GPU Core" temp (not Hotspot) for fan control
- Hotspot is still tracked separately for thermal alerts

**Result:** Fans should be much more stable with less oscillation during normal use.

---

## 📋 Known Limitations

### VBIOS Power Limit Override
**User Feedback:** "Is it possible to overwrite system PL? I've been trying to VBIOS flash my omen 14 2025 and the GPU refuses to get fed more power than the default 75W."

**Status:** Not currently possible.

**Explanation:** GPU power limits are enforced at multiple levels:
1. **BIOS/EC level** - System-wide limits based on thermal design
2. **VBIOS level** - GPU firmware limits
3. **Driver level** - NVIDIA/AMD driver enforcement

OmenCore can only control what HP exposes via WMI/EC. The "75W limit" is likely enforced by:
- The system EC which has safety limits
- The VBIOS which may have been flashed but doesn't bypass EC
- NVIDIA GPU Boost 4.0/5.0 which reads total board power from EC

**Workaround:** Some users have had success with:
- ThrottleStop (for CPU PL limits)
- NVIDIA Profile Inspector (for some GPU limits)
- HP proprietary tools that have deeper EC access

---

### TCC Offset (Temperature Limit) Resets on Reboot
**User Feedback:** "TjMax set to 80°C but CPU still reached 88°C. After restart, resets to 100°C."

**Status:** Expected hardware behavior, improved restoration in beta3.

**Explanation:** TCC (Thermal Control Circuit) offset is a **volatile MSR register** - the CPU BIOS resets it to 0 on every boot. This is by design at the hardware level.

OmenCore attempts to restore your saved TCC offset on startup, but:
1. **PawnIO driver must load first** - If the driver isn't ready, restoration fails
2. **Timing-sensitive** - There's a retry mechanism (up to 8 attempts over ~30 seconds)
3. **Check the log** - Look for "TCC restore check" entries to see if restoration attempted

**Improvements in beta3:**
- Increased retry attempts from 5 to 8
- Enhanced logging for debugging restoration issues
- Better diagnostics for why restoration might fail

**Note:** The slider shows "Temperature Limit" which is `TjMax - Offset`. So if TjMax is 100°C and you want an 80°C limit, you're actually setting a 20°C offset.

---

### Custom Fan Curve 100% Not Reaching Max RPM
**User Feedback:** "Custom curve set to 100% at 80°C but fans won't spin beyond ~4500 RPM."

**Status:** Expected WMI limitation on some models.

**Explanation:** Custom fan curves use `SetFanLevel(0-55)` which maps percentage to "krpm level":
- 100% → level 55 → ~5500 RPM theoretical max
- But **BIOS may cap lower** than hardware capability

**Why "Max" preset works differently:**
- Max preset uses `SetFanMax(true)` - a special BIOS command that bypasses level limits
- This tells the BIOS "ignore thermal policy, run fans at absolute maximum"

**Solution:** If you need truly maximum fan speed at high temps:
1. Use the **Max** preset instead of custom curve
2. Custom curves are designed for quiet profiles and graduated control
3. Consider using Max preset during heavy gaming, then switch to curve preset for normal use

---

### OGH Power Settings Not Available
**User Feedback:** "How do I configure Smart Performance Gain, Maximum Battery Drain, and Chassis Temperature Limit like in OGH?"

**Status:** These features are **HP OMEN Gaming Hub exclusive**.

**Explanation:** These settings are NOT exposed through standard WMI/BIOS interfaces:
- **Smart Performance Gain (SPG)** - HP's proprietary power algorithm
- **Maximum Battery Drain** - EC-level power limit during battery discharge  
- **Chassis Temperature Limit** - Thermal sensors HP doesn't expose publicly

OmenCore uses only publicly documented WMI interfaces that HP exposes for fan/performance control. The advanced power features require HP's proprietary SDK which is not publicly available.

**What OmenCore CAN control:**
- Performance modes (Default/Balanced/Performance)
- Fan presets and custom curves
- GPU power boost (TGP/PPAB on supported models)
- TCC offset (temperature throttling limit)
- Keyboard backlighting (model-dependent)

---

### Desktop RGB Lighting Control Not Available
**User Feedback:** "OMEN desktop lighting - RGB fan control, interior strip light control, omen logo lights - is that planned?"

**Status:** Not yet implemented. Planned for v1.6+.

**Explanation:** OMEN desktops (45L, 25L, etc.) have a dedicated RGB controller that manages:
- RGB case fans
- Interior LED strip
- OMEN logo lighting
- Front panel accents

This uses a different protocol than laptop keyboard backlighting. Initial research shows:
- OpenRGB has partial support for OMEN desktop RGB
- May use USB HID or WMI commands
- Need hardware access for reverse engineering

**Planned:** Desktop RGB support is on the v1.6 roadmap. Community contributions welcome!

---

### Desktop Multi-Fan Control (AIO, Chassis Fans)
**User Feedback:** "I have multiple fans and my CPU fan is actually an AIO - theres no settings for that yet."

**Status:** Not yet implemented. Investigating for v1.6.

**Explanation:** OMEN desktops with upgraded cooling (aftermarket AIO, extra chassis fans) expose these as separate controllable zones. Currently OmenCore:
- Detects fan speeds via LibreHardwareMonitor (CPU fan, chassis fans)
- Only controls fans through HP's 2-zone WMI interface (CPU/GPU zones)

**What's Needed:**
- Identify EC registers or WMI methods for individual fan zones
- Add UI for per-fan curve control (like Acer Nitro Sense)
- Support for AIO pump speed control (often should be max/static)

**Workaround:** For AIO pumps, consider:
- Setting pump PWM header to "full speed" in BIOS
- Using the motherboard's fan control for chassis fans
- OmenCore's fan profiles still work for the HP-controlled zones

---

### Feature Toggles Don't Hide UI Tabs
**User Feedback:** "Hide unused modules doesn't work - unchecked modules are still visible."

**Status:** By design - feature toggles disable services, not UI.

**Explanation:** The feature toggles in Settings > Features are designed to:
- ✅ Stop background services from running
- ✅ Reduce CPU/memory usage
- ❌ NOT hide navigation tabs

**Reason:** Keeping tabs visible lets you:
- See what features are available
- Re-enable features without hunting for hidden settings
- Access diagnostic info even when service is disabled

**Future consideration:** We could add a "Hide disabled features" option that removes tabs entirely, but this would need careful UX design to ensure users can find the settings again.

---

### LibreHardwareMonitor CPU Sensor Issues
**User Feedback:** "I see a system module called librehardwaremonitor not properly working and is likely the reason why it says there's a CPU temp sensor issue."

**Status:** Known issue on some systems.

**Explanation:** LibreHardwareMonitor (LHM) requires:
- Administrator privileges (OmenCore requests this)
- Specific CPU sensor drivers
- Some CPUs/laptops have locked sensor access

**Workaround:**
- Ensure OmenCore is running as administrator
- If using latest Intel CPUs (Meteor Lake/Lunar Lake), LHM may need updates
- Check if CPU temp shows in Task Manager - if not, it's a system-level restriction

---

### Memory Usage (~400MB)
**User Feedback:** "RAM usage is around 400MB which seems large for this utility."

**Status:** Expected for current architecture.

**Explanation:**
- .NET 8 WPF runtime has baseline overhead (~100-150MB)
- LibreHardwareMonitor library for hardware monitoring (~50MB)
- WPF UI with charts, graphs, and styling (~100MB)
- Services and caching for responsiveness

**Comparison:**
- OMEN Gaming Hub: 200-400MB
- MSI Afterburner + RTSS: 150-300MB
- HWiNFO64: 50-150MB

**Future Consideration:** A separate lightweight daemon + GUI architecture could reduce idle memory when GUI is hidden, but this would be a major architectural change.

---

### Keyboard Lighting on Some Models
**User Feedback:** "Whenever I change the keyboard lighting pattern it doesn't change, it's stuck on Omen Red. My model is WD0012TX."

**Status:** Model-specific WMI limitation.

**Explanation:** HP OMEN keyboard lighting works through:
1. **WMI BIOS** (preferred) - Works on most models
2. **EC Direct** (experimental) - Dangerous, causes crashes on some models

The WD0012TX and similar models may have:
- Non-standard WMI implementation
- Different EC register layout
- Firmware that ignores color commands

**Troubleshooting:**
1. Try "Apply" multiple times
2. Check Settings > Features > Keyboard Lighting toggle
3. **DO NOT** enable EC keyboard - it can cause display issues

---

## 🔨 Technical Changes

### Event-Driven UI Synchronization
Services now raise events when presets/modes are applied, allowing:
- Real-time UI updates across all views (sidebar, tray, dashboard)
- Proper sync when changes come from power automation
- Decoupled architecture (services don't need to know about UI)

### Startup Cleanup
The app now proactively cleans up old startup methods:
- Registry Run key
- Startup folder shortcuts
- Ensures only Task Scheduler method is used

This prevents boot failures caused by conflicting startup entries.

---

## 📁 Files Changed in Beta3

| File | Change Type |
|------|-------------|
| `OmenCoreApp.csproj` | Version bump to 1.5.0 |
| `ViewModels/MainViewModel.cs` | Hotkey logging fix, log display fix, UI sync events |
| `Services/FanService.cs` | PresetApplied event, fan restore on dispose |
| `Services/PerformanceModeService.cs` | ModeApplied event |
| `ViewModels/SettingsViewModel.cs` | Startup cleanup, EC keyboard warning |
| `ViewModels/SystemControlViewModel.cs` | SelectModeByNameNoApply method |
| `Hardware/LibreHardwareMonitorImpl.cs` | CPU Core #1 temp, GPU Core temp, **Intel Arc support**, **NVML crash protection** |
| `installer/OmenCoreInstaller.iss` | Task scheduler startup, cleanup on uninstall |
| `App.xaml.cs` | Dispose MainViewModel on exit, **NVML-specific crash dialog** |

---

## 🔄 Upgrade Notes

### From v1.5.0-beta2
- **Startup entries will be cleaned up** - The app will remove old registry and startup folder entries, keeping only the scheduled task
- If OmenCore doesn't auto-start after upgrade, re-enable "Start with Windows" in Settings

### From v1.4.x
- All beta1 and beta2 changes apply
- Review Settings > Features section for new options
