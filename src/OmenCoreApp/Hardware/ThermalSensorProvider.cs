using System;
using System.Collections.Generic;
using OmenCore.Models;
using OmenCore.Services;

namespace OmenCore.Hardware
{
    public class ThermalSensorProvider
    {
        private readonly LibreHardwareMonitorImpl? _bridge;
        private readonly HpWmiBios? _wmiBios;
        
        /// <summary>
        /// Create ThermalSensorProvider with LibreHardwareMonitorImpl for full monitoring
        /// </summary>
        public ThermalSensorProvider(LibreHardwareMonitorImpl bridge)
        {
            _bridge = bridge;
        }
        
        /// <summary>
        /// Create ThermalSensorProvider with IHardwareMonitorBridge interface.
        /// Will use LibreHardwareMonitor if available, otherwise WMI BIOS fallback.
        /// </summary>
        public ThermalSensorProvider(IHardwareMonitorBridge bridge)
        {
            _bridge = bridge as LibreHardwareMonitorImpl;
            if (_bridge == null)
            {
                // Use WMI BIOS fallback for temperature readings
                _wmiBios = new HpWmiBios(null);
            }
        }

        public IEnumerable<TemperatureReading> ReadTemperatures()
        {
            var list = new List<TemperatureReading>();

            double cpuTemp = 0;
            double gpuTemp = 0;
            
            // Try LibreHardwareMonitor first
            if (_bridge != null)
            {
                cpuTemp = _bridge.GetCpuTemperature();
                gpuTemp = _bridge.GetGpuTemperature();
            }
            // Fall back to WMI BIOS
            else if (_wmiBios != null && _wmiBios.IsAvailable)
            {
                var temps = _wmiBios.GetBothTemperatures();
                if (temps.HasValue)
                {
                    var (cpu, gpu) = temps.Value;
                    cpuTemp = cpu;
                    gpuTemp = gpu;
                }
            }

            if (cpuTemp > 0)
            {
                list.Add(new TemperatureReading { Sensor = "CPU Package", Celsius = cpuTemp });
            }

            if (gpuTemp > 0)
            {
                list.Add(new TemperatureReading { Sensor = "GPU", Celsius = gpuTemp });
            }

            // Fallback if no data available
            if (list.Count == 0)
            {
                list.Add(new TemperatureReading { Sensor = "CPU", Celsius = 0 });
                list.Add(new TemperatureReading { Sensor = "GPU", Celsius = 0 });
            }

            return list;
        }
    }
}
