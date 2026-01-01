using System.CommandLine;
using System.Diagnostics;

namespace OmenCore.Linux.Commands;

/// <summary>
/// Daemon management command for running OmenCore as a background service.
/// 
/// Examples:
///   omencore-cli daemon --start
///   omencore-cli daemon --stop
///   omencore-cli daemon --status
///   omencore-cli daemon --install   (install systemd service)
///   omencore-cli daemon --uninstall (remove systemd service)
/// </summary>
public static class DaemonCommand
{
    private const string ServiceName = "omencore";
    private const string PidFile = "/var/run/omencore.pid";
    
    private static readonly string SystemdServicePath = $"/etc/systemd/system/{ServiceName}.service";
    
    public static Command Create()
    {
        var command = new Command("daemon", "Manage OmenCore background daemon");
        
        var startOption = new Option<bool>(
            aliases: new[] { "--start" },
            description: "Start the daemon");
            
        var stopOption = new Option<bool>(
            aliases: new[] { "--stop" },
            description: "Stop the daemon");
            
        var statusOption = new Option<bool>(
            aliases: new[] { "--status" },
            description: "Check daemon status");
            
        var installOption = new Option<bool>(
            aliases: new[] { "--install" },
            description: "Install systemd service");
            
        var uninstallOption = new Option<bool>(
            aliases: new[] { "--uninstall" },
            description: "Uninstall systemd service");
            
        var generateOption = new Option<bool>(
            aliases: new[] { "--generate-service" },
            description: "Print systemd service file to stdout");
        
        command.AddOption(startOption);
        command.AddOption(stopOption);
        command.AddOption(statusOption);
        command.AddOption(installOption);
        command.AddOption(uninstallOption);
        command.AddOption(generateOption);
        
        command.SetHandler(async (start, stop, status, install, uninstall, generate) =>
        {
            await HandleDaemonCommandAsync(start, stop, status, install, uninstall, generate);
        }, startOption, stopOption, statusOption, installOption, uninstallOption, generateOption);
        
        return command;
    }
    
    private static async Task HandleDaemonCommandAsync(
        bool start, bool stop, bool status, bool install, bool uninstall, bool generate)
    {
        if (generate)
        {
            PrintSystemdService();
            return;
        }
        
        if (install)
        {
            await InstallServiceAsync();
            return;
        }
        
        if (uninstall)
        {
            await UninstallServiceAsync();
            return;
        }
        
        if (start)
        {
            await StartDaemonAsync();
            return;
        }
        
        if (stop)
        {
            await StopDaemonAsync();
            return;
        }
        
        // Default: show status
        await ShowStatusAsync();
    }
    
    private static void PrintSystemdService()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "/usr/local/bin/omencore-cli";
        
        Console.WriteLine($@"[Unit]
Description=OmenCore HP OMEN Laptop Control Daemon
After=network.target

[Service]
Type=simple
ExecStart={exePath} monitor --interval 2000
Restart=on-failure
RestartSec=5
User=root
Environment=HOME=/root

# Apply saved configuration on start
ExecStartPre={exePath} config --apply

[Install]
WantedBy=multi-user.target");
    }
    
    private static async Task InstallServiceAsync()
    {
        if (Mono.Unix.Native.Syscall.getuid() != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Root privileges required to install service");
            Console.ResetColor();
            return;
        }
        
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "/usr/local/bin/omencore-cli";
            
            var serviceContent = $@"[Unit]
Description=OmenCore HP OMEN Laptop Control Daemon
After=network.target

[Service]
Type=simple
ExecStart={exePath} monitor --interval 2000
Restart=on-failure
RestartSec=5
User=root
Environment=HOME=/root

# Apply saved configuration on start
ExecStartPre={exePath} config --apply

[Install]
WantedBy=multi-user.target";
            
            await File.WriteAllTextAsync(SystemdServicePath, serviceContent);
            
            // Reload systemd and enable service
            await RunCommandAsync("systemctl", "daemon-reload");
            await RunCommandAsync("systemctl", "enable omencore.service");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Systemd service installed");
            Console.WriteLine();
            Console.WriteLine("To start the service:");
            Console.WriteLine("  sudo systemctl start omencore");
            Console.WriteLine();
            Console.WriteLine("To check status:");
            Console.WriteLine("  sudo systemctl status omencore");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error installing service: {ex.Message}");
            Console.ResetColor();
        }
    }
    
    private static async Task UninstallServiceAsync()
    {
        if (Mono.Unix.Native.Syscall.getuid() != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Root privileges required to uninstall service");
            Console.ResetColor();
            return;
        }
        
        try
        {
            await RunCommandAsync("systemctl", "stop omencore.service");
            await RunCommandAsync("systemctl", "disable omencore.service");
            
            if (File.Exists(SystemdServicePath))
            {
                File.Delete(SystemdServicePath);
            }
            
            await RunCommandAsync("systemctl", "daemon-reload");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Systemd service uninstalled");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error uninstalling service: {ex.Message}");
            Console.ResetColor();
        }
    }
    
    private static async Task StartDaemonAsync()
    {
        if (File.Exists(SystemdServicePath))
        {
            await RunCommandAsync("systemctl", "start omencore.service");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Service started via systemd");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Systemd service not installed. Install with: omencore-cli daemon --install");
            Console.WriteLine();
            Console.WriteLine("Starting monitor in foreground instead...");
            Console.ResetColor();
            
            // Fall back to running monitor command
            var args = new[] { "monitor", "--interval", "2000" };
            await new System.CommandLine.RootCommand().InvokeAsync(args);
        }
    }
    
    private static async Task StopDaemonAsync()
    {
        if (File.Exists(SystemdServicePath))
        {
            await RunCommandAsync("systemctl", "stop omencore.service");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Service stopped");
            Console.ResetColor();
        }
        else
        {
            // Try to find and kill by PID file
            if (File.Exists(PidFile))
            {
                var pid = await File.ReadAllTextAsync(PidFile);
                await RunCommandAsync("kill", pid.Trim());
                File.Delete(PidFile);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Daemon stopped");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No running daemon found");
                Console.ResetColor();
            }
        }
    }
    
    private static async Task ShowStatusAsync()
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              OmenCore Linux - Daemon Status               ║");
        Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
        
        var serviceInstalled = File.Exists(SystemdServicePath);
        Console.WriteLine($"║  Systemd service: {(serviceInstalled ? "✓ Installed" : "✗ Not installed"),-38} ║");
        
        if (serviceInstalled)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "is-active omencore.service",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                
                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    var isActive = output.Trim() == "active";
                    Console.WriteLine($"║  Service status:  {(isActive ? "✓ Running" : "✗ Stopped"),-38} ║");
                }
            }
            catch
            {
                Console.WriteLine("║  Service status:  ? Unknown                               ║");
            }
        }
        
        Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  Commands:                                                ║");
        Console.WriteLine("║    daemon --install     Install systemd service           ║");
        Console.WriteLine("║    daemon --start       Start the daemon                  ║");
        Console.WriteLine("║    daemon --stop        Stop the daemon                   ║");
        Console.WriteLine("║    daemon --uninstall   Remove systemd service            ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }
    
    private static async Task RunCommandAsync(string command, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        using var process = Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }
}
