using System.CommandLine;
using System.Text.Json;
using OmenCore.Linux.Commands;

namespace OmenCore.Linux;

/// <summary>
/// OmenCore Linux CLI - Command-line utility for controlling HP OMEN laptops.
/// 
/// Usage:
///   omencore-cli fan --profile auto|silent|gaming|max
///   omencore-cli fan --speed 50%
///   omencore-cli fan --curve "40:20,50:30,60:50,80:80,90:100"
///   omencore-cli perf --mode balanced|performance
///   omencore-cli keyboard --color FF0000
///   omencore-cli keyboard --zone 0 --color 00FF00
///   omencore-cli status [--json]
///   omencore-cli monitor [--interval 1000]
///   omencore-cli config --show|--set key=value
///   omencore-cli daemon --start|--stop|--status
/// 
/// Requirements:
///   - Linux kernel with ec_sys module (write_support=1)
///   - HP WMI module for keyboard lighting
///   - Root privileges for EC access
/// </summary>
class Program
{
    public static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".config", "omencore", "config.json");

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("OmenCore Linux CLI - HP OMEN laptop control utility v2.0.0");
        
        // Add commands
        rootCommand.AddCommand(FanCommand.Create());
        rootCommand.AddCommand(PerformanceCommand.Create());
        rootCommand.AddCommand(KeyboardCommand.Create());
        rootCommand.AddCommand(StatusCommand.Create());
        rootCommand.AddCommand(MonitorCommand.Create());
        rootCommand.AddCommand(ConfigCommand.Create());
        rootCommand.AddCommand(DaemonCommand.Create());
        
        // Add global options
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose output");
        rootCommand.AddGlobalOption(verboseOption);
        
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json", "-j" },
            description: "Output in JSON format for scripting");
        rootCommand.AddGlobalOption(jsonOption);
        
        return await rootCommand.InvokeAsync(args);
    }
}

/// <summary>
/// Configuration file management.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigDir = Path.GetDirectoryName(Program.ConfigPath)!;
    
    public static Dictionary<string, string> Load()
    {
        try
        {
            if (File.Exists(Program.ConfigPath))
            {
                var json = File.ReadAllText(Program.ConfigPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch { }
        return new();
    }
    
    public static void Save(Dictionary<string, string> config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Program.ConfigPath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save config: {ex.Message}");
        }
    }
    
    public static string? Get(string key) => Load().TryGetValue(key, out var val) ? val : null;
    
    public static void Set(string key, string value)
    {
        var config = Load();
        config[key] = value;
        Save(config);
    }
}
