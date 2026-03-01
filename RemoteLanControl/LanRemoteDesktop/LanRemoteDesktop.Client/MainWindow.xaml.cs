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
            try
            {
                await _controller.StopStreamAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Stop error");
            }
        }

        private async void RemoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoteImage.Focus();

            var payload = new MouseAbsInputPayload(MouseFlags.LeftDown, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private async void RemoteImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoteImage.Focus();

            var payload = new MouseAbsInputPayload(MouseFlags.RightDown, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private async void RemoteImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var payload = new MouseAbsInputPayload(MouseFlags.LeftUp, 0, 0);
            await _controller.SendMouseAsync(payload);
        }

        private async void RemoteImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var payload = new MouseAbsInputPayload(MouseFlags.RightUp, 0, 0);
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
            double uiW = RemoteImage.ActualWidth;
            double uiH = RemoteImage.ActualHeight;

            ushort x = (ushort)Math.Clamp((int)Math.Round(pos.X * (1280.0 / uiW)), 0, 1279); // Resize so it fits the screen and not ui
            ushort y = (ushort)Math.Clamp((int)Math.Round(pos.Y * (720.0 / uiH)), 0, 719); // Resize so it fits the screen and not the ui
            if (!_hasLastMousePos)
            {
                _lastMousePos = pos;
                _hasLastMousePos = true;
                return;
            }

            
            var payload = new MouseAbsInputPayload(MouseFlags.Move, x, y);

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
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(key);
            var payload = new KeyboardInputPayload(vk, true);

            await _controller.SendKeyboardAsync(payload);
            e.Handled = true;
        }

        private async void RemoteImage_KeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(key);
            var payload = new KeyboardInputPayload(vk, false);

            await _controller.SendKeyboardAsync(payload);
            e.Handled = true;
        }
    }
}