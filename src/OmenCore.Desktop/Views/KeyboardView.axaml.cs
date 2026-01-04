using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace OmenCore.Desktop.Views;

public partial class KeyboardView : UserControl
{
    public KeyboardView()
    {
        InitializeComponent();
        
        // Wire up slider value changed events
        RedSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
            {
                RedValue.Text = ((int)RedSlider.Value).ToString();
                UpdateColorPreview();
            }
        };
        
        GreenSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
            {
                GreenValue.Text = ((int)GreenSlider.Value).ToString();
                UpdateColorPreview();
            }
        };
        
        BlueSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
            {
                BlueValue.Text = ((int)BlueSlider.Value).ToString();
                UpdateColorPreview();
            }
        };
        
        BrightnessSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
                BrightnessValue.Text = $"{(int)BrightnessSlider.Value}%";
        };
        
        SpeedSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
                SpeedValue.Text = ((int)SpeedSlider.Value).ToString();
        };
        
        PerZoneToggle.IsCheckedChanged += (s, e) =>
        {
            ZoneGrid.IsVisible = PerZoneToggle.IsChecked == true;
        };
    }
    
    private void ColorPreset_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string colorHex)
        {
            var color = Color.Parse(colorHex);
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
        }
    }
    
    private void UpdateColorPreview()
    {
        var color = Color.FromRgb(
            (byte)RedSlider.Value,
            (byte)GreenSlider.Value,
            (byte)BlueSlider.Value);
        ColorPreview.Background = new SolidColorBrush(color);
    }
    
    private void ApplyLighting_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Apply lighting settings via service
        // Get effect type
        string effect = "Static";
        if (EffectBreathing.IsChecked == true) effect = "Breathing";
        else if (EffectCycle.IsChecked == true) effect = "ColorCycle";
        else if (EffectWave.IsChecked == true) effect = "Wave";
        else if (EffectReactive.IsChecked == true) effect = "Reactive";
        else if (EffectOff.IsChecked == true) effect = "Off";
        
        // Get color
        var r = (byte)RedSlider.Value;
        var g = (byte)GreenSlider.Value;
        var b = (byte)BlueSlider.Value;
        
        // Get settings
        var brightness = (int)BrightnessSlider.Value;
        var speed = (int)SpeedSlider.Value;
        
        // TODO: Call keyboard service
        System.Diagnostics.Debug.WriteLine($"Apply: Effect={effect}, Color=({r},{g},{b}), Brightness={brightness}, Speed={speed}");
    }
}
