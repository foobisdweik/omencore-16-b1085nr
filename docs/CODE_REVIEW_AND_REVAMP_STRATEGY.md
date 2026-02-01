# OmenCore Comprehensive Code Review & Revamp Strategy

**Version:** 2.5.1  
**Review Date:** February 1, 2026  
**Reviewer:** Senior Software Engineer  
**Last Stable Reference:** v2.3.2  

---

## Executive Summary

OmenCore has experienced significant regressions since v2.3.2, particularly in fan control functionality, temperature monitoring, and application performance. This review identifies the root causes, provides detailed analysis, and outlines an actionable roadmap to restore stability.

### Critical Findings Overview

| Category | Issues Found | Severity |
|----------|--------------|----------|
| Fan Control | 7 | 🔴 Critical |
| Temperature Monitoring | 4 | 🔴 Critical |
| Startup Performance | 5 | 🟠 High |
| Global Hotkeys | 2 | 🟠 High |
| Hardware Worker | 4 | 🟡 Medium |
| Test Coverage | 6 | 🟡 Medium |

---

## Part 1: Bug Impact Analysis

### GitHub Issues Analysis

#### Issue #52 - Fan Diagnostics Broken Since v2.3.2
**Status:** 🔴 Critical  
**Reporter:** karaokki  
**Model:** HP OMEN 16-wf0156TX (i5-13500HX, RTX 4060)

**Root Cause Analysis:**
- Fan diagnostics worked in v2.3.2 but broke in subsequent versions
- The `FanVerificationService` was modified in v2.4.0+ to add verification loops
- These changes introduced race conditions with the curve engine

**Evidence from Logs:**
```
[Monitor] [FanDebug] No fan sensors found via LibreHardwareMonitor. Hardware: []
Read-back verification failed. Expected mode: 0x2, got: 0x0
```

**Fix Required:**
1. Revert to simpler verification in `FanVerificationService`
2. Add proper diagnostic mode flag that truly suspends all curve operations
3. Increase verification timeout for slower BIOS implementations

---

#### Issue #53 - Hotkeys Misbehave (Ctrl+S triggers mode change)
**Status:** 🟠 High  
**Reporter:** its-urbi

**Root Cause Analysis:**
- `HotkeyService.RegisterDefaultHotkeys()` registers `Ctrl+S` globally
- This conflicts with standard save shortcuts in all applications
- Global hotkeys should only fire when OmenCore window is focused OR in specific contexts

**Code Location:** [HotkeyService.cs](../src/OmenCoreApp/Services/HotkeyService.cs#L160-L170)

```csharp
// PROBLEM: This registers globally, not window-specific
RegisterHotkey(HotkeyAction.ApplySettings, ModifierKeys.Control, Key.S);
```

**Fix Required:**
1. Remove `Ctrl+S` from global hotkey registration
2. Implement window-focused hotkey handling instead
3. Use OMEN-specific key combinations (e.g., `Ctrl+Shift+Alt+S`)

---

#### Issue #54 - OMEN MAX 16 ak0003nr: Almost Nothing Works
**Status:** 🔴 Critical  
**Reporter:** snowfallhateall  
**Model:** OMEN 16 MAX ak0003nr (AMD HX 375 + RTX 5080)

**Root Cause Analysis:**
This is a **2025 model with new WMI BIOS interface** that OmenCore doesn't fully support:
1. New thermal policy version (V2/V3) not properly detected
2. AMD Ryzen AI 9 HX 375 (Strix Point) has different MSR addresses
3. RTX 5080 requires updated NVAPI calls
4. Fan commands use new register addresses

**Evidence:**
- Fan curves, performance profiles, keyboard lighting all fail
- Only basic LibreHardwareMonitor readings work
- GPU mode detection incorrect (shows iGPU when on hybrid)

**Fix Required:**
1. Add detection for 2025 OMEN MAX models (BIOS version F.06+)
2. Implement V2 WMI command sequences from `CMD_FAN_GET_LEVEL_V2` (0x37) and `CMD_FAN_GET_RPM` (0x38)
3. Add Strix Point CPU detection in sensor fallbacks
4. Update NVAPI for Ada/Blackwell architecture GPUs

---

#### Issue #55 - Fan Speed Display Issues
**Status:** 🔴 Critical  
**Reporter:** Sailu49  
**Model:** HP OMEN 16-wd0xxx (i7-13620H, RTX 4060)

**Root Cause Analysis:**
Fan speed readings show impossible values (screenshots suggest incorrect parsing).

**Technical Root Causes:**
1. **krpm → RPM conversion bug** in `WmiFanController.ReadFanSpeeds()`
2. **V2 systems return raw RPM** but code assumes level * 100
3. **Byte order mismatch** in newer BIOS versions

---

### Discord User Reports Analysis

#### Report 1: Banana Peel - OMEN 16-ap0xxx (Chinese Market)
**Model:** Ryzen 8945HX + RTX 5060

**Issues Reported:**
- 15 seconds startup time (expected: 1-3s)
- UI lags when dragged
- Fan speeds show 3300 RPM constantly
- Log spamming
- "Limited mode" warning despite uninstalling OGH

**Root Causes Identified:**

1. **Slow Startup (15s):**
   - Hardware worker initialization: 1.5s mandatory delay + 4s wait loop
   - Sequential device discovery (Corsair, Logitech, Razer)
   - WMI heartbeat sequence: 600ms
   - LibreHardwareMonitor full scan: 2-5s

2. **UI Lag:**
   - Excessive logging to file during drag operations
   - BeginInvoke backlog in `HardwareMonitoringService`
   - Missing dispatcher priority optimization

3. **Fan Speed Display (3300 constant):**
   - Fallback estimation logic kicking in due to WMI read failures
   - `_isMaxModeActive` flag causing RPM estimation instead of real reads

4. **Limited Mode Warning:**
   - Occurs when `OmenCap.exe` is not running
   - Without OGH, WMI BIOS commands may be restricted

**Log Evidence:**
```
[Monitor] [Worker] Hardware worker executable not found, falling back to in-process
[Monitor] PawnIO CPU temperature fallback unavailable - will use LibreHardwareMonitor only
```

---

#### Report 2: Victus 16 s0044ax - Max Fan Not Working, Absurd RPM
**Issues:**
- Max fan option not working
- CPU fan showing 20297 RPM (!!)
- GPU fan showing 78 RPM

**Root Cause:**
- **20297 RPM** indicates raw 16-bit value being parsed incorrectly
- **78 RPM** suggests only low byte being read (0x4E = 78 from actual 5000+ RPM)

**Code Location:** [HpWmiBios.cs](../src/OmenCoreApp/Hardware/HpWmiBios.cs#L509)

```csharp
// Current code - may have byte order issues
int fan1Rpm = rpmResult[0] | (rpmResult[1] << 8);

// Possible fix for big-endian systems:
int fan1Rpm = (rpmResult[0] << 8) | rpmResult[1];
```

---

## Part 2: Root Causes of Regressions

### 2.1 Architectural Issues

#### Issue A: Circular Dependency in Fan Verification

**Location:** `WmiFanController.VerifyMaxAppliedWithRetries()`

```csharp
// PROBLEM: Verification uses the same data source it's trying to verify
private bool VerifyMaxAppliedWithRetries()
{
    for (int i = 0; i < 3; i++)
    {
        Thread.Sleep(500);
        var fans = ReadFanSpeeds().ToList();  // This estimates RPM when real data unavailable!
        
        bool cpuOk = fans[0].DutyCyclePercent >= 90 || fans[0].SpeedRpm >= 3000;
        // ...
    }
}
```

The `ReadFanSpeeds()` method falls back to estimation when WMI fails:

```csharp
// In WmiFanController.ReadFanSpeeds():
if (_isMaxModeActive)
{
    // Estimate max RPM when in max mode
    return new[] { 
        new FanInfo { SpeedRpm = 5500, DutyCyclePercent = 100, FanType = "CPU" },
        // ...
    };
}
```

**Result:** Verification always passes because it's verifying estimated data, not real hardware state.

---

#### Issue B: Fire-and-Forget Async in Constructor

**Location:** `LibreHardwareMonitorImpl`

```csharp
private void InitializeWorker()
{
    // Fire-and-forget - no way to await this
    _ = InitializeWorkerAsync();
}
```

This causes:
1. Race conditions during startup
2. Up to 4 seconds of blocking wait in `ReadSampleAsync()`
3. Unpredictable initialization order

---

#### Issue C: Inadequate Error Handling in WMI Communication

**Location:** `HpWmiBios.SendBiosCommand()`

The 5-second timeout added in v2.4.0 helps prevent UI freezes, but:
- No retry logic for transient failures
- Silent fallback to cached/estimated values
- User not informed when WMI is non-functional

---

### 2.2 Logic Flaws

#### Flaw 1: Fan Curve Fallback Uses First Point Instead of Last

**Fixed in v2.2.3** but worth documenting:

```csharp
// OLD (Bug): Used FirstOrDefault - returned lowest temp/lowest fan%
var targetPoint = curveList.FirstOrDefault(p => p.TemperatureC >= maxTemp);

// FIXED: Use Last when temp exceeds all points
var targetPoint = curveList.LastOrDefault(p => p.TemperatureC <= maxTemp) 
                  ?? curveList.Last();  // Highest fan speed as safety
```

---

#### Flaw 2: RPM Parsing Assumptions

**Location:** Multiple files

```csharp
// Assumption: WMI returns level 0-55, multiply by 100 for RPM
fan1Rpm = fanLevel.Value.fan1 * 100;

// Reality on V2 systems: WMI returns actual RPM or different encoding
```

**Models Affected:**
- OMEN MAX 2025 (V2 interface)
- OMEN Transcend
- Some Victus 16/17 models

---

### 2.3 Performance Issues

#### Startup Time Breakdown (Observed: 15s, Expected: <3s)

| Component | Time (ms) | Notes |
|-----------|-----------|-------|
| Worker startup delay | 1,500 | Mandatory in `HardwareWorkerClient` |
| Worker connection wait | 4,000 | If initialization slow |
| LibreHardwareMonitor scan | 2,000-5,000 | Full hardware enumeration |
| WMI heartbeat | 600 | 3 attempts × 200ms |
| Corsair discovery | 1,000-2,000 | USB enumeration |
| Logitech discovery | 1,000-2,000 | HID enumeration |
| Razer discovery | 500-1,000 | SDK initialization |
| **Total (worst case)** | **~16,000** | |

---

## Part 3: Code Quality Assessment

### 3.1 Positive Aspects

✅ **Good separation of concerns** - Services, Hardware, ViewModels layers  
✅ **Comprehensive logging** - LoggingService with levels  
✅ **Safety features** - Desktop detection, thermal protection  
✅ **Multi-backend support** - WMI, EC, OGH proxy  
✅ **Crash isolation** - Out-of-process hardware worker  

### 3.2 Areas Needing Improvement

#### Missing Abstractions

| Current | Recommended |
|---------|-------------|
| Direct WMI calls scattered | Unified `IWmiService` interface |
| Hardcoded register addresses | Model-specific register maps |
| Mixed sync/async patterns | Consistent async throughout |

#### Error Handling Gaps

- WMI timeouts return `null` without user notification
- EC read failures silently fall back to estimates
- No circuit breaker pattern for repeated failures

#### Memory Management

- `LibreHardwareMonitor` instances not always disposed properly
- Event handler leaks in ViewModels
- Large diagnostic history buffers (120 samples = ~10MB)

### 3.3 Test Coverage Analysis

**Current Coverage:**
- 66 unit tests total
- Focus on RGB providers, settings, UI components
- **Gap:** No tests for `WmiFanController`, `FanService`, or `HpWmiBios`

**Missing Test Categories:**

| Category | Status | Priority |
|----------|--------|----------|
| Fan control logic | ❌ Missing | 🔴 Critical |
| Temperature parsing | ❌ Missing | 🔴 Critical |
| WMI command encoding | ❌ Missing | 🔴 Critical |
| Worker IPC | ❌ Missing | 🟠 High |
| Curve interpolation | ❌ Missing | 🟠 High |
| Startup sequencer | ⚠️ Partial | 🟡 Medium |
| RGB lighting | ✅ Covered | ✅ OK |
| Settings persistence | ✅ Covered | ✅ OK |

---

## Part 4: Proposed Fixes & Prioritization

### 🔴 Priority 1: Critical (Fix Immediately)

#### Fix 1.1: RPM Parsing for V2 Systems
**Effort:** 2-4 hours  
**Files:** `HpWmiBios.cs`, `WmiFanController.cs`

```csharp
// Add to HpWmiBios.cs
private (int fan1Rpm, int fan2Rpm)? TryGetFanRpmV2()
{
    try
    {
        var result = SendBiosCommand(BiosCmd.Default, CMD_FAN_GET_RPM, new byte[4], 128);
        if (result == null || result.Length < 4) return null;
        
        // Try little-endian first
        int fan1 = result[0] | (result[1] << 8);
        int fan2 = result[2] | (result[3] << 8);
        
        // Sanity check - RPM should be 0-8000
        if (fan1 > 0 && fan1 < 8000 && fan2 > 0 && fan2 < 8000)
            return (fan1, fan2);
        
        // Try big-endian
        fan1 = (result[0] << 8) | result[1];
        fan2 = (result[2] << 8) | result[3];
        
        if (fan1 > 0 && fan1 < 8000 && fan2 > 0 && fan2 < 8000)
            return (fan1, fan2);
        
        _logging?.Warn($"Invalid RPM values: raw={BitConverter.ToString(result)}");
        return null;
    }
    catch (Exception ex)
    {
        _logging?.Error($"GetFanRpmV2 failed: {ex.Message}");
        return null;
    }
}
```

---

#### Fix 1.2: Break Circular Verification Dependency
**Effort:** 4-6 hours  
**Files:** `WmiFanController.cs`

```csharp
// BEFORE: Verification uses same method that estimates
private bool VerifyMaxAppliedWithRetries()
{
    var fans = ReadFanSpeeds().ToList();  // May return estimates!
    // ...
}

// AFTER: Use raw hardware read without fallback
private bool VerifyMaxAppliedWithRetries()
{
    // Record baseline BEFORE command was sent
    _lastCommandRpmBefore = GetRawFanRpm();
    
    for (int i = 0; i < 3; i++)
    {
        Thread.Sleep(500);
        
        // Get RAW reading, no fallback
        var rawRpm = GetRawFanRpm();
        if (rawRpm == null)
        {
            _logging?.Warn($"Verification attempt {i+1}: Cannot read raw RPM");
            continue;
        }
        
        // Verify RPM INCREASED significantly
        if (_lastCommandRpmBefore != null)
        {
            int increase = rawRpm.Value - _lastCommandRpmBefore.Value;
            if (increase >= 1000)  // Must increase by 1000+ RPM
            {
                _logging?.Info($"Max verified: {_lastCommandRpmBefore} -> {rawRpm} (+{increase} RPM)");
                return true;
            }
        }
        
        // Absolute check: must be > 4000 RPM for max
        if (rawRpm >= 4000)
        {
            _logging?.Info($"Max verified by absolute RPM: {rawRpm}");
            return true;
        }
    }
    
    _logging?.Warn("Max verification failed after 3 attempts");
    return false;
}

private int? GetRawFanRpm()
{
    // Only use WMI BIOS, no estimation fallback
    var rpmData = _wmiBios.GetFanRpm();
    if (rpmData == null) return null;
    return Math.Max(rpmData.Value.fan1Rpm, rpmData.Value.fan2Rpm);
}
```

---

#### Fix 1.3: Sanity Check for RPM Values
**Effort:** 1-2 hours  
**Files:** `WmiFanController.cs`

```csharp
private IEnumerable<FanInfo> ReadFanSpeeds()
{
    // ... existing code to read RPM ...
    
    // Add sanity validation
    if (fan1Rpm > 8000 || fan1Rpm < 0)
    {
        _logging?.Warn($"Invalid CPU fan RPM: {fan1Rpm}, marking as unavailable");
        fan1Rpm = -1;  // Signal invalid
    }
    
    if (fan2Rpm > 8000 || fan2Rpm < 0)
    {
        _logging?.Warn($"Invalid GPU fan RPM: {fan2Rpm}, marking as unavailable");
        fan2Rpm = -1;
    }
    
    // Only return valid readings
    if (fan1Rpm > 0)
        yield return new FanInfo { FanType = "CPU", SpeedRpm = fan1Rpm, ... };
    // ...
}
```

---

### 🟠 Priority 2: High (Fix This Week)

#### Fix 2.1: Remove Global Ctrl+S Hotkey
**Effort:** 30 minutes  
**Files:** `HotkeyService.cs`

```csharp
public void RegisterDefaultHotkeys()
{
    // Keep these safe combinations
    RegisterHotkey(HotkeyAction.ToggleFanMode, ModifierKeys.Control | ModifierKeys.Shift, Key.F);
    RegisterHotkey(HotkeyAction.TogglePerformanceMode, ModifierKeys.Control | ModifierKeys.Shift, Key.P);
    RegisterHotkey(HotkeyAction.ToggleWindow, ModifierKeys.Control | ModifierKeys.Shift, Key.O);
    
    // REMOVE: This conflicts with save in every application
    // RegisterHotkey(HotkeyAction.ApplySettings, ModifierKeys.Control, Key.S);
    
    // Replace with safe alternative
    RegisterHotkey(HotkeyAction.ApplySettings, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt, Key.S);
}
```

---

#### Fix 2.2: Reduce Startup Time
**Effort:** 4-8 hours  
**Files:** `HardwareWorkerClient.cs`, `MainViewModel.cs`, `LibreHardwareMonitorImpl.cs`

**Changes:**

1. **Reduce worker startup delay:**
```csharp
// HardwareWorkerClient.cs
private const int WorkerStartupDelayMs = 500;  // Was 1500
```

2. **Parallelize device discovery:**
```csharp
// MainViewModel.cs - InitializePeripherals()
await Task.WhenAll(
    Task.Run(() => InitializeCorsairAsync()),
    Task.Run(() => InitializeLogitechAsync()),
    Task.Run(() => InitializeRazerAsync())
);
```

3. **Lazy-load non-critical services:**
```csharp
// Defer RGB discovery until Lighting tab is accessed
private CorsairDeviceService? _corsairDeviceService;
public CorsairDeviceService CorsairDeviceService => 
    _corsairDeviceService ??= new CorsairDeviceService(_logging);
```

4. **Add progress feedback:**
```csharp
// StartupSequencer - report progress to splash screen
ProgressChanged?.Invoke(this, new StartupProgressEventArgs 
{ 
    TaskName = "Initializing hardware...",
    PercentComplete = 30 
});
```

---

#### Fix 2.3: Add 2025 Model Detection
**Effort:** 6-8 hours  
**Files:** `HpWmiBios.cs`, `SystemInfoService.cs`

```csharp
// SystemInfoService.cs
public bool Is2025OmenMax()
{
    // Check BIOS version (F.05+), CPU (Strix Point), or model string
    return _biosVersion.StartsWith("F.0") && 
           int.Parse(_biosVersion.Substring(2)) >= 5;
}

// HpWmiBios.cs - Use V2 commands for 2025 models
private bool UseV2Commands => _systemInfo.Is2025OmenMax();

public (int fan1Rpm, int fan2Rpm)? GetFanRpm()
{
    if (UseV2Commands)
        return TryGetFanRpmV2();
    else
        return TryGetFanRpmV1();
}
```

---

### 🟡 Priority 3: Medium (Fix This Sprint)

#### Fix 3.1: Add Fan Control Unit Tests
**Effort:** 8-12 hours  
**Files:** New test files

```csharp
// OmenCoreApp.Tests/Hardware/WmiFanControllerTests.cs
public class WmiFanControllerTests
{
    [Fact]
    public void SetFanMax_ShouldCallSetFanMaxFirst_ThenVerify()
    {
        // Arrange
        var mockWmiBios = new Mock<IWmiBios>();
        mockWmiBios.Setup(x => x.SetFanMax(true)).Returns(true);
        mockWmiBios.Setup(x => x.GetFanRpm()).Returns((4500, 4200));
        
        var controller = new WmiFanController(mockWmiBios.Object, ...);
        
        // Act
        var result = controller.ApplyPreset(FanPresets.Max);
        
        // Assert
        Assert.True(result);
        mockWmiBios.Verify(x => x.SetFanMax(true), Times.Once);
    }
    
    [Theory]
    [InlineData(20297, false)]  // Too high - invalid
    [InlineData(78, false)]     // Too low - invalid
    [InlineData(3500, true)]    // Valid
    [InlineData(5000, true)]    // Valid
    public void ReadFanSpeeds_ShouldValidateRpmRange(int rpm, bool isValid)
    {
        // Test RPM sanity validation
    }
}
```

---

#### Fix 3.2: Implement Circuit Breaker for WMI
**Effort:** 4-6 hours  
**Files:** `HpWmiBios.cs`

```csharp
public class WmiCircuitBreaker
{
    private int _consecutiveFailures = 0;
    private DateTime _lastFailure = DateTime.MinValue;
    private bool _isOpen = false;
    
    private const int FailureThreshold = 3;
    private const int CooldownSeconds = 30;
    
    public bool IsOpen => _isOpen && 
        (DateTime.Now - _lastFailure).TotalSeconds < CooldownSeconds;
    
    public void RecordSuccess()
    {
        _consecutiveFailures = 0;
        _isOpen = false;
    }
    
    public void RecordFailure()
    {
        _consecutiveFailures++;
        _lastFailure = DateTime.Now;
        
        if (_consecutiveFailures >= FailureThreshold)
        {
            _isOpen = true;
            _logging?.Warn($"WMI circuit breaker OPEN after {_consecutiveFailures} failures");
        }
    }
}
```

---

## Part 5: Regression Detection Strategy

### 5.1 Automated Regression Tests

Add these tests to the CI pipeline:

```csharp
// Tests that would have caught v2.3.2+ regressions
[Fact]
public void FanCurve_WhenTempExceedsAllPoints_ShouldUseHighestFanSpeed()
{
    var curve = new[] {
        new FanCurvePoint { TemperatureC = 40, FanPercent = 30 },
        new FanCurvePoint { TemperatureC = 80, FanPercent = 100 }
    };
    
    // At 95°C (exceeds all points), should use 100%, not 30%
    var result = FanCurveService.InterpolateFanSpeed(curve, 95);
    Assert.Equal(100, result);
}

[Fact]
public void RpmParsing_ShouldRejectImpossibleValues()
{
    var rawBytes = new byte[] { 0xE9, 0x4F, 0x4E, 0x00 };  // Would parse to 20457
    var rpm = ParseFanRpm(rawBytes);
    
    Assert.True(rpm < 8000 || rpm == -1, "Should reject RPM > 8000");
}

[Fact]
public void MaxVerification_ShouldNotUseEstimatedValues()
{
    // Mock WMI to return null (unavailable)
    var mock = new Mock<IHpWmiBios>();
    mock.Setup(x => x.GetFanRpm()).Returns((null as (int, int)?));
    
    var controller = new WmiFanController(mock.Object, ...);
    controller.ApplyPreset(FanPresets.Max);
    
    // Verification should FAIL, not pass with estimated values
    Assert.False(controller.IsMaxModeVerified);
}
```

### 5.2 Hardware-in-the-Loop Testing

Create a diagnostic test mode that:
1. Sends fan commands
2. Waits 3 seconds
3. Reads back RPM from multiple sources (WMI, EC, LibreHardwareMonitor)
4. Compares expected vs actual
5. Reports model-specific success rate

### 5.3 Telemetry for Regression Detection

```csharp
public class FanControlTelemetry
{
    public string ModelId { get; set; }
    public string BiosVersion { get; set; }
    public string CommandType { get; set; }
    public bool CommandSuccess { get; set; }
    public int? RpmBefore { get; set; }
    public int? RpmAfter { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

// Collect and analyze for patterns:
// - Models with high failure rates
// - Commands that succeed but don't change RPM
// - Timing issues
```

---

## Part 6: Testing & Validation Plan

### 6.1 Unit Tests (Add 50+ New Tests)

| Test File | Coverage Target | Priority |
|-----------|-----------------|----------|
| `WmiFanControllerTests.cs` | Fan commands, verification | 🔴 Critical |
| `HpWmiBiosTests.cs` | Command encoding, parsing | 🔴 Critical |
| `FanServiceTests.cs` | Curve application, thermal protection | 🔴 Critical |
| `LibreHardwareMonitorTests.cs` | Sensor parsing, fallbacks | 🟠 High |
| `HardwareWorkerClientTests.cs` | IPC, restart logic | 🟠 High |
| `StartupSequencerTests.cs` | Task ordering, retries | 🟡 Medium |

### 6.2 Integration Tests

```csharp
// Integration test requiring actual hardware (skip in CI)
[Fact]
[Trait("Category", "Integration")]
[Trait("RequiresHardware", "true")]
public async Task FanControl_EndToEnd_ShouldChangeActualFanSpeed()
{
    // Only run on development machines with hardware
    Skip.IfNot(Environment.GetEnvironmentVariable("RUN_HW_TESTS") == "1");
    
    var fanService = new FanService(...);
    var initialRpm = await fanService.GetCurrentRpmAsync();
    
    fanService.ApplyPreset(FanPresets.Max);
    await Task.Delay(5000);  // Wait for fans to spin up
    
    var finalRpm = await fanService.GetCurrentRpmAsync();
    
    Assert.True(finalRpm > initialRpm + 500, 
        $"Expected RPM increase, but got {initialRpm} -> {finalRpm}");
}
```

### 6.3 Model-Specific Test Matrix

| Model Family | WMI V1 | WMI V2 | EC Direct | Priority |
|--------------|--------|--------|-----------|----------|
| OMEN 15 2018-2020 | ✅ Test | ❌ N/A | ✅ Test | 🟡 Medium |
| OMEN 16 2021-2023 | ✅ Test | ⚠️ Partial | ✅ Test | 🔴 Critical |
| OMEN 16 2024 | ⚠️ Partial | ✅ Test | ❌ Blocked | 🔴 Critical |
| OMEN MAX 2025 | ❌ Fails | ✅ Test | ❌ Blocked | 🔴 Critical |
| Victus 16/17 | ✅ Test | ⚠️ Varies | ⚠️ Varies | 🟠 High |

---

## Part 7: Release Timeline & Tagging Plan

### Phase 1: Emergency Patch (v2.5.2) - ETA: 1 Week

**Goal:** Fix critical fan control regressions

**Scope:**
- [ ] Fix RPM parsing for V2 systems (#55)
- [ ] Fix verification circular dependency (#52)
- [ ] Add RPM sanity checks
- [ ] Remove Ctrl+S global hotkey (#53)
- [ ] Add diagnostic mode for fan testing

**Release Criteria:**
- All existing tests pass
- Fan diagnostics work on v2.3.2-compatible models
- No new regressions in hotkeys

---

### Phase 2: Stability Release (v2.6.0) - ETA: 3 Weeks

**Goal:** Restore v2.3.2-level stability for all supported models

**Scope:**
- [ ] Add 2025 OMEN MAX support (#54)
- [ ] Reduce startup time to <5 seconds
- [ ] Implement WMI circuit breaker
- [ ] Add 50+ unit tests for fan control
- [ ] Fix hardware worker reliability

**Release Criteria:**
- Fan control verified on 5+ model families
- Startup time <5 seconds on reference hardware
- Test coverage >60% for hardware layer

---

### Phase 3: Architecture Improvements (v3.0.0) - ETA: 8 Weeks

**Goal:** Refactor for maintainability and extensibility

**Scope:**
- [ ] Abstract WMI/EC access behind interfaces
- [ ] Implement model-specific register maps
- [ ] Add hardware abstraction layer (HAL)
- [ ] Convert to fully async architecture
- [ ] Implement proper dependency injection
- [ ] Add comprehensive telemetry

**Release Criteria:**
- All tests pass (target: 200+ tests)
- No WPF Dispatcher deadlocks
- Modular architecture allowing per-model customization

---

## Part 8: Versioning Strategy

### Semantic Versioning (Already in Use)

```
MAJOR.MINOR.PATCH

2.5.2 - Patch: Bug fixes only
2.6.0 - Minor: New features, backward compatible
3.0.0 - Major: Breaking changes, architecture refactor
```

### Pre-Release Tags

```
2.6.0-alpha.1 - Internal testing
2.6.0-beta.1  - Public testing, may have bugs
2.6.0-rc.1    - Release candidate, feature complete
2.6.0         - Stable release
```

### Git Branching

```
main          - Stable releases only
develop       - Active development
release/v2.6  - Release preparation
hotfix/v2.5.2 - Emergency fixes for production
feature/xyz   - Feature branches
```

---

## Part 9: Acceptance Criteria for Future Releases

### Minimum Requirements for Patch Releases (x.x.N)

- [ ] All existing tests pass
- [ ] No new compiler warnings
- [ ] Smoke tested on 2+ hardware models
- [ ] Changelog updated
- [ ] No performance regression (startup <10s)

### Minimum Requirements for Minor Releases (x.N.0)

- [ ] All patch requirements
- [ ] New features have unit tests
- [ ] Documentation updated
- [ ] Tested on 5+ hardware models
- [ ] Beta period of 1+ week with community feedback

### Minimum Requirements for Major Releases (N.0.0)

- [ ] All minor requirements
- [ ] Migration guide for breaking changes
- [ ] Architecture review completed
- [ ] Performance benchmarks documented
- [ ] Security audit for kernel driver interactions

---

## Appendix A: File Reference

### Critical Files for Fan Control

| File | Responsibility | Lines |
|------|----------------|-------|
| [WmiFanController.cs](../src/OmenCoreApp/Hardware/WmiFanController.cs) | WMI fan commands | 1,341 |
| [HpWmiBios.cs](../src/OmenCoreApp/Hardware/HpWmiBios.cs) | Low-level WMI | 1,345 |
| [FanService.cs](../src/OmenCoreApp/Services/FanService.cs) | High-level control | 827 |
| [LibreHardwareMonitorImpl.cs](../src/OmenCoreApp/Hardware/LibreHardwareMonitorImpl.cs) | Sensor reading | 1,753 |
| [HardwareWorkerClient.cs](../src/OmenCoreApp/Hardware/HardwareWorkerClient.cs) | IPC client | 510 |

### Test Files to Create

| File | Tests | Priority |
|------|-------|----------|
| `WmiFanControllerTests.cs` | 15+ | 🔴 |
| `HpWmiBiosTests.cs` | 10+ | 🔴 |
| `FanServiceTests.cs` | 15+ | 🔴 |
| `RpmParsingTests.cs` | 10+ | 🔴 |
| `VerificationLogicTests.cs` | 8+ | 🟠 |
| `StartupPerformanceTests.cs` | 5+ | 🟡 |

---

## Appendix B: Immediate Actions Checklist

### This Week

- [ ] Create `hotfix/v2.5.2` branch
- [ ] Fix RPM parsing (Issue #55)
- [ ] Fix verification logic (Issue #52)
- [ ] Remove Ctrl+S hotkey (Issue #53)
- [ ] Add 10 critical unit tests
- [ ] Release v2.5.2-beta.1 for testing

### This Sprint

- [ ] Add 2025 OMEN MAX detection (Issue #54)
- [ ] Reduce startup time
- [ ] Add 40 more unit tests
- [ ] Create hardware test matrix
- [ ] Release v2.6.0-alpha.1

### This Month

- [ ] Complete test coverage for hardware layer
- [ ] Implement telemetry for failure detection
- [ ] Create model-specific documentation
- [ ] Release v2.6.0 stable

---

*Document generated: February 1, 2026*  
*Next review scheduled: After v2.5.2 release*
