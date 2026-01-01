using System.CommandLine;
using OmenCore.Linux.Hardware;

namespace OmenCore.Linux.Commands;

/// <summary>
/// Fan control command.
/// 
/// Examples:
///   omencore-cli fan --profile auto
///   omencore-cli fan --speed 80
///   omencore-cli fan --curve "40:20,50:30,60:50,80:80,90:100"
/// </summary>
public static class FanCommand
{
    public static Command Create()
    {
        var command = new Command("fan", "Control fan speed and profiles");
        
        // Options
        var profileOption = new Option<string?>(
            aliases: new[] { "--profile", "-p" },
            description: "Fan profile: auto, silent, balanced, gaming, max");
            
        var speedOption = new Option<int?>(
            aliases: new[] { "--speed", "-s" },
            description: "Manual fan speed percentage (0-100)");
            
        var curveOption = new Option<string?>(
            aliases: new[] { "--curve", "-c" },
            description: "Custom fan curve: temp:speed pairs (e.g., '40:20,50:30,60:50,80:80,90:100')");
            
        var fan1Option = new Option<int?>(
            name: "--fan1",
            description: "Set Fan 1 (CPU) speed in RPM");
            
        var fan2Option = new Option<int?>(
            name: "--fan2",
            description: "Set Fan 2 (GPU) speed in RPM");
            
        var boostOption = new Option<bool?>(
            aliases: new[] { "--boost", "-b" },
            description: "Enable/disable fan boost mode");
        
        command.AddOption(profileOption);
        command.AddOption(speedOption);
        command.AddOption(curveOption);
        command.AddOption(fan1Option);
        command.AddOption(fan2Option);
        command.AddOption(boostOption);
        
        command.SetHandler(async (profile, speed, curve, fan1, fan2, boost) =>
        {
            await HandleFanCommandAsync(profile, speed, curve, fan1, fan2, boost);
        }, profileOption, speedOption, curveOption, fan1Option, fan2Option, boostOption);
        
        return command;
    }
    
    private static async Task HandleFanCommandAsync(
        string? profile, int? speed, string? curve, 
        int? fan1, int? fan2, bool? boost)
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
        
        // Check EC access
        if (!ec.IsAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Cannot access EC. Ensure ec_sys module is loaded with write_support=1");
            Console.WriteLine("  sudo modprobe ec_sys write_support=1");
            Console.ResetColor();
            return;
        }
        
        // Handle profile
        if (!string.IsNullOrEmpty(profile))
        {
            var success = profile.ToLower() switch
            {
                "auto" => ec.SetFanProfile(FanProfile.Auto),
                "silent" => ec.SetFanProfile(FanProfile.Silent),
                "balanced" => ec.SetFanProfile(FanProfile.Balanced),
                "gaming" => ec.SetFanProfile(FanProfile.Gaming),
                "max" => ec.SetFanProfile(FanProfile.Max),
                _ => false
            };
            
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Fan profile set to: {profile}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to set fan profile: {profile}");
                Console.ResetColor();
            }
            return;
        }
        
        // Handle speed
        if (speed.HasValue)
        {
            var pct = Math.Clamp(speed.Value, 0, 100);
            if (ec.SetFanSpeedPercent(pct))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Fan speed set to: {pct}%");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to set fan speed");
                Console.ResetColor();
            }
            return;
        }
        
        // Handle individual fan RPM
        if (fan1.HasValue || fan2.HasValue)
        {
            if (fan1.HasValue)
            {
                var rpm = (byte)(fan1.Value / 100);
                if (ec.SetFan1Speed(rpm))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Fan 1 speed set to: {fan1.Value} RPM");
                    Console.ResetColor();
                }
            }
            
            if (fan2.HasValue)
            {
                var rpm = (byte)(fan2.Value / 100);
                if (ec.SetFan2Speed(rpm))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Fan 2 speed set to: {fan2.Value} RPM");
                    Console.ResetColor();
                }
            }
            return;
        }
        
        // Handle boost
        if (boost.HasValue)
        {
            if (ec.SetFanBoost(boost.Value))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Fan boost {(boost.Value ? "enabled" : "disabled")}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to set fan boost");
                Console.ResetColor();
            }
            return;
        }
        
        // No options - show current status
        ShowFanStatus(ec);
        await Task.CompletedTask;
    }
    
    private static void ShowFanStatus(LinuxEcController ec)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║           Fan Status                 ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        
        var (fan1Speed, fan2Speed) = ec.GetFanSpeeds();
        var (fan1Pct, fan2Pct) = ec.GetFanSpeedPercent();
        
        Console.WriteLine($"║  Fan 1 (CPU): {fan1Speed,5} RPM  ({fan1Pct,3}%)  ║");
        Console.WriteLine($"║  Fan 2 (GPU): {fan2Speed,5} RPM  ({fan2Pct,3}%)  ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();
    }
}
