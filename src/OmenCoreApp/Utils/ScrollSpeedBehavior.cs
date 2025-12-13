using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OmenCore.Utils
{
    /// <summary>
    /// Attached behavior to increase ScrollViewer scroll speed.
    /// Usage: utils:ScrollSpeedBehavior.ScrollSpeedMultiplier="3.0"
    /// </summary>
    public static class ScrollSpeedBehavior
    {
        public static readonly DependencyProperty ScrollSpeedMultiplierProperty =
            DependencyProperty.RegisterAttached(
                "ScrollSpeedMultiplier",
                typeof(double),
                typeof(ScrollSpeedBehavior),
                new PropertyMetadata(1.0, OnScrollSpeedMultiplierChanged));

        public static double GetScrollSpeedMultiplier(DependencyObject obj)
            => (double)obj.GetValue(ScrollSpeedMultiplierProperty);

        public static void SetScrollSpeedMultiplier(DependencyObject obj, double value)
            => obj.SetValue(ScrollSpeedMultiplierProperty, value);

        private static void OnScrollSpeedMultiplierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
                
                if ((double)e.NewValue > 1.0)
                {
                    scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
                }
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && !e.Handled)
            {
                var multiplier = GetScrollSpeedMultiplier(scrollViewer);
                
                // Calculate scroll amount (default is ~48 pixels per notch, we multiply it)
                var scrollAmount = e.Delta * multiplier;
                
                // Scroll by the calculated amount
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollAmount);
                
                e.Handled = true;
            }
        }
    }
}
