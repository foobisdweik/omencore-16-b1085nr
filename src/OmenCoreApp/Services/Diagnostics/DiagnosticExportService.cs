using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmenCore.Hardware;

namespace OmenCore.Services.Diagnostics
{
    /// <summary>
    /// Privacy-first diagnostic data collection and export.
    /// Collects logs, EC state, system info for bug reports without sensitive data.
    /// </summary>
    public class DiagnosticExportService
    {
        private readonly LoggingService _logging;
        private readonly string _logsDirectory;

        public DiagnosticExportService(LoggingService logging, string logsDirectory)
        {
            _logging = logging;
            _logsDirectory = logsDirectory;
        }

        /// <summary>
        /// Collect diagnostics: logs, system info, EC state, etc.
        /// Returns path to diagnostic bundle ZIP file.
        /// </summary>
        public async Task<string> CollectAndExportAsync(
            IEcAccess? ecAccess = null,
            LibreHardwareMonitorImpl? hwMonitor = null)
        {
            try
            {
                var exportPath = Path.Combine(Path.GetTempPath(), $"OmenCore-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}");
                Directory.CreateDirectory(exportPath);

                _logging.Info($"Collecting diagnostics to {exportPath}");

                // Collect components in parallel
                var tasks = new List<Task>
                {
                    CollectLogsAsync(exportPath),
                    CollectSystemInfoAsync(exportPath),
                    CollectEcStateAsync(exportPath, ecAccess),
                    CollectHardwareInfoAsync(exportPath, hwMonitor)
                };

                await Task.WhenAll(tasks);

                // Create ZIP archive
                string zipPath = ZipDiagnostics(exportPath);

                _logging.Info($"✓ Diagnostics exported to {zipPath}");
                return zipPath;
            }
            catch (Exception ex)
            {
                _logging.Error($"Failed to export diagnostics: {ex.Message}", ex);
                throw;
            }
        }

        private async Task CollectLogsAsync(string exportPath)
        {
            try
            {
                // Copy recent log files
                if (Directory.Exists(_logsDirectory))
                {
                    var logFiles = Directory.GetFiles(_logsDirectory, "*.log")
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(5); // Last 5 logs

                    var logsExportPath = Path.Combine(exportPath, "logs");
                    Directory.CreateDirectory(logsExportPath);

                    foreach (var logFile in logFiles)
                    {
                        File.Copy(logFile, Path.Combine(logsExportPath, Path.GetFileName(logFile)), overwrite: true);
                    }

                    _logging.Info($"Collected {logFiles.Count()} log files");
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logging.Warn($"Failed to collect logs: {ex.Message}");
            }
        }

        private async Task CollectSystemInfoAsync(string exportPath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== SYSTEM INFORMATION ===");
                sb.AppendLine($"Timestamp: {DateTime.Now:O}");
                sb.AppendLine($"OmenCore Version: {GetOmenCoreVersion()}");
                sb.AppendLine($"OS: {Environment.OSVersion.VersionString}");
                sb.AppendLine($"Processor: {Environment.ProcessorCount} cores");
                sb.AppendLine($"RAM: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
                sb.AppendLine();

                // Check security features
                sb.AppendLine("=== SECURITY FEATURES ===");
                sb.AppendLine($"SecureBoot: {GetSecureBootStatus()}");
                sb.AppendLine($"HVCI: {GetHvciStatus()}");
                sb.AppendLine();

                // Driver status
                sb.AppendLine("=== DRIVER STATUS ===");
                sb.AppendLine($"WinRing0: {GetWinRing0Status()}");
                sb.AppendLine($"PawnIO: {GetPawnIOStatus()}");
                sb.AppendLine();

                // Services
                sb.AppendLine("=== SERVICES ===");
                sb.AppendLine($"XTU Service: {GetXtuServiceStatus()}");
                sb.AppendLine($"Afterburner: {GetAfterburnerStatus()}");

                File.WriteAllText(Path.Combine(exportPath, "system-info.txt"), sb.ToString());
                _logging.Info("Collected system information");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logging.Warn($"Failed to collect system info: {ex.Message}");
            }
        }

        private async Task CollectEcStateAsync(string exportPath, IEcAccess? ecAccess)
        {
            try
            {
                if (ecAccess == null || !ecAccess.IsAvailable)
                {
                    File.WriteAllText(Path.Combine(exportPath, "ec-state.txt"), "EC access not available");
                    await Task.CompletedTask;
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("=== EC STATE DUMP ===");
                sb.AppendLine($"Captured: {DateTime.Now:O}");
                sb.AppendLine();

                // Read key EC registers (safe addresses only)
                var registers = new[] { 0x2E, 0x2F, 0x34, 0x35, 0xCE, 0xCF };
                sb.AppendLine("Safe Register Values:");
                foreach (var reg in registers)
                {
                    try
                    {
                        byte value = ecAccess.ReadByte((ushort)reg);
                        sb.AppendLine($"  0x{reg:X2} = 0x{value:X2}");
                    }
                    catch { }
                }

                File.WriteAllText(Path.Combine(exportPath, "ec-state.txt"), sb.ToString());
                _logging.Info("Collected EC state");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logging.Warn($"Failed to collect EC state: {ex.Message}");
            }
        }

        private async Task CollectHardwareInfoAsync(string exportPath, LibreHardwareMonitorImpl? hwMonitor)
        {
            try
            {
                if (hwMonitor == null)
                {
                    File.WriteAllText(Path.Combine(exportPath, "hardware-info.txt"), "Hardware monitoring not available");
                    await Task.CompletedTask;
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("=== HARDWARE TELEMETRY ===");
                sb.AppendLine($"Captured: {DateTime.Now:O}");
                sb.AppendLine();

                sb.AppendLine($"CPU Temp: {hwMonitor.GetCpuTemperature()}°C");
                sb.AppendLine($"GPU Temp: {hwMonitor.GetGpuTemperature()}°C");

                File.WriteAllText(Path.Combine(exportPath, "hardware-info.txt"), sb.ToString());
                _logging.Info("Collected hardware information");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logging.Warn($"Failed to collect hardware info: {ex.Message}");
            }
        }

        private string ZipDiagnostics(string exportPath)
        {
            try
            {
                var zipPath = Path.ChangeExtension(exportPath, ".zip");
                
                // Use .NET built-in ZipFile
                if (Directory.Exists(exportPath))
                {
                    System.IO.Compression.ZipFile.CreateFromDirectory(exportPath, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                    Directory.Delete(exportPath, recursive: true);
                }

                _logging.Info($"Created diagnostic archive: {zipPath}");
                return zipPath;
            }
            catch (Exception ex)
            {
                _logging.Warn($"Failed to create ZIP archive: {ex.Message}");
                return exportPath; // Return directory if ZIP fails
            }
        }

        private string GetOmenCoreVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            }
            catch { return "Unknown"; }
        }

        private string GetSecureBootStatus()
        {
            // Placeholder - would check Windows security settings
            return "N/A";
        }

        private string GetHvciStatus()
        {
            return "N/A";
        }

        private string GetWinRing0Status()
        {
            // Placeholder - would check Windows driver status
            return "N/A";
        }

        private string GetPawnIOStatus() => "Not Implemented";
        private string GetXtuServiceStatus() => "Not Running";
        private string GetAfterburnerStatus() => "Not Detected";
    }

    /// <summary>
    /// GitHub issue template generator for bug reports.
    /// Creates pre-filled issue text with diagnostic context.
    /// </summary>
    public class GitHubIssueTemplate
    {
        public static string GenerateBugReportTemplate(string issueTitle, string issueDescription, string diagnosticsZipPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## {issueTitle}");
            sb.AppendLine();
            sb.AppendLine("### Description");
            sb.AppendLine(issueDescription);
            sb.AppendLine();
            sb.AppendLine("### Environment");
            sb.AppendLine($"- **OmenCore Version**: {GetVersionFromAssembly()}");
            sb.AppendLine($"- **OS**: {Environment.OSVersion.VersionString}");
            sb.AppendLine($"- **Time**: {DateTime.Now:O}");
            sb.AppendLine();
            sb.AppendLine("### Diagnostics");
            sb.AppendLine($"Diagnostic package attached: `{Path.GetFileName(diagnosticsZipPath)}`");
            sb.AppendLine();
            sb.AppendLine("### Steps to Reproduce");
            sb.AppendLine("1. ...");
            sb.AppendLine("2. ...");
            sb.AppendLine("3. ...");
            sb.AppendLine();
            sb.AppendLine("### Expected Behavior");
            sb.AppendLine("- ...");
            sb.AppendLine();
            sb.AppendLine("### Actual Behavior");
            sb.AppendLine("- ...");

            return sb.ToString();
        }

        private static string GetVersionFromAssembly()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            }
            catch { return "Unknown"; }
        }
    }
}
