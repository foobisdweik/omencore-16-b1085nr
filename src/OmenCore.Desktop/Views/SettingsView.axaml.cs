using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OmenCore.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        // TODO: Load settings from config file
        // For now, use defaults
        
        // Update config path based on platform
        var configPath = Environment.OSVersion.Platform == PlatformID.Unix
            ? "~/.config/omencore"
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OmenCore";
        ConfigPathText.Text = configPath;
        
        // Update version
        VersionText.Text = "Version 2.1.1-beta";
    }
    
    private void AccentColor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string colorHex)
        {
            // TODO: Apply accent color to theme
            System.Diagnostics.Debug.WriteLine($"Accent color selected: {colorHex}");
        }
    }
    
    private void ReinstallDriver_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Trigger driver reinstallation
        System.Diagnostics.Debug.WriteLine("Reinstall driver clicked");
    }
    
    private void OpenConfigFolder_Click(object? sender, RoutedEventArgs e)
    {
        // Open config folder in file manager
        var configPath = Environment.OSVersion.Platform == PlatformID.Unix
            ? Environment.GetEnvironmentVariable("HOME") + "/.config/omencore"
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OmenCore";
        
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open config folder: {ex.Message}");
        }
    }
    
    private void OpenGitHub_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/theantipopau/omencore");
    }
    
    private void OpenIssues_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/theantipopau/omencore/issues");
    }
    
    private void CheckUpdates_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Check for updates
        System.Diagnostics.Debug.WriteLine("Check updates clicked");
    }
    
    private void ResetSettings_Click(object? sender, RoutedEventArgs e)
    {
        // Reset all toggles and combos to defaults
        StartWithSystemToggle.IsChecked = false;
        StartMinimizedToggle.IsChecked = false;
        CloseToTrayToggle.IsChecked = true;
        CheckUpdatesToggle.IsChecked = true;
        PollingIntervalCombo.SelectedIndex = 1;
        ApplyFanOnStartToggle.IsChecked = true;
        ApplyLightingOnStartToggle.IsChecked = true;
        ThemeCombo.SelectedIndex = 0;
        DebugLoggingToggle.IsChecked = false;
    }
    
    private void SaveSettings_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Save settings to config file
        System.Diagnostics.Debug.WriteLine("Save settings clicked");
        
        // Collect current settings
        var settings = new
        {
            StartWithSystem = StartWithSystemToggle.IsChecked,
            StartMinimized = StartMinimizedToggle.IsChecked,
            CloseToTray = CloseToTrayToggle.IsChecked,
            CheckUpdates = CheckUpdatesToggle.IsChecked,
            PollingInterval = PollingIntervalCombo.SelectedIndex,
            ApplyFanOnStart = ApplyFanOnStartToggle.IsChecked,
            ApplyLightingOnStart = ApplyLightingOnStartToggle.IsChecked,
            Theme = ThemeCombo.SelectedIndex,
            DebugLogging = DebugLoggingToggle.IsChecked
        };
        
        System.Diagnostics.Debug.WriteLine($"Settings: {settings}");
    }
    
    private static void OpenUrl(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }
}
