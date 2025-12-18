using System;
using System.Collections.Generic;

namespace OmenCore.Services.SystemOptimizer
{
    /// <summary>
    /// Represents the current state of all optimizations.
    /// </summary>
    public class OptimizationState
    {
        public PowerOptimizationState Power { get; set; } = new();
        public ServiceOptimizationState Services { get; set; } = new();
        public NetworkOptimizationState Network { get; set; } = new();
        public InputOptimizationState Input { get; set; } = new();
        public VisualOptimizationState Visual { get; set; } = new();
        public StorageOptimizationState Storage { get; set; } = new();
        
        public DateTime LastChecked { get; set; }
        
        public int ActiveCount => 
            Power.ActiveCount + Services.ActiveCount + Network.ActiveCount + 
            Input.ActiveCount + Visual.ActiveCount + Storage.ActiveCount;
            
        public int TotalCount =>
            Power.TotalCount + Services.TotalCount + Network.TotalCount +
            Input.TotalCount + Visual.TotalCount + Storage.TotalCount;
    }

    public class PowerOptimizationState
    {
        public bool UltimatePerformancePlan { get; set; }
        public bool HardwareGpuScheduling { get; set; }
        public bool GameModeEnabled { get; set; }
        public bool ForegroundPriority { get; set; }
        
        public int ActiveCount => (UltimatePerformancePlan ? 1 : 0) + (HardwareGpuScheduling ? 1 : 0) + 
            (GameModeEnabled ? 1 : 0) + (ForegroundPriority ? 1 : 0);
        public int TotalCount => 4;
    }

    public class ServiceOptimizationState
    {
        public bool TelemetryDisabled { get; set; }
        public bool SysMainDisabled { get; set; }      // Superfetch
        public bool SearchIndexingDisabled { get; set; }
        public bool DiagTrackDisabled { get; set; }    // Connected User Experiences
        
        public int ActiveCount => (TelemetryDisabled ? 1 : 0) + (SysMainDisabled ? 1 : 0) + 
            (SearchIndexingDisabled ? 1 : 0) + (DiagTrackDisabled ? 1 : 0);
        public int TotalCount => 4;
    }

    public class NetworkOptimizationState
    {
        public bool TcpNoDelay { get; set; }
        public bool TcpAckFrequency { get; set; }
        public bool DeliveryOptimizationDisabled { get; set; }
        public bool NagleDisabled { get; set; }
        
        public int ActiveCount => (TcpNoDelay ? 1 : 0) + (TcpAckFrequency ? 1 : 0) + 
            (DeliveryOptimizationDisabled ? 1 : 0) + (NagleDisabled ? 1 : 0);
        public int TotalCount => 4;
    }

    public class InputOptimizationState
    {
        public bool MouseAccelerationDisabled { get; set; }
        public bool GameDvrDisabled { get; set; }
        public bool GameBarDisabled { get; set; }
        public bool FullscreenOptimizationsDisabled { get; set; }
        
        public int ActiveCount => (MouseAccelerationDisabled ? 1 : 0) + (GameDvrDisabled ? 1 : 0) + 
            (GameBarDisabled ? 1 : 0) + (FullscreenOptimizationsDisabled ? 1 : 0);
        public int TotalCount => 4;
    }

    public class VisualOptimizationState
    {
        public string Mode { get; set; } = "Default"; // Default, Balanced, Minimal
        public bool AnimationsDisabled { get; set; }
        public bool TransparencyDisabled { get; set; }
        
        public int ActiveCount => (AnimationsDisabled ? 1 : 0) + (TransparencyDisabled ? 1 : 0);
        public int TotalCount => 2;
    }

    public class StorageOptimizationState
    {
        public bool IsSsd { get; set; }
        public bool TrimEnabled { get; set; }
        public bool DefragDisabled { get; set; }
        public bool ShortNamesDisabled { get; set; }   // 8.3 filename creation
        public bool LastAccessDisabled { get; set; }
        
        public int ActiveCount => (TrimEnabled && IsSsd ? 1 : 0) + (DefragDisabled && IsSsd ? 1 : 0) + 
            (ShortNamesDisabled ? 1 : 0) + (LastAccessDisabled ? 1 : 0);
        public int TotalCount => 4;
    }

    /// <summary>
    /// Result of applying or reverting an optimization.
    /// </summary>
    public class OptimizationResult
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresReboot { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Defines an individual optimization setting.
    /// </summary>
    public class OptimizationDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public OptimizationRisk Risk { get; set; } = OptimizationRisk.Low;
        public bool RequiresAdmin { get; set; } = true;
        public bool RequiresReboot { get; set; }
        public bool IsRecommended { get; set; }
        public string? Warning { get; set; }
    }

    public enum OptimizationRisk
    {
        Low,        // Safe, no side effects
        Medium,     // May affect some functionality
        High        // Aggressive, may cause issues
    }
}
