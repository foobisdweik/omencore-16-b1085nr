# OmenCore v2.6.0 Roadmap

**Target Release:** Q2 2026
**Status:** Planning
**Last Updated:** January 24, 2026

---

## Overview

Version 2.6.0 represents a **major enhancement release** integrating 21 comprehensive improvements across performance, features, stability, and user experience. This roadmap leverages existing GitHub libraries to minimize development time while maximizing feature impact.

| # | Feature Category | Priority | Effort | Status | Library Leverage |
|---|------------------|----------|--------|--------|------------------|
| 1 | Memory Management | 🔴 Critical | Medium | 📋 **Planned** | .NET Built-in |
| 2 | Hardware Compatibility | 🔴 Critical | Medium | 📋 **Planned** | WMI/Performance Counters |
| 3 | Advanced Monitoring | 🟡 High | Medium | 📋 **Planned** | SharpPcap, LiveCharts2 |
| 4 | Network Monitoring | 🟡 High | Low | 📋 **Planned** | SharpPcap |
| 5 | Storage Health | 🟡 High | Medium | 📋 **Planned** | NVMe CLI Libraries |
| 6 | RGB Enhancements | 🟡 High | Medium | 📋 **Planned** | Existing RGB Framework |
| 7 | Third-Party Integrations | 🟢 Medium | Medium | 📋 **Planned** | Steam API, RTSS SDK |
| 8 | Power Management | 🟢 Medium | Medium | 📋 **Planned** | HP BIOS APIs |
| 9 | Performance Optimization | 🟢 Medium | Low | 📋 **Planned** | .NET Performance Counters |
| 10 | UI/UX Improvements | 🟢 Medium | Low | 📋 **Planned** | Avalonia UI |
| 11 | Testing Infrastructure | 🔵 Low | High | 📋 **Planned** | xUnit, Playwright |
| 12 | Developer Experience | 🔵 Low | Medium | 📋 **Planned** | Roslyn Analyzers |

---

## 1. Memory Management Enhancements 🔴

**Status:** 📋 Planned
**Priority:** Critical - Performance and stability
**Libraries:** .NET Built-in (WeakReference, MemoryCache)
**Files:** `src/OmenCoreApp/Services/MemoryManagementService.cs`

### Issues Addressed:
- Long-running session memory creep (Recommendation #1)
- UI component disposal issues
- Large diagnostic export memory consumption

### Implementation Plan:

#### Phase 1: Memory Profiling Infrastructure
- [ ] Implement `WeakReference` caching for sensor data
- [ ] Add memory pressure monitoring with `GC.GetTotalMemory()`
- [ ] Create diagnostic memory counters
- [ ] Add memory leak detection in debug builds

#### Phase 2: Resource Optimization
- [ ] Bitmap/icon resource pooling with `MemoryCache`
- [ ] UI virtualization for large data grids
- [ ] Automatic cleanup under memory pressure
- [ ] Background garbage collection optimization

#### Phase 3: Memory-Aware Features
- [ ] Intelligent cache size adjustment based on available RAM
- [ ] Memory usage warnings and recommendations
- [ ] Export size limits with compression options

---

## 2. Hardware Compatibility Fixes 🔴

**Status:** 📋 Planned
**Priority:** Critical - User-reported issues
**Libraries:** WMI, Performance Counters
**Files:** `src/OmenCoreApp/Hardware/CompatibilityService.cs`

### Issues Addressed:
- RAM display showing "0/0 GB" (deferred from v2.5.1)
- Victus 16 series sensor reliability (Recommendation #8)
- OMEN 17 2023+ fan control inconsistencies
- Secure Boot conflicts

### Implementation Plan:

#### Phase 1: Enhanced Detection
- [ ] Multi-source memory detection (WMI + Performance Counters)
- [ ] BIOS version-specific command sets
- [ ] Hardware model fingerprinting with firmware detection
- [ ] Fallback mechanisms for failed hardware access

#### Phase 2: Compatibility Layer
- [ ] BIOS/EC memory detection fallback
- [ ] Hardware conflict detection with user guidance
- [ ] Secure Boot compatibility detection
- [ ] Automatic driver/module management

#### Phase 3: Validation Framework
- [ ] Hardware compatibility scoring system
- [ ] User-reported issue tracking
- [ ] Automated compatibility testing

---

## 3. Advanced Monitoring Dashboard 🟡

**Status:** 📋 Planned
**Priority:** High - Enhanced user experience
**Libraries:** LiveCharts2, SharpPcap
**Files:** `src/OmenCoreApp/Views/MonitoringView.axaml`

### Features (Enhanced from Original):

#### Real-time Visualization
- [ ] Temperature history graphs (CPU, GPU, SSD, Storage)
- [ ] Fan speed curves with predictive analysis
- [ ] Power consumption trends with TDP monitoring
- [ ] Memory usage with leak detection indicators

#### Network Monitoring Integration
- [ ] Per-application bandwidth monitoring using SharpPcap
- [ ] Network latency and jitter tracking
- [ ] WiFi signal strength visualization
- [ ] Gaming traffic prioritization indicators

#### Storage Health Dashboard
- [ ] NVMe health metrics (TBW, wear leveling)
- [ ] Storage temperature monitoring
- [ ] I/O performance graphs
- [ ] SMART attribute visualization

#### Customizable Interface
- [ ] Drag-and-drop widget system
- [ ] Collapsible monitoring sections
- [ ] Multiple dashboard layouts (Gaming, Productivity, Benchmarking)
- [ ] Data export to CSV/JSON with compression

---

## 4. Network Monitoring & Optimization 🟡

**Status:** 📋 Planned
**Priority:** High - Performance gaming feature
**Libraries:** SharpPcap (packet capture), NetCoreServer
**Files:** `src/OmenCoreApp/Services/NetworkMonitoringService.cs`

### Implementation Plan:

#### Packet-Level Monitoring
- [ ] Real-time bandwidth monitoring per application using SharpPcap
- [ ] Network interface statistics collection
- [ ] QoS traffic classification and prioritization

#### Gaming Optimization
- [ ] Gaming traffic detection and prioritization
- [ ] Ping/jitter monitoring with historical tracking
- [ ] Network latency optimization recommendations

#### WiFi Management
- [ ] Signal strength monitoring and channel analysis
- [ ] Automatic channel optimization suggestions
- [ ] WiFi vs Ethernet performance comparison

#### Integration Features
- [ ] Steam game network usage tracking
- [ ] Discord voice chat quality monitoring
- [ ] OBS streaming bandwidth optimization

---

## 5. Storage Health & Optimization 🟡

**Status:** 📋 Planned
**Priority:** High - Hardware longevity
**Libraries:** NVMe CLI wrappers, SMART libraries
**Files:** `src/OmenCoreApp/Services/StorageHealthService.cs`

### Implementation Plan:

#### NVMe Health Monitoring
- [ ] Total Bytes Written (TBW) tracking
- [ ] Wear leveling status monitoring
- [ ] Temperature management with fan coordination
- [ ] Power state optimization

#### SMART Integration
- [ ] Attribute monitoring and alerting
- [ ] Predictive failure detection
- [ ] Performance degradation tracking
- [ ] Automated TRIM optimization scheduling

#### Storage Performance
- [ ] I/O throughput monitoring
- [ ] Queue depth analysis
- [ ] Defragmentation automation for HDDs
- [ ] Cache performance metrics

---

## 6. Enhanced RGB Lighting System 🟡

**Status:** 📋 Planned
**Priority:** High - Community requested
**Libraries:** Existing RGB framework with audio analysis
**Files:** `src/OmenCoreApp/Services/RgbService.cs`

### Advanced Effects (Enhanced):

#### Audio-Reactive Lighting
- [ ] Spectrum analysis integration with audio devices
- [ ] Frequency-based color mapping
- [ ] Volume-reactive brightness control
- [ ] Microphone input for voice-reactive effects

#### Temperature-Based Effects
- [ ] CPU/GPU temperature color mapping
- [ ] Thermal gradient visualization
- [ ] Cooling system status indication
- [ ] Performance-based color transitions

#### Advanced Patterns
- [ ] Wave effects with customizable speed/direction
- [ ] Breathing patterns with multi-color transitions
- [ ] Particle effects and animations
- [ ] Custom effect creation tools

#### Zone Control Enhancement
- [ ] Per-key RGB effects for gaming keyboards
- [ ] Logo and accent lighting zones
- [ ] Synchronized multi-device lighting
- [ ] Effect library with import/export

---

## 7. Third-Party Integrations 🟢

**Status:** 📋 Planned
**Priority:** Medium - Ecosystem expansion
**Libraries:** Steam API, RTSS SDK, Discord RPC
**Files:** `src/OmenCoreApp/Services/IntegrationService.cs`

### High Priority Integrations:

#### Steam Integration
- [ ] Game detection and automatic profile switching
- [ ] Steam Overlay compatibility
- [ ] Game-specific performance monitoring
- [ ] Achievement and stats integration

#### RTSS (RivaTuner Statistics Server)
- [ ] Real-time overlay integration
- [ ] Custom sensor data sharing
- [ ] Performance monitoring synchronization
- [ ] Benchmarking data collection

#### Discord Rich Presence
- [ ] System status display
- [ ] Performance metrics sharing
- [ ] Custom status messages
- [ ] Voice chat quality monitoring

#### OBS Studio Plugin
- [ ] Hardware monitoring overlay
- [ ] Streaming performance optimization
- [ ] Real-time alerts for stream health
- [ ] Automated scene switching based on performance

---

## 8. Advanced Power Management 🟢

**Status:** 📋 Planned
**Priority:** Medium - Efficiency optimization
**Libraries:** HP BIOS APIs, WMI Power Management
**Files:** `src/OmenCoreApp/Services/PowerManagementService.cs`

### Intelligent TDP Management
- [ ] Dynamic TDP adjustment based on workload analysis
- [ ] Gaming vs Productivity mode automation
- [ ] Battery optimization for Victus laptops
- [ ] Thermal budget monitoring with predictive cooling

### Power Profile Automation
- [ ] Application-based profile switching
- [ ] Time-of-day power optimization
- [ ] Performance vs Efficiency balancing
- [ ] Power consumption tracking and reporting

---

## 9. Performance Optimization Suite 🟢

**Status:** 📋 Planned
**Priority:** Medium - Sustained performance
**Libraries:** .NET Performance Counters, BenchmarkDotNet
**Files:** `src/OmenCoreApp/Services/PerformanceService.cs`

### CPU Usage Optimization
- [ ] Background thread prioritization
- [ ] Adaptive polling rates based on system load
- [ ] Multi-core optimization with thread affinity
- [ ] GPU-accelerated UI rendering for charts

### Startup Performance
- [ ] Parallel hardware initialization
- [ ] Lazy loading for non-critical components
- [ ] Splash screen with progress indicators
- [ ] Pre-compiled regex patterns for WMI queries

### Memory Optimization
- [ ] Intelligent caching strategies
- [ ] Memory-mapped file usage for large datasets
- [ ] Background cleanup processes
- [ ] Memory usage profiling and reporting

---

## 10. UI/UX Improvements 🟢

**Status:** 📋 Planned
**Priority:** Medium - User experience
**Libraries:** Avalonia UI enhancements
**Files:** `src/OmenCoreApp/Views/`, `src/OmenCoreApp/ViewModels/`

### Accessibility Enhancements
- [ ] High contrast themes for visually impaired users
- [ ] Keyboard navigation improvements
- [ ] Screen reader support
- [ ] Font scaling options

### User Experience
- [ ] Async UI operations with progress indicators
- [ ] Robust error handling with user-friendly messages
- [ ] Settings migration between versions
- [ ] UI state preservation during restarts

### Internationalization
- [ ] Multi-language support foundation
- [ ] Localized hardware terminology
- [ ] RTL language support preparation

---

## 11. Testing Infrastructure 🔵

**Status:** 📋 Planned
**Priority:** Low - Long-term quality
**Libraries:** xUnit, Playwright, Spectre.Console
**Files:** `tests/`, `src/OmenCoreApp.Testing/`

### Automated Testing Framework
- [ ] Hardware simulation framework for unit tests
- [ ] UI automation tests with Playwright
- [ ] Performance regression testing with BenchmarkDotNet
- [ ] Cross-platform CI/CD pipeline

### Quality Assurance
- [ ] Code coverage targets (80%+)
- [ ] Static analysis integration (SonarQube style)
- [ ] API documentation generation
- [ ] Dependency vulnerability scanning

---

## 12. Developer Experience Enhancements 🔵

**Status:** 📋 Planned
**Priority:** Low - Team productivity
**Libraries:** Roslyn Analyzers, Serilog
**Files:** `build/`, `docs/`

### Development Tools
- [ ] Structured logging with correlation IDs
- [ ] Performance metrics collection
- [ ] Remote diagnostics (opt-in)
- [ ] Log analysis tools for support

### Code Quality
- [ ] Code analysis rules and Roslyn analyzers
- [ ] Automated code formatting
- [ ] Dependency management improvements
- [ ] Build performance optimization

---

## Development Timeline

### Phase 1: Critical Fixes (Weeks 1-6)
- Memory management enhancements
- Hardware compatibility fixes
- RAM display fix completion
- Performance optimization foundation

### Phase 2: Core Features (Weeks 7-16)
- Advanced monitoring dashboard with network/storage integration
- Enhanced RGB lighting with audio reactivity
- Third-party integrations (Steam, RTSS, Discord)
- Power management intelligence

### Phase 3: Quality & Polish (Weeks 17-20)
- UI/UX improvements and accessibility
- Testing infrastructure implementation
- Performance benchmarking and optimization
- Documentation and developer experience

### Phase 4: Integration & Testing (Weeks 21-24)
- Cross-platform compatibility validation
- Beta testing program
- Performance regression testing
- Release preparation

---

## Dependencies & Libraries

### Core Dependencies
- **Avalonia UI:** v11.0+ for enhanced UI capabilities
- **LiveCharts2:** Real-time charting and visualization
- **SharpPcap:** Packet-level network monitoring
- **NetCoreServer:** High-performance networking

### Integration Libraries
- **Steam API:** Game detection and integration
- **RTSS SDK:** Real-time statistics overlay
- **Discord RPC:** Rich presence integration
- **NVMe CLI:** Storage health monitoring

### Development Tools
- **xUnit:** Unit testing framework
- **Playwright:** UI automation testing
- **BenchmarkDotNet:** Performance testing
- **Roslyn Analyzers:** Code analysis

---

## Risk Assessment

### High Risk
- Hardware compatibility across diverse HP Omen/Victus models
- Performance impact of real-time network monitoring
- Third-party API integration stability

### Medium Risk
- Memory management changes affecting stability
- UI responsiveness with advanced visualizations
- Cross-platform compatibility challenges

### Low Risk
- RGB lighting enhancements
- Basic power management features
- Developer experience improvements

---

## Success Criteria

### Performance Targets
- **Memory Usage:** < 100MB baseline, < 150MB under load
- **CPU Usage:** < 1% during monitoring, < 5% during intensive operations
- **Startup Time:** < 2 seconds cold start, < 1 second warm start
- **UI Responsiveness:** < 100ms for all interactions

### Feature Completeness
- [ ] All 21 enhancement recommendations integrated
- [ ] Network monitoring with SharpPcap implementation
- [ ] Storage health monitoring with NVMe integration
- [ ] Third-party integrations functional
- [ ] Advanced RGB effects working

### Quality Metrics
- [ ] Test coverage > 80% for new features
- [ ] Zero critical bugs in beta testing
- [ ] Hardware compatibility > 95% of supported models
- [ ] User satisfaction > 4.5/5 in beta feedback

---

## Implementation Notes

### Library Integration Strategy
- **SharpPcap:** Primary network monitoring with fallback to system APIs
- **LiveCharts2:** GPU-accelerated real-time charting
- **Steam API:** Opt-in integration with user consent
- **NVMe Libraries:** Open-source CLI wrappers for cross-platform support

### Backward Compatibility
- All existing features maintained
- Settings migration between versions
- Graceful degradation for missing dependencies
- Opt-in model for new features

### Performance Considerations
- Background processing for non-critical features
- Memory-aware caching and cleanup
- CPU affinity for consistent performance
- Battery optimization for laptop users

This comprehensive roadmap transforms OmenCore v2.6.0 into a feature-rich, performance-optimized, and user-centric hardware control suite while leveraging existing libraries to minimize development time and maximize reliability.
- [ ] Synchronized lighting across components

#### Effect Library
- [ ] Preset effect collection
- [ ] Custom effect creation tools
- [ ] Effect import/export functionality
- [ ] Community effect sharing

---

## 4. MSI Afterburner Integration 🟢

**Status:** 📋 Planned
**Priority:** Medium - Power user feature
**Files:** `src/OmenCoreApp/Hardware/AfterburnerService.cs`

### Planned Features

#### Hardware Monitoring
- [ ] Import MSI Afterburner sensor data
- [ ] GPU temperature, clock speeds, voltages
- [ ] Custom sensor overlay integration
- [ ] Real-time hardware monitoring sync

#### Profile Management
- [ ] Load/save Afterburner profiles
- [ ] Automatic profile switching based on conditions
- [ ] Profile backup and restore
- [ ] Profile conflict detection

---

## 5. Bug Fixes & Stability 🔴

**Status:** 📋 Planned
**Priority:** High - Quality assurance
**Scope:** All components

### Known Issues to Address

#### Fan Control
- [ ] Max preset false positive fixes (deferred from v2.5.1)
- [ ] Fan curve stability improvements
- [ ] BIOS communication reliability

#### Hardware Monitoring
- [ ] CPU temperature accuracy on certain configurations
- [ ] GPU memory detection issues
- [ ] SSD temperature reporting inconsistencies

#### UI/UX
- [ ] Memory leak fixes in long-running sessions
- [ ] UI responsiveness improvements
- [ ] Error handling and user feedback

#### Performance
- [ ] CPU usage optimization
- [ ] Memory usage reduction
- [ ] Startup time improvements

---

## Development Timeline

### Phase 1: Bug Fixes (Weeks 1-4)
- RAM display fix implementation
- Critical stability patches
- Performance optimizations

### Phase 2: Monitoring Dashboard (Weeks 5-8)
- Real-time charts implementation
- Performance metrics collection
- UI layout customization

### Phase 3: RGB Enhancements (Weeks 9-12)
- Advanced lighting effects
- Zone control system
- Effect library creation

### Phase 4: Integration Features (Weeks 13-16)
- MSI Afterburner integration
- Profile management
- Testing and validation

---

## Testing Strategy

### Unit Testing
- [ ] Hardware service mocking
- [ ] UI component testing
- [ ] Integration test coverage

### Hardware Testing
- [ ] Multiple HP Omen configurations
- [ ] Various GPU configurations
- [ ] RGB hardware compatibility

### User Acceptance Testing
- [ ] Beta testing program
- [ ] Community feedback integration
- [ ] Performance benchmarking

---

## Dependencies

- **Avalonia UI:** v11.0+ for enhanced charting
- **LiveCharts2:** For real-time monitoring graphs
- **NVAPI SDK:** Latest version for GPU integration
- **MSI Afterburner Shared Memory:** For hardware monitoring integration

---

## Risk Assessment

### High Risk
- RAM detection fix complexity across hardware variants
- RGB effect performance impact on system resources

### Medium Risk
- MSI Afterburner integration compatibility
- Chart rendering performance with high-frequency updates

### Low Risk
- UI layout customization
- Profile management features

---

## Success Criteria

- [ ] RAM display shows accurate values on all tested configurations
- [ ] Monitoring dashboard provides real-time system insights
- [ ] RGB lighting offers advanced customization options
- [ ] MSI Afterburner integration works seamlessly
- [ ] No critical bugs reported in beta testing
- [ ] Performance meets or exceeds v2.5.1 benchmarks