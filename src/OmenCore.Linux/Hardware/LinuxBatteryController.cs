namespace OmenCore.Linux.Hardware;

/// <summary>
/// Linux battery status detection for low-overhead mode (#22)
/// Reads from /sys/class/power_supply/* to detect AC/battery state.
/// </summary>
public class LinuxBatteryController
{
    private const string PowerSupplyPath = "/sys/class/power_supply";
    
    private string? _batteryPath;
    private string? _acAdapterPath;
    private DateTime _lastCheck = DateTime.MinValue;
    private bool _lastOnBattery = false;
    private readonly TimeSpan _cacheTime = TimeSpan.FromSeconds(10);
    
    public LinuxBatteryController()
    {
        DiscoverPowerSupply();
    }
    
    private void DiscoverPowerSupply()
    {
        if (!Directory.Exists(PowerSupplyPath))
            return;
            
        foreach (var dir in Directory.GetDirectories(PowerSupplyPath))
        {
            try
            {
                var typePath = Path.Combine(dir, "type");
                if (!File.Exists(typePath))
                    continue;
                    
                var type = File.ReadAllText(typePath).Trim().ToLowerInvariant();
                
                if (type == "battery")
                {
                    _batteryPath = dir;
                }
                else if (type == "mains" || type == "usb")
                {
                    _acAdapterPath = dir;
                }
            }
            catch
            {
                // Ignore discovery errors
            }
        }
    }
    
    /// <summary>
    /// Check if currently running on battery power.
    /// Uses caching to reduce file system access.
    /// </summary>
    public bool IsOnBattery()
    {
        // Use cached value if recent
        if (DateTime.Now - _lastCheck < _cacheTime)
            return _lastOnBattery;
            
        _lastCheck = DateTime.Now;
        
        // Check AC adapter first (most reliable)
        if (!string.IsNullOrEmpty(_acAdapterPath))
        {
            var onlinePath = Path.Combine(_acAdapterPath, "online");
            if (File.Exists(onlinePath))
            {
                try
                {
                    var online = File.ReadAllText(onlinePath).Trim();
                    _lastOnBattery = online == "0"; // 0 = not plugged in = on battery
                    return _lastOnBattery;
                }
                catch { }
            }
        }
        
        // Fallback: check battery status
        if (!string.IsNullOrEmpty(_batteryPath))
        {
            var statusPath = Path.Combine(_batteryPath, "status");
            if (File.Exists(statusPath))
            {
                try
                {
                    var status = File.ReadAllText(statusPath).Trim().ToLowerInvariant();
                    _lastOnBattery = status == "discharging";
                    return _lastOnBattery;
                }
                catch { }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get battery percentage (0-100).
    /// </summary>
    public int? GetBatteryPercentage()
    {
        if (string.IsNullOrEmpty(_batteryPath))
            return null;
            
        var capacityPath = Path.Combine(_batteryPath, "capacity");
        if (!File.Exists(capacityPath))
            return null;
            
        try
        {
            var content = File.ReadAllText(capacityPath).Trim();
            if (int.TryParse(content, out var capacity))
                return capacity;
        }
        catch { }
        
        return null;
    }
    
    /// <summary>
    /// Get battery status string.
    /// </summary>
    public string? GetBatteryStatus()
    {
        if (string.IsNullOrEmpty(_batteryPath))
            return null;
            
        var statusPath = Path.Combine(_batteryPath, "status");
        if (!File.Exists(statusPath))
            return null;
            
        try
        {
            return File.ReadAllText(statusPath).Trim();
        }
        catch
        {
            return null;
        }
    }
}
