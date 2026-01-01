namespace OmenCore.Linux.Hardware;

/// <summary>
/// Linux EC (Embedded Controller) interface for fan and performance control.
/// 
/// Access via /sys/kernel/debug/ec/ec0/io requires:
///   1. Root privileges
///   2. ec_sys kernel module loaded with write_support=1:
///      sudo modprobe ec_sys write_support=1
/// 
/// Register map based on omen-fan project:
/// https://github.com/alou-S/omen-fan/blob/main/docs/probes.md
/// </summary>
public class LinuxEcController
{
    // EC sysfs path
    private const string EC_PATH = "/sys/kernel/debug/ec/ec0/io";
    
    // EC Register addresses (from omen-fan)
    private const byte REG_FAN1_SPEED_SET = 0x34;      // Fan 1 speed in units of 100 RPM
    private const byte REG_FAN2_SPEED_SET = 0x35;      // Fan 2 speed in units of 100 RPM
    private const byte REG_FAN1_SPEED_PCT = 0x2E;      // Fan 1 speed 0-100%
    private const byte REG_FAN2_SPEED_PCT = 0x2F;      // Fan 2 speed 0-100%
    private const byte REG_FAN_BOOST = 0xEC;           // Fan boost: 0x00=OFF, 0x0C=ON
    private const byte REG_FAN_STATE = 0xF4;           // Fan state: 0x00=Enable, 0x02=Disable
    private const byte REG_CPU_TEMP = 0x57;            // CPU temperature
    private const byte REG_GPU_TEMP = 0xB7;            // GPU temperature
    private const byte REG_BIOS_CONTROL = 0x62;        // BIOS control: 0x00=Enabled, 0x06=Disabled
    private const byte REG_TIMER = 0x63;               // Timer (counts down from 0x78)
    private const byte REG_PERF_MODE = 0x95;           // Performance mode
    private const byte REG_THERMAL_POWER = 0xBA;       // Thermal power limit (0-5)
    
    // Performance mode values
    private const byte PERF_MODE_DEFAULT = 0x30;
    private const byte PERF_MODE_PERFORMANCE = 0x31;
    private const byte PERF_MODE_COOL = 0x50;
    
    public bool IsAvailable { get; }
    
    public LinuxEcController()
    {
        IsAvailable = File.Exists(EC_PATH);
    }
    
    public static bool CheckRootAccess()
    {
        return Environment.UserName == "root" || Mono.Unix.Native.Syscall.getuid() == 0;
    }
    
    /// <summary>
    /// Read a byte from the EC at the specified address.
    /// </summary>
    public byte? ReadByte(byte address)
    {
        if (!IsAvailable) return null;
        
        try
        {
            using var fs = new FileStream(EC_PATH, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(address, SeekOrigin.Begin);
            var value = fs.ReadByte();
            return value >= 0 ? (byte)value : null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Write a byte to the EC at the specified address.
    /// </summary>
    public bool WriteByte(byte address, byte value)
    {
        if (!IsAvailable) return false;
        
        try
        {
            using var fs = new FileStream(EC_PATH, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fs.Seek(address, SeekOrigin.Begin);
            fs.WriteByte(value);
            fs.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    #region Fan Control
    
    /// <summary>
    /// Get current fan speeds in RPM.
    /// </summary>
    public (int fan1, int fan2) GetFanSpeeds()
    {
        var fan1 = (ReadByte(REG_FAN1_SPEED_SET) ?? 0) * 100;
        var fan2 = (ReadByte(REG_FAN2_SPEED_SET) ?? 0) * 100;
        return (fan1, fan2);
    }
    
    /// <summary>
    /// Get current fan speeds as percentage (0-100).
    /// </summary>
    public (int fan1, int fan2) GetFanSpeedPercent()
    {
        var fan1 = ReadByte(REG_FAN1_SPEED_PCT) ?? 0;
        var fan2 = ReadByte(REG_FAN2_SPEED_PCT) ?? 0;
        return (fan1, fan2);
    }
    
    /// <summary>
    /// Set Fan 1 speed in units of 100 RPM.
    /// </summary>
    public bool SetFan1Speed(byte speedUnit)
    {
        return WriteByte(REG_FAN1_SPEED_SET, speedUnit);
    }
    
    /// <summary>
    /// Set Fan 2 speed in units of 100 RPM.
    /// </summary>
    public bool SetFan2Speed(byte speedUnit)
    {
        return WriteByte(REG_FAN2_SPEED_SET, speedUnit);
    }
    
    /// <summary>
    /// Set both fan speeds to the same percentage.
    /// </summary>
    public bool SetFanSpeedPercent(int percent)
    {
        var pct = (byte)Math.Clamp(percent, 0, 100);
        // Convert % to RPM units (assuming max ~5500 RPM = 55 units)
        var speedUnit = (byte)(pct * 55 / 100);
        
        return SetFan1Speed(speedUnit) && SetFan2Speed(speedUnit);
    }
    
    /// <summary>
    /// Enable or disable fan boost mode.
    /// </summary>
    public bool SetFanBoost(bool enabled)
    {
        return WriteByte(REG_FAN_BOOST, (byte)(enabled ? 0x0C : 0x00));
    }
    
    /// <summary>
    /// Set fan profile.
    /// </summary>
    public bool SetFanProfile(FanProfile profile)
    {
        return profile switch
        {
            FanProfile.Auto => SetFanState(true),
            FanProfile.Silent => SetFanSpeedPercent(30),
            FanProfile.Balanced => SetFanSpeedPercent(50),
            FanProfile.Gaming => SetFanSpeedPercent(80),
            FanProfile.Max => SetFanSpeedPercent(100),
            _ => false
        };
    }
    
    /// <summary>
    /// Enable or disable BIOS fan control.
    /// </summary>
    public bool SetFanState(bool biosControl)
    {
        return WriteByte(REG_FAN_STATE, (byte)(biosControl ? 0x00 : 0x02));
    }
    
    #endregion
    
    #region Temperature
    
    /// <summary>
    /// Get CPU temperature from EC.
    /// </summary>
    public int? GetCpuTemperature()
    {
        return ReadByte(REG_CPU_TEMP);
    }
    
    /// <summary>
    /// Get GPU temperature from EC.
    /// </summary>
    public int? GetGpuTemperature()
    {
        return ReadByte(REG_GPU_TEMP);
    }
    
    #endregion
    
    #region Performance
    
    /// <summary>
    /// Get current performance mode.
    /// </summary>
    public PerformanceMode GetPerformanceMode()
    {
        var value = ReadByte(REG_PERF_MODE);
        return value switch
        {
            PERF_MODE_DEFAULT => PerformanceMode.Default,
            PERF_MODE_PERFORMANCE => PerformanceMode.Performance,
            PERF_MODE_COOL => PerformanceMode.Cool,
            _ => PerformanceMode.Balanced
        };
    }
    
    /// <summary>
    /// Set performance mode.
    /// </summary>
    public bool SetPerformanceMode(PerformanceMode mode)
    {
        var value = mode switch
        {
            PerformanceMode.Default => PERF_MODE_DEFAULT,
            PerformanceMode.Performance => PERF_MODE_PERFORMANCE,
            PerformanceMode.Cool => PERF_MODE_COOL,
            PerformanceMode.Balanced => PERF_MODE_DEFAULT,
            _ => PERF_MODE_DEFAULT
        };
        
        return WriteByte(REG_PERF_MODE, value);
    }
    
    /// <summary>
    /// Set TCC offset (0-15).
    /// Note: This may not work on all models via EC.
    /// </summary>
    public bool SetTccOffset(int offset)
    {
        // TCC offset is typically set via MSR, not EC
        // This is a placeholder for potential EC-based TCC control
        return false;
    }
    
    /// <summary>
    /// Set thermal power limit multiplier (0-5).
    /// </summary>
    public bool SetThermalPowerLimit(int level)
    {
        var value = (byte)Math.Clamp(level, 0, 5);
        return WriteByte(REG_THERMAL_POWER, value);
    }
    
    #endregion
}

public enum FanProfile
{
    Auto,
    Silent,
    Balanced,
    Gaming,
    Max
}

public enum PerformanceMode
{
    Default,
    Balanced,
    Performance,
    Cool
}
