namespace OmenCore.Linux.Hardware;

/// <summary>
/// Linux hwmon sensor interface for temperature monitoring.
/// 
/// Reads from /sys/class/hwmon/* to get CPU and GPU temperatures.
/// This is preferred over EC-based temperature reading when available.
/// </summary>
public class LinuxHwMonController
{
    private const string HWMON_PATH = "/sys/class/hwmon";
    
    private string? _cpuHwmonPath;
    private string? _gpuHwmonPath;
    
    public LinuxHwMonController()
    {
        DiscoverSensors();
    }
    
    private void DiscoverSensors()
    {
        if (!Directory.Exists(HWMON_PATH))
            return;
            
        foreach (var hwmonDir in Directory.GetDirectories(HWMON_PATH))
        {
            try
            {
                var namePath = Path.Combine(hwmonDir, "name");
                if (!File.Exists(namePath))
                    continue;
                    
                var name = File.ReadAllText(namePath).Trim().ToLower();
                
                // CPU temperature sensors
                if (name.Contains("coretemp") || name.Contains("k10temp") || name.Contains("zenpower"))
                {
                    _cpuHwmonPath = hwmonDir;
                }
                
                // GPU temperature sensors
                if (name.Contains("nouveau") || name.Contains("amdgpu") || name.Contains("nvidia"))
                {
                    _gpuHwmonPath = hwmonDir;
                }
            }
            catch
            {
                // Ignore errors during discovery
            }
        }
    }
    
    /// <summary>
    /// Get CPU temperature from hwmon.
    /// </summary>
    public int? GetCpuTemperature()
    {
        if (_cpuHwmonPath == null)
            return null;
            
        return ReadTemperature(_cpuHwmonPath, "temp1_input") ??
               ReadTemperature(_cpuHwmonPath, "temp2_input");
    }
    
    /// <summary>
    /// Get GPU temperature from hwmon.
    /// </summary>
    public int? GetGpuTemperature()
    {
        if (_gpuHwmonPath == null)
            return null;
            
        return ReadTemperature(_gpuHwmonPath, "temp1_input");
    }
    
    /// <summary>
    /// Read temperature from a hwmon temp file.
    /// Temperature files report millidegrees Celsius.
    /// </summary>
    private int? ReadTemperature(string hwmonPath, string tempFile)
    {
        try
        {
            var path = Path.Combine(hwmonPath, tempFile);
            if (!File.Exists(path))
                return null;
                
            var content = File.ReadAllText(path).Trim();
            if (int.TryParse(content, out var millidegrees))
            {
                return millidegrees / 1000; // Convert from millidegrees to degrees
            }
        }
        catch
        {
            // Ignore read errors
        }
        
        return null;
    }
    
    /// <summary>
    /// Get all available temperature sensors.
    /// </summary>
    public IEnumerable<(string Name, string Path, int Temperature)> GetAllSensors()
    {
        var results = new List<(string Name, string Path, int Temperature)>();
        
        if (!Directory.Exists(HWMON_PATH))
            return results;
            
        foreach (var hwmonDir in Directory.GetDirectories(HWMON_PATH))
        {
            string? name = null;
            try
            {
                var namePath = Path.Combine(hwmonDir, "name");
                if (File.Exists(namePath))
                    name = File.ReadAllText(namePath).Trim();
            }
            catch { }
            
            // Find temp files
            try
            {
                foreach (var tempFile in Directory.GetFiles(hwmonDir, "temp*_input"))
                {
                    try
                    {
                        var content = File.ReadAllText(tempFile).Trim();
                        if (int.TryParse(content, out var millidegrees))
                        {
                            var label = Path.GetFileNameWithoutExtension(tempFile);
                            results.Add((name ?? "unknown", label, millidegrees / 1000));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        return results;
    }
}
