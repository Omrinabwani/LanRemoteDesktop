using LanRemoteDesktop.Host.Logging;
using System.Windows;
using LanRemoteDesktop.Common.Logger;

namespace LanRemoteDesktop.Host
{
    public partial class App : Application
    {
        private CancellationTokenSource? _cts;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _cts = new CancellationTokenSource();

            // Show UI
            var window = new MainWindow();
            MainWindow = window;
            window.Show();

            // Create logger that logs into the UI
            ILogger logger = new WpfLogger(window);

            // Start host in background so UI thread is free
            _ = Task.Run(async () =>
            {
                try
                {
                    var controller = new Controllers.HostController(logger);
                    await controller.RunAsync(port: 5000, _cts.Token);
                }
                catch (Exception ex)
                {
                    logger.Error("Host crashed", ex);
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _cts?.Cancel();
            base.OnExit(e);
        }
    }
}