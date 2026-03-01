using LanRemoteDesktop.Common.Logger; 
using System.Windows;

namespace LanRemoteDesktop.Host.Logging
{
    public sealed class WpfLogger : ILogger
    {
        private readonly MainWindow _window;

        public WpfLogger(MainWindow window)
        {
            _window = window;
        }

        public void Info(string message)
        {
            Application.Current.Dispatcher.Invoke(() => _window.AddLog(message));
        }

        public void Error(string message, Exception? ex = null)
        {
            var full = ex is null ? message : $"{message}\n{ex}";
            Application.Current.Dispatcher.Invoke(() => _window.AddLog(full));
        }
    }
}