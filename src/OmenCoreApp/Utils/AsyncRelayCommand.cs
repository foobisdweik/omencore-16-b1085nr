using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace OmenCore.Utils
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _executeAsync(parameter);
            }
            catch (Exception ex)
            {
                // Log the error and show user feedback
                // This prevents unhandled exceptions from crashing the application
                var logging = App.Current?.Properties["LoggingService"] as OmenCore.Services.LoggingService;
                logging?.Error($"Command execution failed: {ex.Message}", ex);
                
                // Show error dialog to user on UI thread
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Operation failed: {ex.Message}\n\nCheck logs for details.\n\nTip: Try restarting OmenCore or check Settings for any misconfigurations.",
                        "Operation Failed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }
    }
}
