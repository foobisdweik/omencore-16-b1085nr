using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OmenCore.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem item)
        {
            var tag = item.Tag?.ToString();
            ContentArea.Content = tag switch
            {
                "Dashboard" => new DashboardView(),
                "Fans" => new FanControlView(),
                "Performance" => new PerformanceView(),
                "Keyboard" => new KeyboardView(),
                "Settings" => new SettingsView(),
                _ => new DashboardView()
            };
        }
    }
}
