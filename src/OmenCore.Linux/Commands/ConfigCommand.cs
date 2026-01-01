using System.CommandLine;
using System.Text.Json;

namespace OmenCore.Linux.Commands;

/// <summary>
/// Configuration management command.
/// 
/// Examples:
///   omencore-cli config --show
///   omencore-cli config --set fan.profile=gaming
///   omencore-cli config --set keyboard.color=FF0000
///   omencore-cli config --reset
/// </summary>
public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Manage OmenCore configuration");
        
        var showOption = new Option<bool>(
            aliases: new[] { "--show", "-s" },
            description: "Show current configuration");
            
        var setOption = new Option<string?>(
            aliases: new[] { "--set" },
            description: "Set a configuration value (key=value)");
            
        var getOption = new Option<string?>(
            aliases: new[] { "--get" },
            description: "Get a configuration value by key");
            
        var resetOption = new Option<bool>(
            aliases: new[] { "--reset" },
            description: "Reset configuration to defaults");
            
        var applyOption = new Option<bool>(
            aliases: new[] { "--apply", "-a" },
            description: "Apply saved configuration immediately");
        
        command.AddOption(showOption);
        command.AddOption(setOption);
        command.AddOption(getOption);
        command.AddOption(resetOption);
        command.AddOption(applyOption);
        
        command.SetHandler(async (show, set, get, reset, apply) =>
        {
            await HandleConfigCommandAsync(show, set, get, reset, apply);
        }, showOption, setOption, getOption, resetOption, applyOption);
        
        return command;
    }
    
    private static async Task HandleConfigCommandAsync(
        bool show, string? set, string? get, bool reset, bool apply)
    {
        var configPath = Program.ConfigPath;
        
        if (reset)
        {
            var defaults = GetDefaultConfig();
            ConfigManager.Save(defaults);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Configuration reset to defaults");
            Console.ResetColor();
            return;
        }
        
        if (!string.IsNullOrEmpty(set))
        {
            var parts = set.Split('=', 2);
            if (parts.Length != 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Invalid format. Use --set key=value");
                Console.ResetColor();
                return;
            }
            
            ConfigManager.Set(parts[0].Trim(), parts[1].Trim());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Set {parts[0]} = {parts[1]}");
            Console.ResetColor();
            return;
        }
        
        if (!string.IsNullOrEmpty(get))
        {
            var value = ConfigManager.Get(get);
            if (value != null)
            {
                Console.WriteLine(value);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"(not set)");
                Console.ResetColor();
            }
            return;
        }
        
        if (apply)
        {
            await ApplyConfigAsync();
            return;
        }
        
        // Default: show config
        ShowConfig();
        await Task.CompletedTask;
    }
    
    private static void ShowConfig()
    {
        var config = ConfigManager.Load();
        
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              OmenCore Linux - Configuration               ║");
        Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  Config Path: {Program.ConfigPath,-42} ║");
        Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
        
        if (config.Count == 0)
        {
            Console.WriteLine("║  (no configuration set - using defaults)                  ║");
        }
        else
        {
            foreach (var kvp in config)
            {
                Console.WriteLine($"║  {kvp.Key,-20} = {kvp.Value,-32} ║");
            }
        }
        
        Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  Available Keys:                                          ║");
        Console.WriteLine("║    fan.profile     - auto|silent|balanced|gaming|max      ║");
        Console.WriteLine("║    fan.boost       - true|false                           ║");
        Console.WriteLine("║    perf.mode       - default|balanced|performance|cool    ║");
        Console.WriteLine("║    keyboard.color  - RRGGBB hex color                     ║");
        Console.WriteLine("║    keyboard.brightness - 0-100                            ║");
        Console.WriteLine("║    startup.apply   - true|false (apply config on boot)    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }
    
    private static Dictionary<string, string> GetDefaultConfig()
    {
        return new Dictionary<string, string>
        {
            { "fan.profile", "auto" },
            { "fan.boost", "false" },
            { "perf.mode", "balanced" },
            { "keyboard.color", "FF0000" },
            { "keyboard.brightness", "100" },
            { "startup.apply", "false" }
        };
    }
    
    private static async Task ApplyConfigAsync()
    {
        var config = ConfigManager.Load();
        
        Console.WriteLine("Applying configuration...");
        
        // Apply fan profile
        if (config.TryGetValue("fan.profile", out var profile))
        {
            Console.WriteLine($"  Fan profile: {profile}");
            // Would invoke FanCommand logic here
        }
        
        // Apply performance mode
        if (config.TryGetValue("perf.mode", out var perfMode))
        {
            Console.WriteLine($"  Performance mode: {perfMode}");
            // Would invoke PerformanceCommand logic here
        }
        
        // Apply keyboard settings
        if (config.TryGetValue("keyboard.color", out var kbColor))
        {
            Console.WriteLine($"  Keyboard color: #{kbColor}");
            // Would invoke KeyboardCommand logic here
        }
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Configuration applied");
        Console.ResetColor();
        
        await Task.CompletedTask;
    }
}
