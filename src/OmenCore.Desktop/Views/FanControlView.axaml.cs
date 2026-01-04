using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OmenCore.Desktop.Views;

public partial class FanControlView : UserControl
{
    public FanControlView()
    {
        InitializeComponent();
        RefreshData();
    }

    private void Profile_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string profile)
        {
            CurrentProfileText.Text = $"Current: {profile}";
            // TODO: Apply fan profile via hardware service
        }
    }

    private void ManualToggle_Changed(object? sender, RoutedEventArgs e)
    {
        // Manual mode toggled
    }

    private void ApplyManual_Click(object? sender, RoutedEventArgs e)
    {
        var cpuSpeed = (int)CpuFanSlider.Value;
        var gpuSpeed = (int)GpuFanSlider.Value;
        // TODO: Apply manual fan speeds
    }

    private void RefreshData()
    {
        // Placeholder values
        CpuFanSpeed.Text = "3200 RPM";
        GpuFanSpeed.Text = "3800 RPM";
    }
}
