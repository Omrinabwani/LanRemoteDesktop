using LanRemoteDesktop.Client.Controllers;
using LanRemoteDesktop.Common.Protocol;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace LanRemoteDesktop.Client
{
    public partial class MainWindow : Window
    {
        private readonly ClientController _controller = new ClientController();
        private ClientSession? _session;

        private Point _lastMousePos;
        private bool _hasLastMousePos;

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
            // If you implement StopAsync later:
            // await _controller.StopAsync();
            await Task.CompletedTask;
        }

        private async void RemoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoteImage.Focus();

            var payload = new MouseInputPayload(MouseFlags.LeftDown, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private async void RemoteImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoteImage.Focus();

            var payload = new MouseInputPayload(MouseFlags.RightDown, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private async void RemoteImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var payload = new MouseInputPayload(MouseFlags.LeftUp, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private async void RemoteImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var payload = new MouseInputPayload(MouseFlags.RightUp, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private void RemoteImage_MouseEnter(object sender, MouseEventArgs e)
        {
            _hasLastMousePos = false;
        }

        private async void RemoteImage_MouseMove(object sender, MouseEventArgs e)
        {
            // If you want only when dragging, uncomment:
            // if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(RemoteImage);

            if (!_hasLastMousePos)
            {
                _lastMousePos = pos;
                _hasLastMousePos = true;
                return;
            }

            double dxD = pos.X - _lastMousePos.X;
            double dyD = pos.Y - _lastMousePos.Y;
            _lastMousePos = pos;

            // Ignore tiny jitter (touchpad noise)
            if (Math.Abs(dxD) < 1 && Math.Abs(dyD) < 1)
                return;

            int dxI = (int)Math.Round(dxD);
            int dyI = (int)Math.Round(dyD);

            if (dxI > short.MaxValue) dxI = short.MaxValue;
            if (dxI < short.MinValue) dxI = short.MinValue;
            if (dyI > short.MaxValue) dyI = short.MaxValue;
            if (dyI < short.MinValue) dyI = short.MinValue;

            var payload = new MouseInputPayload(MouseFlags.Move, (short)dxI, (short)dyI);

            try
            {
                await _controller.SendMouseAsync(payload);
            }
            catch
            {
                // MVP ignore; later log/disconnect
            }
        }

        private async void RemoteImage_KeyDown(object sender, KeyEventArgs e)
        {
            ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(e.Key);
            var payload = new KeyboardInputPayload(vk, isDown: true);

            await _controller.SendKeyboardAsync(payload);
            e.Handled = true;
        }

        private async void RemoteImage_KeyUp(object sender, KeyEventArgs e)
        {
            ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(e.Key);
            var payload = new KeyboardInputPayload(vk, isDown: false);

            await _controller.SendKeyboardAsync(payload);
            e.Handled = true;
        }
    }
}