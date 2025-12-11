# OmenCore v1.0.0.6 Release Notes

**Release Date:** December 11, 2025  
**Build:** 1.0.0.6  
**Download:** [OmenCore-1.0.0.6-win-x64.zip](https://github.com/theantipopau/omencore/releases/tag/v1.0.0.6)  
**SHA256:** `54323D1F2F92086988A95EA7BD3D85CFDCC2F2F9348DA294443C7B6EB8AB6B23`

---

## 🎮 Major New Feature: Game Profile System

The headline feature of v1.0.0.6 is the complete **Game Profile System** - automatically switch settings when games launch!

### What's New

#### **Automatic Per-Game Settings**
- 🎯 **Profile-Based Auto-Switching**: Create profiles for each game with custom settings
- 🔄 **Game Detection**: Automatically detects when games launch and exit
- 💾 **Profile Persistence**: Profiles saved to `%APPDATA%\OmenCore\game_profiles.json`
- 📊 **Analytics**: Track launch count and total playtime per game

#### **Profile Manager UI**
- ✨ **Modern Dark Theme**: Clean, intuitive profile management interface
- 🔍 **Search & Filter**: Quickly find profiles in your library
- 📥 **Import/Export**: Share profile collections with other users
- 🗂️ **Profile Templates**: Duplicate existing profiles to save time

#### **Configurable Per-Game Settings**
Each profile can customize:
- **Fan Control**: Fan preset (Silent, Balanced, Performance, Extreme)
- **Performance Mode**: System performance profile
- **CPU Undervolt**: Core and cache voltage offsets
- **GPU Mode**: Graphics switching (Hybrid, Discrete, Integrated)
- **RGB Lighting**: Keyboard and peripheral lighting profiles
- **Priority**: Conflict resolution if multiple games match

#### **Smart Features**
- ⏱️ **2-Second Detection**: Fast game launch detection via WMI polling
- 🎯 **Priority System**: Higher priority profiles win if multiple match
- 🔄 **Auto-Restore**: Settings restore to defaults when game closes
- 📈 **Statistics**: View launch count and playtime per profile
- 🎮 **Game Library**: Detects by executable name or full path

---

## 📋 Implementation Details

### New Components (1,360+ lines of code)

#### **ProcessMonitoringService** (257 lines)
- Background process detection via WMI queries
- Tracks active game processes
- Events: `ProcessDetected`, `ProcessExited`
- Minimal CPU overhead (2-second polling interval)

#### **GameProfileService** (363 lines)
- Profile CRUD operations (Create, Read, Update, Delete)
- Auto-switching logic with priority resolution
- Playtime tracking and analytics
- Import/Export functionality
- JSON persistence

#### **GameProfile Model** (170 lines)
- Complete settings data structure
- Metadata: Priority, launch count, playtime, timestamps
- Methods: `Clone()`, `MatchesProcess()`
- Formatted playtime display

#### **Profile Manager UI** (335 lines XAML)
- Two-panel layout (list + editor)
- Search box with real-time filtering
- Settings organized by category
- Modern dark theme matching OmenCore style

#### **Profile Manager ViewModel** (235 lines)
- MVVM architecture with RelayCommands
- File picker integration (browse for executables)
- Import/Export dialogs
- Real-time property binding

---

## 🎨 UI Enhancements

### Main Window
- ➕ **"Game Profiles" Button**: New button in sidebar to open profile manager
- 📍 Located above "Export Config" for easy access

### Profile Manager Window
- **List Panel**: Profile library with search, create, duplicate, delete
- **Editor Panel**: Scrollable settings form with all profile options
- **Statistics Display**: Launch count, playtime, created/modified dates
- **Action Buttons**: Import/Export profile collections

---

## 🔧 How to Use

### Creating Your First Profile

1. **Open Profile Manager**: Click "Game Profiles" button in sidebar
2. **Create Profile**: Click "➕ New Profile"
3. **Name Your Profile**: e.g., "Apex Legends - Competitive"
4. **Set Executable**: e.g., "r5apex.exe" (or browse for full path)
5. **Configure Settings**:
   - Fan Preset: Extreme
   - Performance Mode: Performance
   - CPU Undervolt: -80mV core, -80mV cache
   - GPU Mode: Discrete
   - Keyboard Lighting: Game Mode
   - Peripheral Lighting: RGB Wave
6. **Set Priority**: Higher numbers win if multiple profiles match (default: 0)
7. **Enable**: Check "Enable Auto-Switch"
8. **Save**: Click "Save and Close"

### Profile Auto-Switching

Once configured:
- ✅ Launch game → Settings automatically applied
- ⏱️ Detection happens within 2 seconds
- 🎮 Game runs with optimized settings
- 🔄 Exit game → Settings restore to defaults
- 📊 Launch count increments, playtime tracked

### Import/Export Profiles

**Export**:
1. Click "📤 Export Profiles"
2. Choose location (saves as JSON)
3. Share file with friends

**Import**:
1. Click "📥 Import Profiles"
2. Select JSON file
3. Profiles added to your library (IDs regenerated to avoid conflicts)

---

## 🚀 Performance Impact

- **Memory**: ~5KB per profile (~500KB for 100 profiles)
- **CPU**: Minimal (WMI queries every 2 seconds when active)
- **Startup**: Profile system initializes asynchronously (no delay)
- **Apply Speed**: Settings applied in <500ms after game detection

---

## 🛠️ Technical Architecture

### Integration with Existing Systems

The game profile system seamlessly integrates with:
- **FanControlViewModel**: Applies fan presets
- **SystemControlViewModel**: Applies performance modes, undervolt, GPU switching
- **LightingViewModel**: Applies Corsair peripheral lighting
- **ConfigurationService**: Persists profiles to JSON

### Event-Driven Design

```
Game Launch → ProcessMonitoringService.ProcessDetected
           → GameProfileService.OnProfileApplyRequested
           → MainViewModel.ApplyGameProfileAsync()
           → Settings applied via existing ViewModels

Game Exit → ProcessMonitoringService.ProcessExited  
         → GameProfileService.OnProfileApplyRequested (restore)
         → MainViewModel.RestoreDefaultSettingsAsync()
         → Settings reverted to Balanced defaults
```

---

## 📦 Installation

### Portable ZIP (Recommended)
1. Download `OmenCore-1.0.0.6-win-x64.zip`
2. Extract to desired location
3. Run `OmenCore.exe` **as Administrator**
4. Game profiles saved to `%APPDATA%\OmenCore\game_profiles.json`

### Upgrade from v1.0.0.5
- Simply extract new version over old installation
- Existing config preserved
- Game profiles: New feature, start fresh

---

## 🐛 Known Issues

### Game Profile System
- ⚠️ **Detection Delay**: 2-second polling means slight delay after game launch
  - Future: Will implement WMI event subscription for instant detection
- ⚠️ **Window Title Matching**: Currently only matches by executable name
  - Multiple instances of same game not distinguished
  - Future: Add window title matching option
- ⚠️ **Pre-Launch Settings**: Settings applied after game starts, not before
  - Future: Investigate process start hooks

### General
- Admin privileges required for EC/WMI access (inherent Windows limitation)
- RGB.NET Corsair integration: DPI/macro/battery not supported (library limitation)
- Logitech support: Still stubbed (no public SDK available)

---

## 🔮 Coming Next

### v1.1.0 Roadmap
- **Per-Key RGB Editor**: Visual keyboard layout for OMEN laptops
- **Game Library Scanner**: Auto-detect installed games (Steam, Epic, GOG)
- **Process Start Hooks**: Apply settings before game launches
- **Window Title Matching**: Distinguish multiple instances
- **Profile Templates**: FPS, MOBA, RPG preset collections
- **Logitech G HUB Integration**: Basic RGB control via IPC

### v1.2.0 Roadmap
- **In-Game Overlay**: FPS, temps, fan speed display
- **AI Profile Suggestions**: Automatic profile optimization
- **Cloud Profile Sync**: Share profiles across devices
- **Community Profile Repository**: Download profiles from community

---

## 📊 Version Comparison

| Feature | v1.0.0.5 | v1.0.0.6 |
|---------|----------|----------|
| **Game Profiles** | ❌ | ✅ |
| **Auto-Switching** | ❌ | ✅ |
| **Profile Manager UI** | ❌ | ✅ |
| **Playtime Tracking** | ❌ | ✅ |
| **Import/Export Profiles** | ❌ | ✅ |
| Fan Control | ✅ | ✅ |
| Performance Modes | ✅ | ✅ |
| CPU Undervolt | ✅ | ✅ |
| GPU Switching | ✅ | ✅ |
| RGB Lighting (Corsair) | ✅ | ✅ |
| Auto-Update | ✅ | ✅ |
| System Optimization | ✅ | ✅ |

---

## 🙏 Acknowledgments

- **RGB.NET** - Corsair iCUE integration (lighting only)
- **LibreHardwareMonitor** - Hardware monitoring
- **Inno Setup** - Installer creation
- **Community Testers** - Feedback and bug reports

---

## 📝 Changelog

### Added
- ✨ Complete game profile system with auto-switching
- ✨ Profile Manager UI with modern dark theme
- ✨ Process monitoring service for game detection
- ✨ Profile import/export functionality
- ✨ Launch count and playtime tracking per profile
- ✨ Priority-based conflict resolution
- ✨ Search and filter in profile list
- ✨ "Game Profiles" button in main window sidebar
- ✨ JSON persistence to `%APPDATA%\OmenCore\game_profiles.json`

### Changed
- 🔄 MainViewModel now integrates with game profile service
- 🔄 Settings can be applied programmatically by game profiles
- 🔄 Auto-restore to defaults on game exit

### Technical
- 📦 1,360 lines of new code across 5 files
- 📦 GameProfile model (170 lines)
- 📦 ProcessMonitoringService (257 lines)
- 📦 GameProfileService (363 lines)
- 📦 GameProfileManagerView (335 lines)
- 📦 GameProfileManagerViewModel (235 lines)

---

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/theantipopau/omencore/issues)
- **Discussions**: [GitHub Discussions](https://github.com/theantipopau/omencore/discussions)
- **Reddit**: r/HPOmen
- **Website**: [omencore.info](https://omencore.info)

---

**Enjoy the new game profile system! 🎮**

Create profiles for all your favorite games and let OmenCore automatically optimize your system for the best gaming experience!
