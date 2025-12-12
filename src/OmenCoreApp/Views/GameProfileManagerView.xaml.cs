using System.Windows;
using System.Windows.Input;

namespace OmenCore.Views
{
    /// <summary>
    /// Game Profile Manager window.
    /// </summary>
    public partial class GameProfileManagerView : Window
    {
        public GameProfileManagerView()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
