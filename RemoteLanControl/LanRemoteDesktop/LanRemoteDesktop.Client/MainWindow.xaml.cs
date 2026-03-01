using LanRemoteDesktop.Client.Controllers;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LanRemoteDesktop.Client
{
    public partial class MainWindow : Window
    {
        private readonly ClientController _controller = new ClientController();
        private ClientSession? _session;

        public MainWindow()
        {
            InitializeComponent();

            _controller.FrameJpegReceived += OnFrameJpegReceived;

            _ = Task.Run(async () =>
            {
                try
                {
                    var (welcome, session) = await _controller.ConnectAndHandshakeAsync(
                        "127.0.0.1",
                        5000,
                        1280,
                        720,
                        70,
                        CancellationToken.None);

                    _session = session;
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(ex.ToString(), "Client error");
                    });
                }
            });
        }

        private void OnFrameJpegReceived(byte[] jpegBytes)
        {
            BitmapImage image = new();

            using (var ms = new MemoryStream(jpegBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
            }

            Dispatcher.InvokeAsync(() =>
            {
                RemoteImage.Source = image;
            });
        }
        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            // await _controller.StopAsync(); 
        }
    }
}