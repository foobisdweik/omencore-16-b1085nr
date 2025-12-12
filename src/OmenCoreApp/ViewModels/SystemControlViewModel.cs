using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using OmenCore.Hardware;
using OmenCore.Models;
using OmenCore.Services;
using OmenCore.Utils;

namespace OmenCore.ViewModels
{
    public class SystemControlViewModel : ViewModelBase
    {
        private readonly UndervoltService _undervoltService;
        private readonly PerformanceModeService _performanceModeService;
        private readonly OmenGamingHubCleanupService _cleanupService;
        private readonly SystemRestoreService _restoreService;
        private readonly GpuSwitchService _gpuSwitchService;
        private readonly LoggingService _logging;
        private readonly HpWmiBios? _wmiBios;

        private PerformanceMode? _selectedPerformanceMode;
        private UndervoltStatus _undervoltStatus = UndervoltStatus.CreateUnknown();
        private double _requestedCoreOffset;
        private double _requestedCacheOffset;
        private bool _respectExternalUndervolt = true;
        private bool _cleanupInProgress;
        private string _cleanupStatus = "Status: Not checked";

        public ObservableCollection<PerformanceMode> PerformanceModes { get; } = new();
        public ObservableCollection<string> OmenCleanupSteps { get; } = new();

        public PerformanceMode? SelectedPerformanceMode
        {
            get => _selectedPerformanceMode;
            set
            {
                if (_selectedPerformanceMode != value)
                {
                    _selectedPerformanceMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public UndervoltStatus UndervoltStatus
        {
            get => _undervoltStatus;
            private set
            {
                _undervoltStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UndervoltStatusSummary));
                (ApplyUndervoltCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public double RequestedCoreOffset
        {
            get => _requestedCoreOffset;
            set
            {
                if (_requestedCoreOffset != value)
                {
                    _requestedCoreOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        public double RequestedCacheOffset
        {
            get => _requestedCacheOffset;
            set
            {
                if (_requestedCacheOffset != value)
                {
                    _requestedCacheOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RespectExternalUndervolt
        {
            get => _respectExternalUndervolt;
            set
            {
                if (_respectExternalUndervolt != value)
                {
                    _respectExternalUndervolt = value;
                    OnPropertyChanged();
                    (ApplyUndervoltCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string UndervoltStatusSummary => UndervoltStatus == null ? "n/a" : $"Core {UndervoltStatus.CurrentCoreOffsetMv:+0;-0;0} mV | Cache {UndervoltStatus.CurrentCacheOffsetMv:+0;-0;0} mV";
        
        public string UndervoltStatusText => UndervoltStatus?.HasExternalController == true 
            ? $"External controller detected: {UndervoltStatus?.ExternalController ?? "Unknown"}" 
            : (UndervoltStatus?.CurrentCoreOffsetMv != 0 || UndervoltStatus?.CurrentCacheOffsetMv != 0)
                ? $"Active: Core {UndervoltStatus?.CurrentCoreOffsetMv:+0;-0;0} mV, Cache {UndervoltStatus?.CurrentCacheOffsetMv:+0;-0;0} mV"
                : "No undervolt active";
        
        public System.Windows.Media.Brush UndervoltStatusColor => (UndervoltStatus?.CurrentCoreOffsetMv != 0 || UndervoltStatus?.CurrentCacheOffsetMv != 0)
            ? System.Windows.Media.Brushes.Lime
            : System.Windows.Media.Brushes.Gray;

        public string PerformanceModeDescription => SelectedPerformanceMode?.Description ?? SelectedPerformanceMode?.Name switch
        {
            "Balanced" => "Normal mode - balanced performance",
            "Performance" => "High performance - maximum GPU wattage",
            "Eco" => "Power saving mode - reduced wattage",
            _ => "Select a performance mode"
        };

        private string _currentGpuMode = "Detecting...";
        public string CurrentGpuMode
        {
            get => _currentGpuMode;
            private set
            {
                if (_currentGpuMode != value)
                {
                    _currentGpuMode = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // GPU Power Boost settings (TGP/PPAB control)
        private string _gpuPowerBoostLevel = "Medium";
        public string GpuPowerBoostLevel
        {
            get => _gpuPowerBoostLevel;
            set
            {
                if (_gpuPowerBoostLevel != value)
                {
                    _gpuPowerBoostLevel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GpuPowerBoostDescription));
                }
            }
        }
        
        private bool _gpuPowerBoostAvailable;
        public bool GpuPowerBoostAvailable
        {
            get => _gpuPowerBoostAvailable;
            private set
            {
                if (_gpuPowerBoostAvailable != value)
                {
                    _gpuPowerBoostAvailable = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private string _gpuPowerBoostStatus = "Detecting...";
        public string GpuPowerBoostStatus
        {
            get => _gpuPowerBoostStatus;
            private set
            {
                if (_gpuPowerBoostStatus != value)
                {
                    _gpuPowerBoostStatus = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string GpuPowerBoostDescription => GpuPowerBoostLevel switch
        {
            "Minimum" => "Base TGP only - Lower power, quieter operation, better battery life",
            "Medium" => "Custom TGP enabled - Balanced performance and thermals",
            "Maximum" => "Custom TGP + Dynamic Boost (PPAB) - Maximum GPU wattage (+15W boost)",
            _ => "Select GPU power level"
        };
        
        public ObservableCollection<string> GpuPowerBoostLevels { get; } = new() { "Minimum", "Medium", "Maximum" };
        
        public ObservableCollection<GpuSwitchMode> GpuSwitchModes { get; } = new();
        public GpuSwitchMode? SelectedGpuMode { get; set; }
        public bool CleanupUninstallApp { get; set; } = true;
        public bool CleanupRemoveServices { get; set; } = true;
        public bool CleanupRegistryEntries { get; set; } = false;
        public bool CleanupRemoveLegacyInstallers { get; set; } = true;
        public bool CleanupRemoveFiles { get; set; } = true;
        public bool CleanupKillProcesses { get; set; } = true;
        public string CleanupStatusText => CleanupStatus;
        public ICommand SwitchGpuModeCommand { get; }
        public ICommand CreateRestorePointCommand { get; }
        public ICommand RunCleanupCommand { get; }
        public ICommand ApplyUndervoltPresetCommand { get; }
        public ICommand ApplyGpuPowerBoostCommand { get; }

        public bool CleanupInProgress
        {
            get => _cleanupInProgress;
            private set
            {
                if (_cleanupInProgress != value)
                {
                    _cleanupInProgress = value;
                    OnPropertyChanged();
                    (CleanupOmenHubCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string CleanupStatus
        {
            get => _cleanupStatus;
            private set
            {
                if (_cleanupStatus != value)
                {
                    _cleanupStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ApplyPerformanceModeCommand { get; }
        public ICommand ApplyUndervoltCommand { get; }
        public ICommand ResetUndervoltCommand { get; }
        public ICommand CleanupOmenHubCommand { get; }

        public SystemControlViewModel(
            UndervoltService undervoltService, 
            PerformanceModeService performanceModeService, 
            OmenGamingHubCleanupService cleanupService,
            SystemRestoreService restoreService,
            GpuSwitchService gpuSwitchService,
            LoggingService logging,
            HpWmiBios? wmiBios = null)
        {
            _undervoltService = undervoltService;
            _performanceModeService = performanceModeService;
            _cleanupService = cleanupService;
            _restoreService = restoreService;
            _gpuSwitchService = gpuSwitchService;
            _logging = logging;
            _wmiBios = wmiBios;

            _undervoltService.StatusChanged += (s, status) => 
            {
                UndervoltStatus = status;
                OnPropertyChanged(nameof(UndervoltStatusText));
                OnPropertyChanged(nameof(UndervoltStatusColor));
            };

            ApplyPerformanceModeCommand = new RelayCommand(_ => ApplyPerformanceMode(), _ => SelectedPerformanceMode != null);
            ApplyUndervoltCommand = new AsyncRelayCommand(_ => ApplyUndervoltAsync(), _ => !RespectExternalUndervolt || !UndervoltStatus.HasExternalController);
            ResetUndervoltCommand = new AsyncRelayCommand(async _ => await _undervoltService.ResetAsync());
            ApplyUndervoltPresetCommand = new AsyncRelayCommand(ApplyUndervoltPresetAsync);
            CleanupOmenHubCommand = new AsyncRelayCommand(_ => RunCleanupAsync(), _ => !CleanupInProgress);
            RunCleanupCommand = new AsyncRelayCommand(_ => RunCleanupAsync(), _ => !CleanupInProgress);
            CreateRestorePointCommand = new AsyncRelayCommand(_ => CreateRestorePointAsync());
            SwitchGpuModeCommand = new AsyncRelayCommand(_ => SwitchGpuModeAsync());
            ApplyGpuPowerBoostCommand = new RelayCommand(_ => ApplyGpuPowerBoost(), _ => GpuPowerBoostAvailable);

            // Initialize performance modes
            PerformanceModes.Add(new PerformanceMode { Name = "Balanced" });
            PerformanceModes.Add(new PerformanceMode { Name = "Performance" });
            PerformanceModes.Add(new PerformanceMode { Name = "Quiet" });
            SelectedPerformanceMode = PerformanceModes.FirstOrDefault();

            // Initialize GPU modes
            GpuSwitchModes.Add(GpuSwitchMode.Hybrid);
            GpuSwitchModes.Add(GpuSwitchMode.Discrete);
            GpuSwitchModes.Add(GpuSwitchMode.Integrated);
            
            // Detect current GPU mode
            DetectGpuMode();
            
            // Detect GPU Power Boost availability
            DetectGpuPowerBoost();
            
            // Initial undervolt status will be set via StatusChanged event
        }
        
        private void DetectGpuPowerBoost()
        {
            if (_wmiBios != null && _wmiBios.IsAvailable)
            {
                GpuPowerBoostAvailable = true;
                var gpuPower = _wmiBios.GetGpuPower();
                if (gpuPower.HasValue)
                {
                    if (gpuPower.Value.customTgp && gpuPower.Value.ppab)
                    {
                        GpuPowerBoostLevel = "Maximum";
                        GpuPowerBoostStatus = "Maximum (Custom TGP + Dynamic Boost)";
                    }
                    else if (gpuPower.Value.customTgp)
                    {
                        GpuPowerBoostLevel = "Medium";
                        GpuPowerBoostStatus = "Medium (Custom TGP)";
                    }
                    else
                    {
                        GpuPowerBoostLevel = "Minimum";
                        GpuPowerBoostStatus = "Minimum (Base TGP)";
                    }
                }
                else
                {
                    GpuPowerBoostStatus = "Could not read current setting";
                }
                _logging.Info($"✓ GPU Power Boost available via WMI BIOS. Current: {GpuPowerBoostStatus}");
            }
            else
            {
                GpuPowerBoostAvailable = false;
                GpuPowerBoostStatus = "Not available (WMI BIOS interface not detected)";
                _logging.Info("GPU Power Boost: HP WMI BIOS interface not available");
            }
        }
        
        private void ApplyGpuPowerBoost()
        {
            if (_wmiBios == null || !_wmiBios.IsAvailable)
            {
                _logging.Warn("Cannot apply GPU Power Boost: WMI BIOS not available");
                return;
            }

            var level = GpuPowerBoostLevel switch
            {
                "Minimum" => HpWmiBios.GpuPowerLevel.Minimum,
                "Medium" => HpWmiBios.GpuPowerLevel.Medium,
                "Maximum" => HpWmiBios.GpuPowerLevel.Maximum,
                _ => HpWmiBios.GpuPowerLevel.Medium
            };

            if (_wmiBios.SetGpuPower(level))
            {
                GpuPowerBoostStatus = GpuPowerBoostLevel switch
                {
                    "Minimum" => "✓ Minimum (Base TGP only)",
                    "Medium" => "✓ Medium (Custom TGP enabled)",
                    "Maximum" => "✓ Maximum (Custom TGP + Dynamic Boost +15W)",
                    _ => "Applied"
                };
                _logging.Info($"✓ GPU Power Boost set to: {GpuPowerBoostLevel}");
            }
            else
            {
                GpuPowerBoostStatus = "Failed to apply setting";
                _logging.Warn($"Failed to set GPU Power Boost to: {GpuPowerBoostLevel}");
            }
        }
        
        private void DetectGpuMode()
        {
            try
            {
                var mode = _gpuSwitchService.DetectCurrentMode();
                CurrentGpuMode = mode switch
                {
                    GpuSwitchMode.Hybrid => "Hybrid (MSHybrid/Optimus)",
                    GpuSwitchMode.Discrete => "Discrete GPU Only",
                    GpuSwitchMode.Integrated => "Integrated GPU Only",
                    _ => "Unknown"
                };
                _logging.Info($"Detected GPU mode: {CurrentGpuMode}");
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to detect GPU mode", ex);
                CurrentGpuMode = "Detection Failed";
            }
        }

        private void ApplyPerformanceMode()
        {
            if (SelectedPerformanceMode != null)
            {
                _performanceModeService.Apply(SelectedPerformanceMode);
                _logging.Info($"Performance mode applied: {SelectedPerformanceMode.Name}");
                OnPropertyChanged(nameof(CurrentPerformanceModeName));
                OnPropertyChanged(nameof(SelectedPerformanceMode));
            }
        }
        
        public string CurrentPerformanceModeName => SelectedPerformanceMode?.Name ?? "Auto";

        private async Task ApplyUndervoltAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var offset = new UndervoltOffset
                {
                    CoreMv = RequestedCoreOffset,
                    CacheMv = RequestedCacheOffset
                };
                await _undervoltService.ApplyAsync(offset);
            }, "Applying undervolt settings...");
        }

        private async Task ApplyUndervoltPresetAsync(object? parameter)
        {
            if (parameter is string offsetStr && double.TryParse(offsetStr, out double offset))
            {
                RequestedCoreOffset = offset;
                RequestedCacheOffset = offset;
                await ApplyUndervoltAsync();
            }
        }

        private async Task CreateRestorePointAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                _logging.Info("Creating system restore point...");
                var result = await _restoreService.CreateRestorePointAsync("OmenCore - Before System Changes");
                
                if (result.Success)
                {
                    _logging.Info($"✓ System restore point created successfully (Sequence: {result.SequenceNumber})");
                }
                else
                {
                    _logging.Error($"✗ Failed to create restore point: {result.Message}");
                }
            }, "Creating system restore point...");
        }

        private async Task SwitchGpuModeAsync()
        {
            if (SelectedGpuMode == null)
            {
                _logging.Warn("No GPU mode selected");
                return;
            }

            await ExecuteWithLoadingAsync(async () =>
            {
                var targetMode = SelectedGpuMode.Value;
                _logging.Info($"⚡ Attempting to switch GPU mode to: {targetMode}");
                
                var success = _gpuSwitchService.Switch(targetMode);
                
                if (success)
                {
                    _logging.Info($"✓ GPU mode switch initiated. System restart required to apply changes.");
                    
                    // Show restart prompt
                    var result = System.Windows.MessageBox.Show(
                        $"GPU mode has been set to {targetMode}.\n\n" +
                        "A system restart is required for changes to take effect.\n\n" +
                        "Would you like to restart now?",
                        "Restart Required - OmenCore",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        _logging.Info("User accepted restart - initiating system restart");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "shutdown",
                            Arguments = "/r /t 5 /c \"Restarting to apply GPU mode change\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                }
                else
                {
                    _logging.Warn($"⚠️ GPU mode switching not supported on this system or failed. Current mode: {CurrentGpuMode}");
                    System.Windows.MessageBox.Show(
                        "GPU mode switching is not supported on this system.\n\n" +
                        "This feature requires:\n" +
                        "• HP Omen Command Center BIOS support\n" +
                        "• NVIDIA Advanced Optimus or AMD Switchable Graphics\n" +
                        "• Compatible laptop model with MUX switch\n\n" +
                        $"Current detected mode: {CurrentGpuMode}",
                        "Not Supported - OmenCore",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                
                await Task.CompletedTask;
            }, "Switching GPU mode...");
        }

        private async Task RunCleanupAsync()
        {
            CleanupInProgress = true;
            CleanupStatus = "Running cleanup...";
            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var options = new OmenCleanupOptions
                    {
                        RemoveStorePackage = CleanupUninstallApp,
                        RemoveServicesAndTasks = CleanupRemoveServices,
                        RemoveRegistryTraces = CleanupRegistryEntries,
                        RemoveLegacyInstallers = CleanupRemoveLegacyInstallers,
                        RemoveResidualFiles = CleanupRemoveFiles,
                        KillRunningProcesses = CleanupKillProcesses,
                        DryRun = false,
                        PreserveFirewallRules = true
                    };
                    var result = await _cleanupService.CleanupAsync(options);
                    CleanupStatus = result.Success ? "Cleanup complete" : "Cleanup failed";
                }, "Running HP Omen cleanup...");
            }
            finally
            {
                CleanupInProgress = false;
            }
        }
    }
}
