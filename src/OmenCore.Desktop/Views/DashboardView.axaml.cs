using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Runtime.InteropServices;

namespace OmenCore.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        
        // Set platform info
        PlatformText.Text = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
            ? $"Linux ({RuntimeInformation.OSDescription})" 
            : $"Windows ({Environment.OSVersion.Version})";
        
        // TODO: Connect to hardware service and update values
        RefreshData();
    }

    private void PerformanceMode_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string mode)
        {
            CurrentModeText.Text = $"Current: {mode}";
            // TODO: Apply performance mode via hardware service
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        RefreshData();
    }

    private void RefreshData()
    {
        // Placeholder - would connect to LinuxHardwareService
        CpuTempText.Text = "65°C";
        GpuTempText.Text = "72°C";
        FanSpeedText.Text = "3500 RPM";
        PowerText.Text = "45W";
        ModelText.Text = "HP OMEN 16";
        CpuNameText.Text = "AMD Ryzen 7 6800H";
        GpuNameText.Text = "NVIDIA RTX 3070 Ti";
    }
}
