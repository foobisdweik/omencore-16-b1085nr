using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OmenCore.Models;

namespace OmenCore.Services
{
    public interface IHardwareMonitoringService
    {
        Task<HardwareMetrics> GetCurrentMetricsAsync();
        Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync();
        Task<IEnumerable<HistoricalDataPoint>> GetHistoricalDataAsync(ChartType chartType, TimeSpan timeRange);
        Task<string> ExportMonitoringDataAsync();
        Task StartMonitoringAsync();
        Task StopMonitoringAsync();
        bool IsMonitoring { get; }
    }

    public class HardwareMetrics
    {
        public double PowerConsumption { get; set; }
        public double PowerConsumptionTrend { get; set; }
        public double BatteryHealthPercentage { get; set; }
        public int BatteryCycles { get; set; }
        public double EstimatedBatteryLifeYears { get; set; }
        public double CpuTemperature { get; set; }
        public double GpuTemperature { get; set; }
        public double PowerEfficiency { get; set; }
        public double FanEfficiency { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SystemAlert
    {
        public string? Icon { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? Timestamp { get; set; }
        public AlertSeverity Severity { get; set; }
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    public class HistoricalDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string? Label { get; set; }
    }

    public enum ChartType
    {
        PowerConsumption,
        BatteryHealth,
        Temperature,
        FanSpeeds
    }
}