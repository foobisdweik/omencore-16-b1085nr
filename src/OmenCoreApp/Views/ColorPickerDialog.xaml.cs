using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OmenCore.Views
{
    public partial class ColorPickerDialog : Window
    {
        public string SelectedHexColor { get; private set; } = "#E6002E";
        public bool DialogResultOk { get; private set; } = false;

        public ColorPickerDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the zone information displayed in the dialog header.
        /// </summary>
        /// <param name="zoneNumber">Zone number (1-4)</param>
        /// <param name="zoneName">Zone name (e.g., "Left", "Middle-L")</param>
        public void SetZoneInfo(int zoneNumber, string zoneName)
        {
            SubtitleText.Text = $"Zone {zoneNumber} - {zoneName}";
        }

        /// <summary>
        /// Sets the initial color for the picker.
        /// </summary>
        /// <param name="hexColor">Hex color string (e.g., "#FF0000")</param>
        public void SetInitialColor(string hexColor)
        {
            SelectedHexColor = hexColor;
            ColorPicker.HexValue = hexColor;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedHexColor = ColorPicker.HexValue;
            DialogResultOk = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResultOk = false;
            DialogResult = false;
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
            else if (e.Key == Key.Enter)
            {
                ApplyButton_Click(sender, e);
            }
        }
    }
}
