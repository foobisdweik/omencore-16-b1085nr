using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace OmenCore.Views
{
    /// <summary>
    /// On-Screen Display window that shows mode changes when hotkeys are pressed.
    /// Appears briefly in the corner of the screen and fades out automatically.
    /// </summary>
    public partial class HotkeyOsdWindow : Window
    {
        private readonly DispatcherTimer _dismissTimer;
        private bool _isAnimatingOut;

        public HotkeyOsdWindow()
        {
            InitializeComponent();
            
            // Set up auto-dismiss timer
            _dismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000)
            };
            _dismissTimer.Tick += DismissTimer_Tick;
        }

        /// <summary>
        /// Show the OSD with the specified mode information
        /// </summary>
        public void ShowMode(string category, string modeName, string? hotkeyDescription = null)
        {
            // Cancel any existing animations
            _isAnimatingOut = false;
            _dismissTimer.Stop();
            
            // Update content
            ModeCategory.Text = category;
            ModeName.Text = modeName;
            ModeIcon.Text = GetModeIcon(category, modeName);
            TriggerText.Text = hotkeyDescription ?? "via Hotkey";
            
            // Update accent color based on mode
            UpdateAccentColor(modeName);
            
            // Show the window first (required for accurate size measurement)
            Opacity = 0;
            Show();
            
            // Use Dispatcher to position after layout is complete
            // This ensures ActualWidth/ActualHeight are properly calculated
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                PositionWindow();
                AnimateIn();
                _dismissTimer.Start();
            });
        }

        private void PositionWindow()
        {
            // Get the working area (excludes taskbar)
            var workArea = SystemParameters.WorkArea;
            
            // Update layout to get actual size
            UpdateLayout();
            
            // Get actual dimensions (fallback to reasonable defaults if not yet measured)
            var width = ActualWidth > 0 ? ActualWidth : 300;
            var height = ActualHeight > 0 ? ActualHeight : 100;
            
            // Position in bottom-right corner with padding
            const double padding = 24;
            Left = workArea.Right - width - padding;
            Top = workArea.Bottom - height - padding;
            
            // Ensure window is within screen bounds (safety check)
            if (Left < workArea.Left) Left = workArea.Left + padding;
            if (Top < workArea.Top) Top = workArea.Top + padding;
        }

        private string GetModeIcon(string category, string modeName)
        {
            var lowerMode = modeName.ToLower();
            
            return category.ToLower() switch
            {
                "fan mode" => lowerMode switch
                {
                    "performance" or "max" or "turbo" => "🔥",
                    "quiet" or "silent" => "🤫",
                    "balanced" or "auto" => "⚖️",
                    _ => "🌀"
                },
                "performance" => lowerMode switch
                {
                    "performance" => "⚡",
                    "balanced" => "⚖️",
                    "quiet" or "power saver" => "🔋",
                    _ => "💻"
                },
                "boost" => "🚀",
                _ => "⚙️"
            };
        }

        private void UpdateAccentColor(string modeName)
        {
            var color = modeName.ToLower() switch
            {
                "performance" or "boost" or "max" or "turbo" => Color.FromRgb(0xFF, 0x6B, 0x35), // Orange
                "quiet" or "silent" => Color.FromRgb(0x4E, 0xCD, 0xC4), // Teal
                "balanced" or "auto" => Color.FromRgb(0x00, 0xD4, 0xFF), // Cyan
                _ => Color.FromRgb(0x00, 0xD4, 0xFF) // Default cyan
            };
            
            OsdBorder.BorderBrush = new SolidColorBrush(color);
        }

        private void AnimateIn()
        {
            // Fade in and slide up
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            var slideIn = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            var transform = new TranslateTransform(0, 20);
            OsdBorder.RenderTransform = transform;
            
            BeginAnimation(OpacityProperty, fadeIn);
            transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        }

        private void AnimateOut(Action? onComplete = null)
        {
            if (_isAnimatingOut) return;
            _isAnimatingOut = true;
            
            // Fade out and slide down
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (s, e) =>
            {
                Hide();
                _isAnimatingOut = false;
                onComplete?.Invoke();
            };
            
            var transform = OsdBorder.RenderTransform as TranslateTransform ?? new TranslateTransform();
            var slideOut = new DoubleAnimation(0, 20, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            OsdBorder.RenderTransform = transform;
            BeginAnimation(OpacityProperty, fadeOut);
            transform.BeginAnimation(TranslateTransform.YProperty, slideOut);
        }

        private void DismissTimer_Tick(object? sender, EventArgs e)
        {
            _dismissTimer.Stop();
            AnimateOut();
        }

        /// <summary>
        /// Dismiss the OSD immediately (with animation)
        /// </summary>
        public void Dismiss()
        {
            _dismissTimer.Stop();
            AnimateOut();
        }
    }
}
