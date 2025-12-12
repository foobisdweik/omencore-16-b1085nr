using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using OmenCore.Services;

namespace OmenCore.Hardware
{
    /// <summary>
    /// OMEN Gaming Hub Service Proxy - provides hardware control through OGH's WMI interface.
    /// 
    /// Some 2023+ OMEN laptops require the OGH background services to be running for
    /// WMI BIOS commands to function. This proxy detects if OGH is available and uses
    /// the hpCpsPubGetSetCommand interface when direct BIOS commands fail.
    /// 
    /// Services detected:
    /// - HPOmenCap (HP Omen HSA Service)
    /// - OmenCommandCenterBackground (main command processor)
    /// - OmenCap (capability service)
    /// - omenmqtt (messaging service)
    /// </summary>
    public class OghServiceProxy : IDisposable
    {
        private readonly LoggingService? _logging;
        private bool _disposed;
        private ManagementObject? _oghInterface;
        
        private const string WmiNamespace = @"root\WMI";
        private const string OghCommandClass = "hpCpsPubGetSetCommand";
        
        // Known OGH service names
        private static readonly string[] OghServiceNames = new[]
        {
            "HPOmenCap",           // HP Omen HSA Service
            "HPOmenCommandCenter", // Command Center Service (older)
        };
        
        // Known OGH process names
        private static readonly string[] OghProcessNames = new[]
        {
            "OmenCommandCenterBackground",
            "OmenCap",
            "omenmqtt",
            "OmenInstallMonitor",
            "OMEN Command Center"
        };

        // OGH command signatures (reverse-engineered from OGH)
        private const uint OGH_SIGNATURE = 0x4F4D454E; // "OMEN"
        
        /// <summary>
        /// Thermal policies supported by OGH.
        /// </summary>
        public enum ThermalPolicy
        {
            Default = 0,      // Balanced thermal profile
            Performance = 1,  // High performance, higher temps allowed
            Cool = 2,         // Cooler operation, may throttle
            L5P = 3           // Legacy OMEN 5 Pro mode
        }

        /// <summary>
        /// Status of OMEN Gaming Hub services.
        /// </summary>
        public class OghStatus
        {
            public bool IsInstalled { get; set; }
            public bool IsRunning { get; set; }
            public bool WmiAvailable { get; set; }
            public bool CommandsWork { get; set; }
            public string[] RunningServices { get; set; } = Array.Empty<string>();
            public string[] RunningProcesses { get; set; } = Array.Empty<string>();
            public string Message { get; set; } = "";
        }

        public OghStatus Status { get; private set; } = new();
        public bool IsAvailable => Status.WmiAvailable && Status.CommandsWork;

        public OghServiceProxy(LoggingService? logging = null)
        {
            _logging = logging;
            DetectOghStatus();
        }

        /// <summary>
        /// Detect the status of OMEN Gaming Hub services and WMI interface.
        /// </summary>
        public void DetectOghStatus()
        {
            Status = new OghStatus();
            
            try
            {
                // Check for OGH services using WMI
                var runningServices = new System.Collections.Generic.List<string>();
                foreach (var serviceName in OghServiceNames)
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Service WHERE Name = '{serviceName}'");
                        foreach (ManagementObject service in searcher.Get())
                        {
                            Status.IsInstalled = true;
                            var state = service["State"]?.ToString();
                            if (state?.Equals("Running", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                runningServices.Add(serviceName);
                            }
                        }
                    }
                    catch (ManagementException)
                    {
                        // Service doesn't exist or WMI query failed
                    }
                }
                Status.RunningServices = runningServices.ToArray();
                
                // Check for OGH processes
                var runningProcesses = new System.Collections.Generic.List<string>();
                var allProcesses = Process.GetProcesses();
                foreach (var processName in OghProcessNames)
                {
                    if (allProcesses.Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)))
                    {
                        runningProcesses.Add(processName);
                        Status.IsInstalled = true;
                    }
                }
                Status.RunningProcesses = runningProcesses.ToArray();
                
                Status.IsRunning = runningServices.Count > 0 || runningProcesses.Count > 0;
                
                // Log detection results
                if (Status.IsInstalled)
                {
                    _logging?.Info($"OMEN Gaming Hub detected:");
                    if (Status.RunningServices.Length > 0)
                        _logging?.Info($"  Services: {string.Join(", ", Status.RunningServices)}");
                    if (Status.RunningProcesses.Length > 0)
                        _logging?.Info($"  Processes: {string.Join(", ", Status.RunningProcesses)}");
                }
                else
                {
                    _logging?.Info("OMEN Gaming Hub not installed");
                }
                
                // Check for OGH WMI interface
                if (Status.IsRunning)
                {
                    CheckOghWmiInterface();
                }
                
                // Build status message
                if (Status.CommandsWork)
                {
                    Status.Message = "OGH proxy available - fan control enabled through Gaming Hub services";
                }
                else if (Status.WmiAvailable)
                {
                    Status.Message = "OGH WMI interface found but commands not functional";
                }
                else if (Status.IsRunning)
                {
                    Status.Message = "OGH services running but WMI interface not accessible (need admin)";
                }
                else if (Status.IsInstalled)
                {
                    Status.Message = "OGH installed but services not running";
                }
                else
                {
                    Status.Message = "OGH not installed - direct BIOS control may be available";
                }
                
                _logging?.Info($"OGH Status: {Status.Message}");
            }
            catch (Exception ex)
            {
                _logging?.Error($"Error detecting OGH status: {ex.Message}", ex);
                Status.Message = $"Detection error: {ex.Message}";
            }
        }

        private void CheckOghWmiInterface()
        {
            try
            {
                // Check if the hpCpsPubGetSetCommand class exists
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {OghCommandClass}");
                var results = searcher.Get();
                
                foreach (ManagementObject obj in results)
                {
                    _oghInterface = obj;
                    Status.WmiAvailable = true;
                    _logging?.Info($"  OGH WMI interface found: {OghCommandClass}");
                    break;
                }
                
                if (Status.WmiAvailable && _oghInterface != null)
                {
                    // Test if commands work
                    Status.CommandsWork = TestOghCommands();
                }
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.AccessDenied)
            {
                _logging?.Warn("OGH WMI interface requires administrator access");
                Status.Message = "OGH WMI requires administrator privileges";
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Could not access OGH WMI interface: {ex.Message}");
            }
        }

        private bool TestOghCommands()
        {
            // Actually test a command to verify the interface works for this model.
            // We use "Fan:GetData" as it's a read-only command that should be supported.
            if (_oghInterface != null && Status.IsRunning)
            {
                try
                {
                    var result = ExecuteOghCommand("Fan:GetData", null);
                    if (result != null)
                    {
                        _logging?.Info("  OGH command test successful (Fan:GetData) ✓");
                        return true;
                    }
                    
                    _logging?.Warn("  OGH command test failed (Fan:GetData returned null or error)");
                }
                catch (Exception ex)
                {
                    _logging?.Warn($"  OGH command test threw exception: {ex.Message}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Execute a command through the OGH WMI interface.
        /// </summary>
        public byte[]? ExecuteOghCommand(string command, byte[]? inputData)
        {
            if (!Status.WmiAvailable || _oghInterface == null)
                return null;
                
            try
            {
                var inParams = _oghInterface.GetMethodParameters("hpCpsPubGetCommand");
                inParams["Command"] = command;
                inParams["SignIn"] = OGH_SIGNATURE;
                
                var outParams = _oghInterface.InvokeMethod("hpCpsPubGetCommand", inParams, null);
                
                if (outParams != null)
                {
                    var returnCode = (uint)outParams["ReturnCode"];
                    if (returnCode == 0)
                    {
                        return outParams["hpqBDataOut"] as byte[];
                    }
                    else
                    {
                        _logging?.Warn($"OGH command '{command}' returned error code: {returnCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"OGH command execution failed: {ex.Message}", ex);
            }
            
            return null;
        }

        /// <summary>
        /// Set a value through the OGH WMI interface.
        /// </summary>
        public bool ExecuteOghSetCommand(string command, byte[] inputData)
        {
            if (!Status.WmiAvailable || _oghInterface == null)
                return false;
                
            try
            {
                var inParams = _oghInterface.GetMethodParameters("hpCpsPubSetCommand");
                inParams["Command"] = command;
                inParams["SignIn"] = OGH_SIGNATURE;
                inParams["DataSizeIn"] = (uint)inputData.Length;
                inParams["hpqBDataIn"] = inputData;
                
                var outParams = _oghInterface.InvokeMethod("hpCpsPubSetCommand", inParams, null);
                
                if (outParams != null)
                {
                    var returnCode = (uint)outParams["ReturnCode"];
                    if (returnCode == 0)
                    {
                        _logging?.Info($"OGH command '{command}' executed successfully");
                        return true;
                    }
                    else
                    {
                        _logging?.Warn($"OGH command '{command}' returned error code: {returnCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.Error($"OGH set command execution failed: {ex.Message}", ex);
            }
            
            return false;
        }

        /// <summary>
        /// Try to set fan mode through OGH if it's available.
        /// </summary>
        public bool SetFanMode(string mode)
        {
            // Map mode names to OGH command format
            var command = mode.ToLower() switch
            {
                "performance" => "FanControl:SetMode:Performance",
                "balanced" or "default" => "FanControl:SetMode:Balanced",
                "quiet" or "cool" => "FanControl:SetMode:Quiet",
                "max" or "maximum" => "FanControl:SetMode:Maximum",
                _ => $"FanControl:SetMode:{mode}"
            };
            
            return ExecuteOghSetCommand(command, new byte[0]);
        }

        /// <summary>
        /// Set thermal policy through OGH.
        /// </summary>
        public bool SetThermalPolicy(ThermalPolicy policy)
        {
            var policyName = policy switch
            {
                ThermalPolicy.Performance => "Performance",
                ThermalPolicy.Cool => "Cool",
                ThermalPolicy.L5P => "L5P",
                _ => "Default"
            };
            
            _logging?.Info($"Setting OGH thermal policy: {policyName}");
            
            // Try multiple command formats that OGH may support
            var commands = new[]
            {
                $"Thermal:SetPolicy:{policyName}",
                $"ThermalPolicy:Set:{policyName}",
                $"FanControl:SetMode:{policyName}"
            };
            
            foreach (var cmd in commands)
            {
                if (ExecuteOghSetCommand(cmd, new byte[] { (byte)policy }))
                    return true;
            }
            
            // Fallback: try SetFanMode
            return SetFanMode(policyName);
        }

        /// <summary>
        /// Enable or disable maximum fan speed through OGH.
        /// </summary>
        public bool SetMaxFan(bool enabled)
        {
            _logging?.Info($"Setting OGH max fan: {(enabled ? "enabled" : "disabled")}");
            
            var commands = new[]
            {
                enabled ? "FanControl:SetMax:Enable" : "FanControl:SetMax:Disable",
                enabled ? "Fan:MaxSpeed:On" : "Fan:MaxSpeed:Off",
                $"FanControl:SetMode:{(enabled ? "Maximum" : "Balanced")}"
            };
            
            foreach (var cmd in commands)
            {
                if (ExecuteOghSetCommand(cmd, new byte[] { enabled ? (byte)1 : (byte)0 }))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Get fan telemetry data from OGH.
        /// </summary>
        public OmenCore.Models.FanTelemetry[]? GetFanData()
        {
            try
            {
                var result = ExecuteOghCommand("Fan:GetData", null);
                if (result != null && result.Length >= 8)
                {
                    // Parse OGH fan data format (varies by model)
                    // Typical format: [Fan1RPM_Low, Fan1RPM_High, Fan1Duty, Fan2RPM_Low, Fan2RPM_High, Fan2Duty, ...]
                    var fans = new System.Collections.Generic.List<OmenCore.Models.FanTelemetry>();
                    
                    for (int i = 0; i + 2 < result.Length; i += 3)
                    {
                        int rpm = result[i] | (result[i + 1] << 8);
                        int duty = result[i + 2];
                        
                        if (rpm > 0 || duty > 0)
                        {
                            fans.Add(new OmenCore.Models.FanTelemetry
                            {
                                Name = $"Fan {fans.Count + 1}",
                                SpeedRpm = rpm,
                                DutyCyclePercent = duty,
                                Temperature = 0 // Temperature not in this data
                            });
                        }
                    }
                    
                    if (fans.Count > 0)
                        return fans.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logging?.Warn($"Failed to get OGH fan data: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Try to get GPU mode through OGH if it's available.
        /// </summary>
        public string? GetGpuMode()
        {
            var result = ExecuteOghCommand("GPU:GetMode", null);
            if (result != null && result.Length > 0)
            {
                return result[0] switch
                {
                    0 => "Hybrid",
                    1 => "Discrete",
                    2 => "Optimus",
                    _ => $"Unknown ({result[0]})"
                };
            }
            return null;
        }

        /// <summary>
        /// Start OGH services if they're installed but not running.
        /// Returns true if services are now running.
        /// </summary>
        public bool TryStartOghServices()
        {
            if (!Status.IsInstalled || Status.IsRunning)
                return Status.IsRunning;
                
            try
            {
                foreach (var serviceName in OghServiceNames)
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Service WHERE Name = '{serviceName}'");
                        foreach (ManagementObject service in searcher.Get())
                        {
                            var state = service["State"]?.ToString();
                            if (state?.Equals("Stopped", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _logging?.Info($"Starting OGH service: {serviceName}");
                                // Use WMI to start the service
                                var result = service.InvokeMethod("StartService", null);
                                if (result != null && (uint)result == 0)
                                {
                                    _logging?.Info($"Started {serviceName} successfully");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logging?.Warn($"Could not start {serviceName}: {ex.Message}");
                    }
                }
                
                // Re-detect status after starting services
                System.Threading.Thread.Sleep(2000); // Give services time to initialize
                DetectOghStatus();
                return Status.IsRunning;
            }
            catch (Exception ex)
            {
                _logging?.Error($"Failed to start OGH services: {ex.Message}", ex);
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _oghInterface?.Dispose();
                _disposed = true;
            }
        }
    }
}
