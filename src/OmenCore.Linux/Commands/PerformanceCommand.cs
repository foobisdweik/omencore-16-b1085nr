using System.CommandLine;
using OmenCore.Linux.Hardware;

namespace OmenCore.Linux.Commands;

/// <summary>
/// Performance mode control command.
/// 
/// Examples:
///   omencore-cli perf --mode balanced
///   omencore-cli perf --mode performance
/// </summary>
public static class PerformanceCommand
{
    public static Command Create()
    {
        var command = new Command("perf", "Control performance mode settings");
        
        var modeOption = new Option<string?>(
            aliases: new[] { "--mode", "-m" },
            description: "Performance mode: default, balanced, performance, cool");
            
        var tccOption = new Option<int?>(
            name: "--tcc",
            description: "TCC offset value (0-15)");
            
        var powerOption = new Option<int?>(
            name: "--power-limit",
            description: "Thermal power limit multiplier (0-5)");
        
        command.AddOption(modeOption);
        command.AddOption(tccOption);
        command.AddOption(powerOption);
        
        command.SetHandler(async (mode, tcc, power) =>
        {
            await HandlePerformanceCommandAsync(mode, tcc, power);
        }, modeOption, tccOption, powerOption);
        
        return command;
    }
    
    private static async Task HandlePerformanceCommandAsync(string? mode, int? tcc, int? power)
    {
        // Check root
        if (!LinuxEcController.CheckRootAccess())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Root privileges required. Run with sudo.");
            Console.ResetColor();
            return;
        }
        
        var ec = new LinuxEcController();
        
        if (!ec.IsAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Cannot access EC. Ensure ec_sys module is loaded.");
            Console.ResetColor();
            return;
        }
        
        // Handle mode
        if (!string.IsNullOrEmpty(mode))
        {
            var success = mode.ToLower() switch
            {
                "default" => ec.SetPerformanceMode(PerformanceMode.Default),
                "balanced" => ec.SetPerformanceMode(PerformanceMode.Balanced),
                "performance" => ec.SetPerformanceMode(PerformanceMode.Performance),
                "cool" => ec.SetPerformanceMode(PerformanceMode.Cool),
                _ => false
            };
            
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Performance mode set to: {mode}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to set performance mode: {mode}");
                Console.WriteLine($"  Valid modes: default, balanced, performance, cool");
                Console.ResetColor();
            }
            return;
        }
        
        // Handle TCC offset
        if (tcc.HasValue)
        {
            var offset = Math.Clamp(tcc.Value, 0, 15);
            if (ec.SetTccOffset(offset))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ TCC offset set to: {offset}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to set TCC offset");
                Console.ResetColor();
            }
            return;
        }
        
        // Handle power limit
        if (power.HasValue)
        {
            var limit = Math.Clamp(power.Value, 0, 5);
            if (ec.SetThermalPowerLimit(limit))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Thermal power limit set to: {limit}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to set thermal power limit");
                Console.ResetColor();
            }
            return;
        }
        
        // No options - show current status
        ShowPerformanceStatus(ec);
        await Task.CompletedTask;
    }
    
    private static void ShowPerformanceStatus(LinuxEcController ec)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║       Performance Status             ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        
        var mode = ec.GetPerformanceMode();
        var modeStr = mode switch
        {
            PerformanceMode.Default => "Default",
            PerformanceMode.Balanced => "Balanced",
            PerformanceMode.Performance => "Performance",
            PerformanceMode.Cool => "Cool",
            _ => "Unknown"
        };
        
        Console.WriteLine($"║  Mode: {modeStr,-27} ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();
    }
}
