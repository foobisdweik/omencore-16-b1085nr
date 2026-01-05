using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using OmenCore.ViewModels;

namespace OmenCore.Views
{
    public partial class MainWindow : Window
    {
        private bool _forceClose = false; // Flag for actual shutdown vs hide-to-tray
        
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
            SystemParameters.StaticPropertyChanged += SystemParametersOnStaticPropertyChanged;
            
            // Apply Stay on Top setting from config
            Topmost = App.Configuration.Config.StayOnTop;
        }
        
        /// <summary>
        /// Forces the window to close completely (for app shutdown).
        /// Call this from tray menu "Exit" or app shutdown.
        /// </summary>
        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
            (DataContext as MainViewModel)?.DiscoverCorsairCommand.Execute(null);
            UpdateMaximizedBounds();
            
            // Initialize global hotkeys
            var windowHandle = new WindowInteropHelper(this).Handle;
            (DataContext as MainViewModel)?.InitializeHotkeys(windowHandle);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            _ = sender;
            
            // Check if we should minimize to tray instead of closing
            bool minimizeToTray = App.Configuration.Config.Monitoring?.MinimizeToTrayOnClose ?? true;
            
            if (minimizeToTray && !_forceClose)
            {
                // Cancel the close and hide to tray instead
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                App.Logging.Debug("Window hidden to tray (close cancelled)");
                return;
            }
            
            // Actual close - clean up
            SystemParameters.StaticPropertyChanged -= SystemParametersOnStaticPropertyChanged;
            (DataContext as MainViewModel)?.Dispose();
        }

        private void SystemParametersOnStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemParameters.WorkArea))
            {
                UpdateMaximizedBounds();
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeButtonGlyph();
            if (WindowState == WindowState.Maximized)
            {
                UpdateMaximizedBounds();
            }
            // Note: We no longer hide to tray on minimize - that was causing Issue #20
            // Minimize now properly minimizes to taskbar
        }

        private void UpdateMaximizeButtonGlyph()
        {
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        private void UpdateMaximizedBounds()
        {
            var workArea = SystemParameters.WorkArea;
            MaxHeight = workArea.Height + 12;
            MaxWidth = workArea.Width + 12;
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
            // Minimize to taskbar (normal Windows behavior)
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
            
            // Check if we should minimize to tray or actually close
            bool minimizeToTray = App.Configuration.Config.Monitoring?.MinimizeToTrayOnClose ?? true;
            
            if (minimizeToTray)
            {
                // Hide to tray on close button
                Hide();
                ShowInTaskbar = false;
            }
            else
            {
                // Actually close the application
                App.Current?.Shutdown();
            }
        }
    }
}
