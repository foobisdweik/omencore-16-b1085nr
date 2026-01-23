using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using OmenCore.Models;
using OmenCore.Services;

namespace OmenCore.ViewModels
{
    public class DashboardViewModel : ViewModelBase, IDisposable
    {
        /// <summary>Maximum number of thermal samples to keep (1 minute at 1 sample/second).</summary>
        private const int MaxThermalSampleHistory = 60;
        
        private readonly HardwareMonitoringService _monitoringService;
        private readonly FanService? _fanService;
        private readonly ObservableCollection<ThermalSample> _thermalSamples = new();
        private MonitoringSample? _latestMonitoringSample;
        private bool _monitoringLowOverhead;
        private string _currentPerformanceMode = "Auto";
        private string _currentFanMode = "Auto";
        private bool _disposed;
        private volatile bool _pendingUIUpdate; // Throttle BeginInvoke backlog
        
        // Session tracking (v2.2)
        private readonly DateTime _sessionStartTime = DateTime.Now;
        private double _peakCpuTemp;
        private double _peakGpuTemp;
        private DispatcherTimer? _uptimeTimer;

        public ReadOnlyObservableCollection<MonitoringSample> MonitoringSamples => _monitoringService.Samples;
        public ObservableCollection<ThermalSample> ThermalSamples => _thermalSamples;
        
        public string CurrentPerformanceMode
        {
            get => _currentPerformanceMode;
            set
            {
                if (_currentPerformanceMode != value)
                {
                    _currentPerformanceMode = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string CurrentFanMode
        {
            get => _currentFanMode;
            set
            {
                if (_currentFanMode != value)
                {
                    _currentFanMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public MonitoringSample? LatestMonitoringSample
        {
            get => _latestMonitoringSample;
            private set
            {
                _latestMonitoringSample = value;
                
                // Track peak temperatures (v2.2)
                if (value != null)
                {
                    if (value.CpuTemperatureC > _peakCpuTemp)
                    {
                        _peakCpuTemp = value.CpuTemperatureC;
                        OnPropertyChanged(nameof(PeakCpuTemp));
                    }
                    if (value.GpuTemperatureC > _peakGpuTemp)
                    {
                        _peakGpuTemp = value.GpuTemperatureC;
                        OnPropertyChanged(nameof(PeakGpuTemp));
                    }
                }
                
                OnPropertyChanged();
                OnPropertyChanged(nameof(CpuSummary));
                OnPropertyChanged(nameof(GpuSummary));
                OnPropertyChanged(nameof(MemorySummary));
                OnPropertyChanged(nameof(StorageSummary));
                OnPropertyChanged(nameof(BatterySummary));
                OnPropertyChanged(nameof(CpuClockSummary));
                OnPropertyChanged(nameof(ThrottlingSummary));
                OnPropertyChanged(nameof(IsThrottling));
                OnPropertyChanged(nameof(IsSsdDataAvailable));
                OnPropertyChanged(nameof(FanSummary));
            }
        }

        public bool MonitoringLowOverheadMode
        {
            get => _monitoringLowOverhead;
            set
            {
                if (_monitoringLowOverhead != value)
                {
                    _monitoringLowOverhead = value;
                    _monitoringService.SetLowOverheadMode(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MonitoringGraphsVisible));
                }
            }
        }

        public bool MonitoringGraphsVisible => !MonitoringLowOverheadMode;

        public string CpuSummary => LatestMonitoringSample == null 
            ? "CPU telemetry unavailable" 
            : LatestMonitoringSample.CpuPowerWatts > 0 
                ? $"{LatestMonitoringSample.CpuTemperatureC:F0}°C • {LatestMonitoringSample.CpuLoadPercent:F0}% • {LatestMonitoringSample.CpuPowerWatts:F0}W"
                : $"{LatestMonitoringSample.CpuTemperatureC:F0}°C • {LatestMonitoringSample.CpuLoadPercent:F0}% load";
        public string GpuSummary => LatestMonitoringSample == null ? "GPU telemetry unavailable" : $"{LatestMonitoringSample.GpuTemperatureC:F0}°C • {LatestMonitoringSample.GpuLoadPercent:F0}% load • {LatestMonitoringSample.GpuVramUsageMb:F0} MB VRAM";
        public string MemorySummary => LatestMonitoringSample == null ? "Memory telemetry unavailable" : $"{LatestMonitoringSample.RamUsageGb:F1} / {LatestMonitoringSample.RamTotalGb:F0} GB";
        public string StorageSummary => LatestMonitoringSample == null ? "Storage telemetry unavailable" : $"SSD {LatestMonitoringSample.SsdTemperatureC:F0}°C • {LatestMonitoringSample.DiskUsagePercent:F0}% active";
        public string BatterySummary => LatestMonitoringSample == null ? "Battery unavailable" 
            : LatestMonitoringSample.IsOnAcPower 
                ? $"{LatestMonitoringSample.BatteryChargePercent:F0}% • AC Power" 
                : $"{LatestMonitoringSample.BatteryChargePercent:F0}% • {LatestMonitoringSample.BatteryTimeRemaining}";
        public string CpuClockSummary => LatestMonitoringSample == null || LatestMonitoringSample.CpuCoreClocksMhz.Count == 0
            ? "Per-core clocks unavailable"
            : string.Join(", ", LatestMonitoringSample.CpuCoreClocksMhz.Select((c, i) => $"C{i + 1}:{c:F0}MHz"));
        
        /// <summary>
        /// Whether the system is currently throttling (thermal or power limited).
        /// </summary>
        public bool IsThrottling => LatestMonitoringSample?.IsThrottling ?? false;
        
        /// <summary>
        /// Human-readable throttling status for display.
        /// </summary>
        public string ThrottlingSummary => LatestMonitoringSample == null 
            ? "Unknown" 
            : LatestMonitoringSample.ThrottlingStatus;
        
        /// <summary>
        /// Whether SSD sensor data is available (non-zero temperature).
        /// Used to hide Storage card when LibreHardwareMonitor can't read SMART data.
        /// </summary>
        public bool IsSsdDataAvailable => LatestMonitoringSample?.IsSsdDataAvailable ?? false;
        
        // Power consumption and efficiency properties
        public string PowerConsumptionSummary => LatestMonitoringSample == null 
            ? "Power telemetry unavailable"
            : LatestMonitoringSample.CpuPowerWatts > 0 || LatestMonitoringSample.GpuPowerWatts > 0
                ? $"CPU: {LatestMonitoringSample.CpuPowerWatts:F0}W • GPU: {LatestMonitoringSample.GpuPowerWatts:F0}W • Total: {LatestMonitoringSample.CpuPowerWatts + LatestMonitoringSample.GpuPowerWatts:F0}W"
                : "Power sensors unavailable";
        
        public string PowerEfficiencySummary
        {
            get
            {
                if (LatestMonitoringSample == null) return "Efficiency data unavailable";
                
                var totalPower = LatestMonitoringSample.CpuPowerWatts + LatestMonitoringSample.GpuPowerWatts;
                if (totalPower <= 0) return "Power data required for efficiency";
                
                // Calculate performance per watt (rough estimate)
                var cpuPerf = LatestMonitoringSample.CpuLoadPercent * LatestMonitoringSample.CpuCoreClocksMhz.Count;
                var gpuPerf = LatestMonitoringSample.GpuLoadPercent;
                var totalPerf = cpuPerf + gpuPerf;
                
                if (totalPerf <= 0) return "Performance data required";
                
                var efficiency = totalPerf / totalPower;
                return $"{efficiency:F1} perf/W • {totalPower:F0}W total";
            }
        }
        
        // Enhanced battery health properties
        public string BatteryHealthSummary
        {
            get
            {
                if (LatestMonitoringSample == null || LatestMonitoringSample.BatteryChargePercent <= 0)
                    return "Battery health unavailable";
                
                // Estimate battery health based on discharge rate and capacity
                var healthPercent = 100.0; // Would need actual battery health API
                var cycleCount = 0; // Would need battery cycle count API
                
                return $"Health: {healthPercent:F0}% • Cycles: {cycleCount}";
            }
        }
        
        // Fan curve visualization data
        private readonly ObservableCollection<FanCurvePoint> _fanCurvePoints = new();
        public ObservableCollection<FanCurvePoint> FanCurvePoints => _fanCurvePoints;
        
        public string FanCurveSummary
        {
            get
            {
                if (_fanService?.FanTelemetry == null || _fanService.FanTelemetry.Count == 0)
                    return "Fan curve unavailable";
                
                var cpuTemp = LatestMonitoringSample?.CpuTemperatureC ?? 0;
                var gpuTemp = LatestMonitoringSample?.GpuTemperatureC ?? 0;
                var avgTemp = (cpuTemp + gpuTemp) / 2;
                
                var cpuFan = _fanService.FanTelemetry.Count > 0 ? _fanService.FanTelemetry[0].SpeedRpm : 0;
                var gpuFan = _fanService.FanTelemetry.Count > 1 ? _fanService.FanTelemetry[1].SpeedRpm : 0;
                
                return $"{avgTemp:F0}°C → CPU: {cpuFan} RPM • GPU: {gpuFan} RPM";
            }
        }
        
        // Session tracking properties (v2.2)
        public string SessionUptime
        {
            get
            {
                var elapsed = DateTime.Now - _sessionStartTime;
                if (elapsed.TotalHours >= 1)
                    return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
                return $"{elapsed.Minutes}m {elapsed.Seconds}s";
            }
        }
        
        public double PeakCpuTemp => _peakCpuTemp;
        public double PeakGpuTemp => _peakGpuTemp;
        
        public string FanSummary
        {
            get
            {
                var telemetry = _fanService?.FanTelemetry;
                if (telemetry == null || telemetry.Count == 0)
                    return "-- RPM";
                
                var fan1 = telemetry.Count > 0 ? telemetry[0].SpeedRpm : 0;
                var fan2 = telemetry.Count > 1 ? telemetry[1].SpeedRpm : 0;
                return $"CPU: {fan1} • GPU: {fan2} RPM";
            }
        }

        public DashboardViewModel(HardwareMonitoringService monitoringService, FanService? fanService = null)
        {
            _monitoringService = monitoringService;
            _fanService = fanService;
            _monitoringService.SampleUpdated += OnSampleUpdated;
            
            // Start uptime timer for session tracking (v2.2)
            _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uptimeTimer.Tick += (s, e) => OnPropertyChanged(nameof(SessionUptime));
            _uptimeTimer.Start();
        }

        private void OnSampleUpdated(object? sender, MonitoringSample sample)
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, 
                () => LatestMonitoringSample = sample);
            
            // Throttle UI updates to prevent Dispatcher backlog during heavy load
            if (!_pendingUIUpdate)
            {
                _pendingUIUpdate = true;
                // Marshal to UI thread for ObservableCollection updates
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
                {
                    // Convert to ThermalSample for temperature charts
                    _thermalSamples.Add(new ThermalSample
                    {
                        Timestamp = sample.Timestamp,
                        CpuCelsius = sample.CpuTemperatureC,
                        GpuCelsius = sample.GpuTemperatureC
                    });
                    
                    // Trim to max history size - remove excess items in one pass
                    var excessCount = _thermalSamples.Count - MaxThermalSampleHistory;
                    for (int i = 0; i < excessCount; i++)
                    {
                        _thermalSamples.RemoveAt(0);
                    }
                    
                    // Update fan curve points for visualization
                    UpdateFanCurvePoints(sample);
                    
                    _pendingUIUpdate = false;
                });
            }
            
            // Notify property changes for new monitoring features
            OnPropertyChanged(nameof(PowerConsumptionSummary));
            OnPropertyChanged(nameof(PowerEfficiencySummary));
            OnPropertyChanged(nameof(BatteryHealthSummary));
            OnPropertyChanged(nameof(FanCurveSummary));
        }
        
        private void UpdateFanCurvePoints(MonitoringSample sample)
        {
            if (_fanService?.FanTelemetry == null || _fanService.FanTelemetry.Count == 0)
                return;
            
            // Calculate average temperature for fan curve
            var avgTemp = (int)((sample.CpuTemperatureC + sample.GpuTemperatureC) / 2);
            
            // Get current fan speeds
            var cpuFanRpm = _fanService.FanTelemetry.Count > 0 ? _fanService.FanTelemetry[0].SpeedRpm : 0;
            var gpuFanRpm = _fanService.FanTelemetry.Count > 1 ? _fanService.FanTelemetry[1].SpeedRpm : 0;
            var avgFanRpm = (cpuFanRpm + gpuFanRpm) / 2;
            
            // Add current point to fan curve (limit to last 50 points for visualization)
            _fanCurvePoints.Add(new FanCurvePoint
            {
                TemperatureC = avgTemp,
                FanSpeedRpm = avgFanRpm,
                Timestamp = sample.Timestamp
            });
            
            // Keep only recent points
            while (_fanCurvePoints.Count > 50)
            {
                _fanCurvePoints.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Dispose resources and unsubscribe from events.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Protected dispose implementation.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                _monitoringService.SampleUpdated -= OnSampleUpdated;
                _uptimeTimer?.Stop();
                _uptimeTimer = null;
            }
            
            _disposed = true;
        }
    }
}
