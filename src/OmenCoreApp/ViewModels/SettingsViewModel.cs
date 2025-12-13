using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using OmenCore.Models;
using OmenCore.Services;
using OmenCore.Utils;

namespace OmenCore.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly LoggingService _logging;
        private readonly ConfigurationService _configService;
        private readonly AppConfig _config;
        private readonly SystemInfoService _systemInfoService;
        private readonly FanCleaningService _fanCleaningService;
        private readonly BiosUpdateService _biosUpdateService;
        
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _minimizeToTrayOnClose = true;
        private int _pollingIntervalMs = 1500;
        private int _historyCount = 120;
        private bool _lowOverheadMode;
        private bool _autoCheckUpdates = true;
        private int _updateCheckIntervalIndex = 2; // Daily
        private bool _includePreReleases;
        private bool _hotkeysEnabled = true;
        private bool _notificationsEnabled = true;
        private bool _gameNotificationsEnabled = true;
        private bool _modeChangeNotificationsEnabled = true;
        private bool _temperatureWarningsEnabled = true;
        private string _fanCleaningStatusText = "Checking hardware...";
        private string _fanCleaningStatusIcon = "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z";
        private Brush _fanCleaningStatusColor = Brushes.Gray;
        private bool _canStartFanCleaning;
        private bool _isFanCleaningActive;
        private string _fanCleaningProgress = "";
        private int _fanCleaningProgressPercent;
        private CancellationTokenSource? _fanCleaningCts;
        
        // Driver status fields
        private string _driverStatusText = "Checking...";
        private string _driverStatusDetail = "";
        private Brush _driverStatusColor = Brushes.Gray;
        
        // BIOS update fields
        private string _systemModel = "";
        private string _currentBiosVersion = "";
        private string _latestBiosVersion = "";
        private bool _biosUpdateAvailable;
        private string _biosDownloadUrl = "";
        private string _biosCheckStatus = "Not checked";
        private bool _isBiosCheckInProgress;
        private Brush _biosStatusColor = Brushes.Gray;

        public SettingsViewModel(LoggingService logging, ConfigurationService configService, 
            SystemInfoService systemInfoService, FanCleaningService fanCleaningService,
            BiosUpdateService biosUpdateService)
        {
            _logging = logging;
            _configService = configService;
            _config = configService.Config;
            _systemInfoService = systemInfoService;
            _fanCleaningService = fanCleaningService;
            _biosUpdateService = biosUpdateService;

            // Load saved settings
            LoadSettings();

            // Initialize commands
            OpenConfigFolderCommand = new RelayCommand(_ => OpenConfigFolder());
            OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
            OpenGitHubCommand = new RelayCommand(_ => OpenUrl("https://github.com/theantipopau/omencore"));
            OpenReleaseNotesCommand = new RelayCommand(_ => OpenUrl("https://github.com/theantipopau/omencore/releases"));
            OpenIssuesCommand = new RelayCommand(_ => OpenUrl("https://github.com/theantipopau/omencore/issues"));
            StartFanCleaningCommand = new AsyncRelayCommand(async _ => await StartFanCleaningAsync(), _ => CanStartFanCleaning && !IsFanCleaningActive);
            ResetToDefaultsCommand = new RelayCommand(_ => ResetToDefaults());
            InstallDriverCommand = new RelayCommand(_ => InstallDriver());
            RefreshDriverStatusCommand = new RelayCommand(_ => CheckDriverStatus());
            CheckBiosUpdatesCommand = new AsyncRelayCommand(async _ => await CheckBiosUpdatesAsync(), _ => !IsBiosCheckInProgress);
            DownloadBiosUpdateCommand = new RelayCommand(_ => DownloadBiosUpdate(), _ => BiosUpdateAvailable && !string.IsNullOrEmpty(BiosDownloadUrl));

            // Check fan cleaning availability
            CheckFanCleaningAvailability();
            
            // Check driver status
            CheckDriverStatus();
            
            // Load system info for BIOS
            LoadSystemInfo();
        }

        #region General Settings

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows != value)
                {
                    _startWithWindows = value;
                    OnPropertyChanged();
                    SetStartWithWindows(value);
                }
            }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized != value)
                {
                    _startMinimized = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool MinimizeToTrayOnClose
        {
            get => _minimizeToTrayOnClose;
            set
            {
                if (_minimizeToTrayOnClose != value)
                {
                    _minimizeToTrayOnClose = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        #endregion

        #region Monitoring Settings

        public int PollingIntervalMs
        {
            get => _pollingIntervalMs;
            set
            {
                if (_pollingIntervalMs != value)
                {
                    _pollingIntervalMs = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public int HistoryCount
        {
            get => _historyCount;
            set
            {
                if (_historyCount != value)
                {
                    _historyCount = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool LowOverheadMode
        {
            get => _lowOverheadMode;
            set
            {
                if (_lowOverheadMode != value)
                {
                    _lowOverheadMode = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        #endregion

        #region Update Settings

        public bool AutoCheckUpdates
        {
            get => _autoCheckUpdates;
            set
            {
                if (_autoCheckUpdates != value)
                {
                    _autoCheckUpdates = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public int UpdateCheckIntervalIndex
        {
            get => _updateCheckIntervalIndex;
            set
            {
                if (_updateCheckIntervalIndex != value)
                {
                    _updateCheckIntervalIndex = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool IncludePreReleases
        {
            get => _includePreReleases;
            set
            {
                if (_includePreReleases != value)
                {
                    _includePreReleases = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        #endregion

        #region Hotkeys Settings

        public bool HotkeysEnabled
        {
            get => _hotkeysEnabled;
            set
            {
                if (_hotkeysEnabled != value)
                {
                    _hotkeysEnabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        #endregion

        #region Notifications Settings

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set
            {
                if (_notificationsEnabled != value)
                {
                    _notificationsEnabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool GameNotificationsEnabled
        {
            get => _gameNotificationsEnabled;
            set
            {
                if (_gameNotificationsEnabled != value)
                {
                    _gameNotificationsEnabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool ModeChangeNotificationsEnabled
        {
            get => _modeChangeNotificationsEnabled;
            set
            {
                if (_modeChangeNotificationsEnabled != value)
                {
                    _modeChangeNotificationsEnabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool TemperatureWarningsEnabled
        {
            get => _temperatureWarningsEnabled;
            set
            {
                if (_temperatureWarningsEnabled != value)
                {
                    _temperatureWarningsEnabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        #endregion

        #region Fan Cleaning

        public string FanCleaningStatusText
        {
            get => _fanCleaningStatusText;
            set { _fanCleaningStatusText = value; OnPropertyChanged(); }
        }

        public string FanCleaningStatusIcon
        {
            get => _fanCleaningStatusIcon;
            set { _fanCleaningStatusIcon = value; OnPropertyChanged(); }
        }

        public Brush FanCleaningStatusColor
        {
            get => _fanCleaningStatusColor;
            set { _fanCleaningStatusColor = value; OnPropertyChanged(); }
        }

        public bool CanStartFanCleaning
        {
            get => _canStartFanCleaning;
            set 
            { 
                _canStartFanCleaning = value; 
                OnPropertyChanged();
                (StartFanCleaningCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsFanCleaningActive
        {
            get => _isFanCleaningActive;
            set 
            { 
                _isFanCleaningActive = value; 
                OnPropertyChanged();
                (StartFanCleaningCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string FanCleaningProgress
        {
            get => _fanCleaningProgress;
            set { _fanCleaningProgress = value; OnPropertyChanged(); }
        }

        public int FanCleaningProgressPercent
        {
            get => _fanCleaningProgressPercent;
            set { _fanCleaningProgressPercent = value; OnPropertyChanged(); }
        }

        #endregion

        #region Data & About

        public string ConfigFolderPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OmenCore");

        public string LogFolderPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmenCore");

        public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly()
            .GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>()?.Version ?? "1.0.0.0";

        #endregion

        #region Commands

        public ICommand OpenConfigFolderCommand { get; }
        public ICommand OpenLogFolderCommand { get; }
        public ICommand OpenGitHubCommand { get; }
        public ICommand OpenReleaseNotesCommand { get; }
        public ICommand OpenIssuesCommand { get; }
        public ICommand StartFanCleaningCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }
        public ICommand InstallDriverCommand { get; }
        public ICommand RefreshDriverStatusCommand { get; }
        public ICommand CheckBiosUpdatesCommand { get; }
        public ICommand DownloadBiosUpdateCommand { get; }

        #endregion
        
        #region Driver Status Properties
        
        public string DriverStatusText
        {
            get => _driverStatusText;
            set { _driverStatusText = value; OnPropertyChanged(); }
        }
        
        public string DriverStatusDetail
        {
            get => _driverStatusDetail;
            set { _driverStatusDetail = value; OnPropertyChanged(); }
        }
        
        public Brush DriverStatusColor
        {
            get => _driverStatusColor;
            set { _driverStatusColor = value; OnPropertyChanged(); }
        }
        
        #endregion
        
        #region BIOS Update Properties
        
        public string SystemModel
        {
            get => _systemModel;
            set { _systemModel = value; OnPropertyChanged(); }
        }
        
        public string CurrentBiosVersion
        {
            get => _currentBiosVersion;
            set { _currentBiosVersion = value; OnPropertyChanged(); }
        }
        
        public string LatestBiosVersion
        {
            get => _latestBiosVersion;
            set { _latestBiosVersion = value; OnPropertyChanged(); }
        }
        
        public bool BiosUpdateAvailable
        {
            get => _biosUpdateAvailable;
            set 
            { 
                _biosUpdateAvailable = value; 
                OnPropertyChanged();
                (DownloadBiosUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        
        public string BiosDownloadUrl
        {
            get => _biosDownloadUrl;
            set 
            { 
                _biosDownloadUrl = value; 
                OnPropertyChanged();
                (DownloadBiosUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        
        public string BiosCheckStatus
        {
            get => _biosCheckStatus;
            set { _biosCheckStatus = value; OnPropertyChanged(); }
        }
        
        public bool IsBiosCheckInProgress
        {
            get => _isBiosCheckInProgress;
            set 
            { 
                _isBiosCheckInProgress = value; 
                OnPropertyChanged();
                (CheckBiosUpdatesCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        
        public Brush BiosStatusColor
        {
            get => _biosStatusColor;
            set { _biosStatusColor = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            // Load from config
            _pollingIntervalMs = _config.Monitoring.PollIntervalMs;
            _historyCount = _config.Monitoring.HistoryCount;
            _lowOverheadMode = _config.Monitoring.LowOverheadMode;
            _autoCheckUpdates = _config.Updates?.AutoCheckEnabled ?? true;
            // Note: IncludePreReleases not yet in UpdatePreferences, using default false
            
            // Load hotkey and notification settings
            _hotkeysEnabled = _config.Monitoring.HotkeysEnabled;
            _notificationsEnabled = _config.Monitoring.NotificationsEnabled;
            _gameNotificationsEnabled = _config.Monitoring.GameNotificationsEnabled;
            _modeChangeNotificationsEnabled = _config.Monitoring.ModeChangeNotificationsEnabled;
            _temperatureWarningsEnabled = _config.Monitoring.TemperatureWarningsEnabled;
            
            // Load UI preferences
            _startMinimized = _config.Monitoring.StartMinimized;
            _minimizeToTrayOnClose = _config.Monitoring.MinimizeToTrayOnClose;

            // Check startup registry
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                _startWithWindows = key?.GetValue("OmenCore") != null;
            }
            catch { }
            
            _logging.Info($"Settings loaded: Hotkeys={_hotkeysEnabled}, Notifications={_notificationsEnabled}");
        }

        private void SaveSettings()
        {
            _config.Monitoring.PollIntervalMs = _pollingIntervalMs;
            _config.Monitoring.HistoryCount = _historyCount;
            _config.Monitoring.LowOverheadMode = _lowOverheadMode;
            
            // Save hotkey and notification settings
            _config.Monitoring.HotkeysEnabled = _hotkeysEnabled;
            _config.Monitoring.NotificationsEnabled = _notificationsEnabled;
            _config.Monitoring.GameNotificationsEnabled = _gameNotificationsEnabled;
            _config.Monitoring.ModeChangeNotificationsEnabled = _modeChangeNotificationsEnabled;
            _config.Monitoring.TemperatureWarningsEnabled = _temperatureWarningsEnabled;
            
            // Save UI preferences
            _config.Monitoring.StartMinimized = _startMinimized;
            _config.Monitoring.MinimizeToTrayOnClose = _minimizeToTrayOnClose;
            
            if (_config.Updates == null)
                _config.Updates = new UpdatePreferences();
            _config.Updates.AutoCheckEnabled = _autoCheckUpdates;
            // Note: IncludePreReleases not yet in UpdatePreferences

            _configService.Save(_config);
        }

        private void SetStartWithWindows(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("OmenCore", $"\"{exePath}\"");
                        _logging.Info("Added OmenCore to Windows startup");
                    }
                }
                else
                {
                    key.DeleteValue("OmenCore", false);
                    _logging.Info("Removed OmenCore from Windows startup");
                }
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to modify startup registry", ex);
            }
        }

        private void OpenConfigFolder()
        {
            try
            {
                var path = ConfigFolderPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to open config folder", ex);
            }
        }

        private void OpenLogFolder()
        {
            try
            {
                var path = LogFolderPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to open log folder", ex);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logging.Error($"Failed to open URL: {url}", ex);
            }
        }

        private void CheckFanCleaningAvailability()
        {
            var systemInfo = _systemInfoService.GetSystemInfo();
            
            if (systemInfo.IsHpGaming)
            {
                var canClean = _fanCleaningService.IsSupported;
                if (canClean)
                {
                    FanCleaningStatusText = $"✓ HP OMEN detected: {systemInfo.Model}. Fan cleaning available.";
                    FanCleaningStatusIcon = "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z"; // Checkmark
                    FanCleaningStatusColor = new SolidColorBrush(Color.FromRgb(129, 199, 132)); // Green
                    CanStartFanCleaning = true;
                }
                else
                {
                    var reason = _fanCleaningService.UnsupportedReason;
                    FanCleaningStatusText = $"⚠️ {reason}";
                    FanCleaningStatusIcon = "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z"; // Warning
                    FanCleaningStatusColor = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Orange
                    CanStartFanCleaning = false;
                    _logging.Info($"Fan cleaning unavailable: {reason}");
                }
            }
            else
            {
                FanCleaningStatusText = $"✗ Non-HP OMEN system detected: {systemInfo.Manufacturer} {systemInfo.Model}. Fan cleaning unavailable.";
                FanCleaningStatusIcon = "M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z"; // X
                FanCleaningStatusColor = new SolidColorBrush(Color.FromRgb(229, 115, 115)); // Red
                CanStartFanCleaning = false;
            }
        }

        private async Task StartFanCleaningAsync()
        {
            var result = MessageBox.Show(
                "This will run the fan cleaning cycle for 20 seconds.\n\n" +
                "⚠️ IMPORTANT:\n" +
                "• Place your laptop on a flat, stable surface\n" +
                "• Ensure vents are not blocked\n" +
                "• The fans will briefly reverse direction\n" +
                "• You may hear unusual fan sounds - this is normal\n\n" +
                "Continue with fan cleaning?",
                "Fan Dust Cleaning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsFanCleaningActive = true;
            _fanCleaningCts = new CancellationTokenSource();

            try
            {
                _logging.Info("Starting fan cleaning cycle");
                
                await _fanCleaningService.StartCleaningAsync(
                    progress =>
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(() =>
                        {
                            FanCleaningProgress = progress.Message;
                            FanCleaningProgressPercent = progress.ProgressPercent;
                        });
                    },
                    _fanCleaningCts.Token);

                _logging.Info("Fan cleaning cycle completed successfully");
                MessageBox.Show("Fan cleaning cycle completed successfully!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                _logging.Info("Fan cleaning was cancelled");
            }
            catch (Exception ex)
            {
                _logging.Error("Fan cleaning failed", ex);
                MessageBox.Show($"Fan cleaning failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsFanCleaningActive = false;
                FanCleaningProgress = "";
                FanCleaningProgressPercent = 0;
                _fanCleaningCts?.Dispose();
                _fanCleaningCts = null;
            }
        }

        private void ResetToDefaults()
        {
            var result = MessageBox.Show(
                "This will reset all settings to their default values.\n\n" +
                "This includes:\n" +
                "• Monitoring settings\n" +
                "• Notification preferences\n" +
                "• Hotkey settings\n" +
                "• Update preferences\n\n" +
                "Continue with reset?",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Reset all settings to defaults
                PollingIntervalMs = 1500;
                HistoryCount = 120;
                LowOverheadMode = false;
                StartMinimized = false;
                MinimizeToTrayOnClose = true;
                AutoCheckUpdates = true;
                UpdateCheckIntervalIndex = 2; // Daily
                IncludePreReleases = false;
                HotkeysEnabled = true;
                NotificationsEnabled = true;
                GameNotificationsEnabled = true;
                ModeChangeNotificationsEnabled = true;
                TemperatureWarningsEnabled = true;

                SaveSettings();
                
                _logging.Info("Settings reset to defaults");
                MessageBox.Show("All settings have been reset to their default values.", 
                    "Settings Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to reset settings", ex);
                MessageBox.Show($"Failed to reset settings: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CheckDriverStatus()
        {
            try
            {
                var secureBootEnabled = IsSecureBootEnabled();
                var memoryIntegrityEnabled = IsMemoryIntegrityEnabled();

                var pawnIoAvailable = IsPawnIOAvailable();

                // Check WinRing0 driver - try multiple device paths
                var devicePaths = new[] { @"\\.\WinRing0_1_2_0", @"\\.\WinRing0_1_2", @"\\.\WinRing0" };
                bool winRing0Available = false;
                
                foreach (var devicePath in devicePaths)
                {
                    try
                    {
                        var handle = NativeMethods.CreateFile(
                            devicePath,
                            NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                            NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                            IntPtr.Zero,
                            3, // OPEN_EXISTING
                            0,
                            IntPtr.Zero);

                        if (!handle.IsInvalid && !handle.IsClosed)
                        {
                            winRing0Available = true;
                            handle.Close();
                            break;
                        }
                    }
                    catch { }
                }
                
                // Check for XTU service conflict
                bool xtuRunning = false;
                string? xtuServiceName = null;
                try
                {
                    var xtuServices = new[] { "XTU3SERVICE", "XtuService", "IntelXtuService" };
                    foreach (var svc in xtuServices)
                    {
                        var processes = Process.GetProcessesByName(svc);
                        if (processes.Any())
                        {
                            xtuRunning = true;
                            xtuServiceName = svc;
                            foreach (var p in processes) p.Dispose();
                            break;
                        }
                    }
                }
                catch { }

                if (pawnIoAvailable)
                {
                    DriverStatusText = "PawnIO Installed";
                    DriverStatusDetail = "Secure Boot compatible driver backend available (recommended)";
                    DriverStatusColor = new SolidColorBrush(Color.FromRgb(102, 187, 106)); // Green
                }
                else if (winRing0Available)
                {
                    if (xtuRunning)
                    {
                        DriverStatusText = "WinRing0 Installed (XTU Conflict)";
                        DriverStatusDetail = $"Intel XTU service ({xtuServiceName}) may block undervolting. Stop XTU to use OmenCore undervolting.";
                        DriverStatusColor = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Orange
                    }
                    else
                    {
                        DriverStatusText = "WinRing0 Installed (Legacy)";
                        DriverStatusDetail = "Legacy driver backend detected. On Secure Boot/HVCI systems, PawnIO is recommended.";
                        DriverStatusColor = new SolidColorBrush(Color.FromRgb(102, 187, 106)); // Green
                    }
                }
                else
                {
                    DriverStatusText = "No Driver Backend Detected";

                    if (secureBootEnabled || memoryIntegrityEnabled)
                    {
                        var reasons = new List<string>();
                        if (secureBootEnabled) reasons.Add("Secure Boot is enabled");
                        if (memoryIntegrityEnabled) reasons.Add("Memory Integrity (HVCI) is enabled");

                        DriverStatusDetail =
                            $"WinRing0 may be blocked ({string.Join(", ", reasons)}). " +
                            "Install PawnIO (pawnio.eu) for Secure Boot compatible EC access.";
                    }
                    else
                    {
                        DriverStatusDetail =
                            "Install PawnIO (recommended) or LibreHardwareMonitor (WinRing0) to enable driver-dependent features.";
                    }

                    DriverStatusColor = new SolidColorBrush(Color.FromRgb(239, 83, 80)); // Red
                }
            }
            catch (Exception ex)
            {
                DriverStatusText = "Check Failed";
                DriverStatusDetail = ex.Message;
                DriverStatusColor = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Orange
            }
        }

        private static bool IsPawnIOAvailable()
        {
            try
            {
                // Registry check
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
                if (key != null)
                    return true;

                // Default install path
                var defaultPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "PawnIO", "PawnIOLib.dll");
                if (System.IO.File.Exists(defaultPath))
                    return true;

                // Driver loaded check
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_SystemDriver WHERE Name LIKE '%PawnIO%'");
                return searcher.Get().Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSecureBootEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                var value = key?.GetValue("UEFISecureBootEnabled");
                return value != null && Convert.ToInt32(value) == 1;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMemoryIntegrityEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity");
                var value = key?.GetValue("Enabled");
                return value != null && Convert.ToInt32(value) == 1;
            }
            catch
            {
                return false;
            }
        }
        
        private void InstallDriver()
        {
            // Show info about what will be installed
            var result = MessageBox.Show(
                "OmenCore requires LibreHardwareMonitor for hardware monitoring.\n\n" +
                "LibreHardwareMonitor includes the WinRing0 driver needed for:\n" +
                "• CPU temperature monitoring\n" +
                "• CPU undervolting (MSR access)\n" +
                "• TCC offset control\n\n" +
                "Click OK to download and install LibreHardwareMonitor.",
                "Install Hardware Monitor", 
                MessageBoxButton.OKCancel, 
                MessageBoxImage.Information);
                
            if (result != MessageBoxResult.OK)
                return;
            
            // Delegate to App's download method
            if (Application.Current is App app)
            {
                // Call the download method via reflection or make it public/static
                var method = typeof(App).GetMethod("DownloadAndInstallLibreHardwareMonitor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(app, null);
                
                // Refresh status after install attempt
                Task.Delay(3000).ContinueWith(_ => 
                    Application.Current.Dispatcher.Invoke(CheckDriverStatus));
            }
            else
            {
                    // Prefer PawnIO on Secure Boot / Memory Integrity systems.
                    if (IsSecureBootEnabled() || IsMemoryIntegrityEnabled())
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://pawnio.eu/",
                            UseShellExecute = true
                        });
                        return;
                    }

                    // Legacy / optional: LibreHardwareMonitor (WinRing0-based) backend.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/LibreHardwareMonitor/LibreHardwareMonitor",
                        UseShellExecute = true
                    });
            }
        }
        
        private void LoadSystemInfo()
        {
            try
            {
                var sysInfo = _systemInfoService.GetSystemInfo();
                // Use Model for display (full name like "HP OMEN by HP Laptop 17-ck2xxx")
                SystemModel = !string.IsNullOrEmpty(sysInfo.Model) ? sysInfo.Model : sysInfo.ProductName ?? "Unknown HP Product";
                CurrentBiosVersion = sysInfo.BiosVersion ?? "Unknown";
                BiosCheckStatus = "Click 'Check for Updates' to check HP for BIOS updates";
                BiosStatusColor = Brushes.Gray;
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to load system info for BIOS display", ex);
                SystemModel = "Unable to detect";
                CurrentBiosVersion = "Unable to detect";
                BiosCheckStatus = "Could not read system information";
                BiosStatusColor = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Orange
            }
        }
        
        private async Task CheckBiosUpdatesAsync()
        {
            if (IsBiosCheckInProgress)
                return;
                
            IsBiosCheckInProgress = true;
            BiosCheckStatus = "Checking HP for BIOS updates...";
            BiosStatusColor = Brushes.Gray;
            
            try
            {
                var sysInfo = _systemInfoService.GetSystemInfo();
                var result = await _biosUpdateService.CheckForUpdatesAsync(sysInfo);
                
                LatestBiosVersion = result.LatestBiosVersion ?? "Unknown";
                BiosDownloadUrl = result.DownloadUrl ?? "";
                BiosUpdateAvailable = result.UpdateAvailable;
                
                if (result.UpdateAvailable)
                {
                    BiosCheckStatus = $"⬆️ Update available: {result.LatestBiosVersion}";
                    BiosStatusColor = new SolidColorBrush(Color.FromRgb(102, 187, 106)); // Green
                }
                else if (!string.IsNullOrEmpty(result.LatestBiosVersion))
                {
                    BiosCheckStatus = "✓ Your BIOS is up to date";
                    BiosStatusColor = new SolidColorBrush(Color.FromRgb(102, 187, 106)); // Green
                }
                else
                {
                    BiosCheckStatus = result.Message;
                    BiosStatusColor = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Orange
                }
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to check for BIOS updates", ex);
                BiosCheckStatus = $"Error: {ex.Message}";
                BiosStatusColor = new SolidColorBrush(Color.FromRgb(239, 83, 80)); // Red
            }
            finally
            {
                IsBiosCheckInProgress = false;
            }
        }
        
        private void DownloadBiosUpdate()
        {
            if (string.IsNullOrEmpty(BiosDownloadUrl))
                return;
                
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BiosDownloadUrl,
                    UseShellExecute = true
                });
                _logging.Info($"Opened BIOS download URL: {BiosDownloadUrl}");
            }
            catch (Exception ex)
            {
                _logging.Error("Failed to open BIOS download URL", ex);
                // Fallback to HP support page
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://support.hp.com/drivers",
                    UseShellExecute = true
                });
            }
        }

        #endregion
        
        private static class NativeMethods
        {
            public const uint GENERIC_READ = 0x80000000;
            public const uint GENERIC_WRITE = 0x40000000;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint FILE_SHARE_WRITE = 0x00000002;
            
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern SafeFileHandle CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);
        }
    }
}
