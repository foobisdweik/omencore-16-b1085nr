using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using OmenCore.ViewModels;

namespace OmenCore.Views
{
    /// <summary>
    /// Interaction logic for SystemOptimizerView.xaml
    /// </summary>
    public partial class SystemOptimizerView : UserControl
    {
        public SystemOptimizerView()
        {
            InitializeComponent();
        }

        private void OnToggleClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.Tag is OptimizationItem item)
            {
                // The binding will update IsEnabled, but we need to trigger the async action
                item.Toggle();
            }
        }
    }
}
