using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LibreHardwareMonitor.Hardware;

namespace OmenCore.HardwareWorker;

/// <summary>
/// Out-of-process hardware monitoring worker.
/// Hosts LibreHardwareMonitor in a separate process to isolate crashes (especially NVML).
/// Communicates with main OmenCore app via named pipes.
/// 
/// If this process crashes due to driver issues, the main app continues running
/// and can restart this worker automatically.
/// </summary>
class Program
{
    private const string PipeName = "OmenCore_HardwareWorker";
    private const int UpdateIntervalMs = 500;
    
    private static Computer? _computer;
    private static readonly object _lock = new();
    private static HardwareSample _lastSample = new();
    private static bool _running = true;
    private static DateTime _lastUpdate = DateTime.MinValue;

    static async Task Main(string[] args)
    {
        // Set up crash handler to log before exit
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            File.AppendAllText(GetLogPath(), $"[{DateTime.Now:O}] WORKER CRASH: {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}\n");
        };

        Console.WriteLine($"OmenCore Hardware Worker starting... PID={Environment.ProcessId}");
        
        try
        {
            InitializeHardware();
            
            // Start background update thread
            var updateTask = Task.Run(UpdateLoop);
            
            // Run pipe server
            await RunPipeServer();
        }
        catch (Exception ex)
        {
            File.AppendAllText(GetLogPath(), $"[{DateTime.Now:O}] WORKER ERROR: {ex}\n");
            throw;
        }
        finally
        {
            CleanupHardware();
        }
    }

    private static string GetLogPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(localAppData, "OmenCore");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "HardwareWorker.log");
    }

    private static void InitializeHardware()
    {
        Console.WriteLine("Initializing LibreHardwareMonitor...");
        
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true,
            IsMotherboardEnabled = true
        };
        
        _computer.Open();
        Console.WriteLine("LibreHardwareMonitor initialized.");
    }

    private static void CleanupHardware()
    {
        _running = false;
        try
        {
            _computer?.Close();
        }
        catch { }
    }

    private static async Task UpdateLoop()
    {
        while (_running)
        {
            try
            {
                UpdateHardwareReadings();
            }
            catch (AccessViolationException ex)
            {
                // NVML crash - log and continue (may crash process anyway)
                File.AppendAllText(GetLogPath(), $"[{DateTime.Now:O}] NVML ACCESS VIOLATION: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(GetLogPath(), $"[{DateTime.Now:O}] Update error: {ex.Message}\n");
            }
            
            await Task.Delay(UpdateIntervalMs);
        }
    }

    private static void UpdateHardwareReadings()
    {
        if (_computer == null) return;
        
        lock (_lock)
        {
            var sample = new HardwareSample();
            
            _computer.Accept(new UpdateVisitor());
            
            foreach (var hardware in _computer.Hardware)
            {
                try
                {
                    hardware.Update();
                    ProcessHardware(hardware, sample);
                    
                    foreach (var subHardware in hardware.SubHardware)
                    {
                        subHardware.Update();
                        ProcessSubHardware(subHardware, sample);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue with other hardware
                    File.AppendAllText(GetLogPath(), $"[{DateTime.Now:O}] Error updating {hardware.Name}: {ex.Message}\n");
                }
            }
            
            sample.Timestamp = DateTime.Now;
            _lastSample = sample;
            _lastUpdate = DateTime.Now;
        }
    }

    private static void ProcessHardware(IHardware hardware, HardwareSample sample)
    {
        switch (hardware.HardwareType)
        {
            case HardwareType.Cpu:
                // CPU Temperature - prefer Core #1 over Package (more stable)
                var cpuTemp = GetSensorValueMulti(hardware, SensorType.Temperature, 
                    "Core #1", "Core #0", "Core (Tctl/Tdie)", "CPU Package");
                if (cpuTemp > 0) sample.CpuTemperature = cpuTemp;
                
                var cpuLoad = GetSensorValue(hardware, SensorType.Load, "CPU Total");
                if (cpuLoad > 0) sample.CpuLoad = cpuLoad;
                
                var cpuPower = GetSensorValueMulti(hardware, SensorType.Power, 
                    "CPU Package", "Package Power");
                if (cpuPower > 0) sample.CpuPower = cpuPower;
                break;
                
            case HardwareType.GpuNvidia:
            case HardwareType.GpuAmd:
                // GPU Temperature - prefer Core over Hotspot (more stable)
                var gpuTemp = GetSensorValueMulti(hardware, SensorType.Temperature, 
                    "GPU Core", "Core");
                if (gpuTemp > 0) sample.GpuTemperature = gpuTemp;
                
                var gpuHotspot = GetSensorValueMulti(hardware, SensorType.Temperature, 
                    "GPU Hot Spot", "Hot Spot");
                if (gpuHotspot > 0) sample.GpuHotspot = gpuHotspot;
                
                var gpuLoad = GetSensorValueMulti(hardware, SensorType.Load, "GPU Core");
                if (gpuLoad > 0) sample.GpuLoad = gpuLoad;
                
                var gpuPower = GetSensorValueMulti(hardware, SensorType.Power, "GPU Power");
                if (gpuPower > 0) sample.GpuPower = gpuPower;
                
                var gpuClock = GetSensorValue(hardware, SensorType.Clock, "GPU Core");
                if (gpuClock > 0) sample.GpuClock = gpuClock;
                
                var gpuMemClock = GetSensorValue(hardware, SensorType.Clock, "GPU Memory");
                if (gpuMemClock > 0) sample.GpuMemoryClock = gpuMemClock;
                
                var vramUsed = GetSensorValueMulti(hardware, SensorType.SmallData, 
                    "GPU Memory Used", "D3D Dedicated Memory Used");
                if (vramUsed > 0) sample.VramUsage = vramUsed;
                
                var vramTotal = GetSensorValue(hardware, SensorType.SmallData, "GPU Memory Total");
                if (vramTotal > 0) sample.VramTotal = vramTotal;
                
                sample.GpuName = hardware.Name;
                break;
                
            case HardwareType.GpuIntel:
                // Intel Arc (dedicated) or integrated
                bool isArc = hardware.Name.Contains("Arc", StringComparison.OrdinalIgnoreCase);
                
                // Only override if Arc or no dedicated GPU found
                if (isArc || sample.GpuTemperature == 0)
                {
                    var intelTemp = GetSensorValueMulti(hardware, SensorType.Temperature, 
                        "GPU Core", "GPU Package");
                    if (intelTemp > 0) sample.GpuTemperature = intelTemp;
                    
                    if (isArc || string.IsNullOrEmpty(sample.GpuName))
                        sample.GpuName = hardware.Name;
                }
                
                if (isArc || sample.GpuLoad == 0)
                {
                    var intelLoad = GetSensorValueMulti(hardware, SensorType.Load, "GPU Core");
                    if (intelLoad > 0) sample.GpuLoad = intelLoad;
                }
                break;
                
            case HardwareType.Memory:
                var ramUsed = GetSensorValue(hardware, SensorType.Data, "Memory Used");
                var ramAvail = GetSensorValue(hardware, SensorType.Data, "Memory Available");
                if (ramUsed > 0) sample.RamUsage = ramUsed;
                if (ramUsed > 0 && ramAvail > 0) sample.RamTotal = ramUsed + ramAvail;
                break;
                
            case HardwareType.Storage:
                if (hardware.Name.Contains("NVMe") || hardware.Name.Contains("SSD"))
                {
                    var ssdTemp = GetSensorValue(hardware, SensorType.Temperature);
                    if (ssdTemp > 0 && sample.SsdTemperature == 0) sample.SsdTemperature = ssdTemp;
                }
                break;
                
            case HardwareType.Battery:
                var charge = GetSensorValueMulti(hardware, SensorType.Level, "Charge Level");
                if (charge > 0) sample.BatteryCharge = charge;
                
                var discharge = GetSensorValueMulti(hardware, SensorType.Power, "Discharge Rate");
                sample.BatteryDischargeRate = discharge;
                sample.IsOnAc = discharge <= 0;
                break;
        }
    }

    private static void ProcessSubHardware(IHardware subHardware, HardwareSample sample)
    {
        // Fan RPM from EC/motherboard
        foreach (var sensor in subHardware.Sensors.Where(s => s.SensorType == SensorType.Fan))
        {
            if (sensor.Value.HasValue && sensor.Value.Value > 0)
            {
                sample.FanSpeeds[sensor.Name] = sensor.Value.Value;
            }
        }
    }

    private static double GetSensorValue(IHardware hardware, SensorType type, string? name = null)
    {
        ISensor? sensor;
        if (name != null)
        {
            sensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == type && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            sensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == type);
        }
        
        return sensor?.Value ?? 0;
    }
    
    /// <summary>
    /// Get sensor value, trying multiple name patterns
    /// </summary>
    private static double GetSensorValueMulti(IHardware hardware, SensorType type, params string[] names)
    {
        foreach (var name in names)
        {
            var value = GetSensorValue(hardware, type, name);
            if (value > 0) return value;
        }
        // Fallback to any sensor of this type
        return GetSensorValue(hardware, type);
    }

    private static async Task RunPipeServer()
    {
        Console.WriteLine($"Starting named pipe server: {PipeName}");
        
        while (_running)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);
                
                Console.WriteLine("Waiting for client connection...");
                await server.WaitForConnectionAsync();
                Console.WriteLine("Client connected.");
                
                await HandleClient(server);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pipe error: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    private static async Task HandleClient(NamedPipeServerStream server)
    {
        var buffer = new byte[4096];
        
        try
        {
            while (server.IsConnected && _running)
            {
                // Read request
                var bytesRead = await server.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                
                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                
                string response;
                if (request == "PING")
                {
                    response = "PONG";
                }
                else if (request == "GET")
                {
                    lock (_lock)
                    {
                        response = JsonSerializer.Serialize(_lastSample);
                    }
                }
                else if (request == "SHUTDOWN")
                {
                    response = "OK";
                    _running = false;
                }
                else
                {
                    response = "UNKNOWN";
                }
                
                // Send response
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await server.WriteAsync(responseBytes, 0, responseBytes.Length);
                await server.FlushAsync();
            }
        }
        catch (IOException)
        {
            // Client disconnected
            Console.WriteLine("Client disconnected.");
        }
    }
}

/// <summary>
/// Visitor pattern for LibreHardwareMonitor updates
/// </summary>
internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);
    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
            sub.Accept(this);
    }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}

/// <summary>
/// Hardware sample data transferred via IPC
/// </summary>
public class HardwareSample
{
    public DateTime Timestamp { get; set; }
    
    // CPU
    public double CpuTemperature { get; set; }
    public double CpuLoad { get; set; }
    public double CpuPower { get; set; }
    
    // GPU
    public string GpuName { get; set; } = "";
    public double GpuTemperature { get; set; }
    public double GpuHotspot { get; set; }
    public double GpuLoad { get; set; }
    public double GpuPower { get; set; }
    public double GpuClock { get; set; }
    public double GpuMemoryClock { get; set; }
    public double VramUsage { get; set; }
    public double VramTotal { get; set; }
    
    // Memory
    public double RamUsage { get; set; }
    public double RamTotal { get; set; }
    
    // Storage
    public double SsdTemperature { get; set; }
    
    // Battery
    public double BatteryCharge { get; set; }
    public double BatteryDischargeRate { get; set; }
    public bool IsOnAc { get; set; } = true;
    
    // Fans (name -> RPM)
    public Dictionary<string, double> FanSpeeds { get; set; } = new();
}
