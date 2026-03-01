using LanRemoteDesktop.Common.Logger;
using LanRemoteDesktop.Common.Networking;
using LanRemoteDesktop.Common.Protocol;
using LanRemoteDesktop.Host.Input;
using LanRemoteDesktop.Host.Networking;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LanRemoteDesktop.Host.Controllers
{
    public sealed class HostController
    {
        private CancellationTokenSource? _streamCts;
        private Task? _streamTask;

        private readonly HostServer _server = new HostServer();
        private readonly ILogger _log;
        private readonly InputInjector _inputInjector;

        public HostController(ILogger log)
        {
            _log = log;
            _inputInjector = new InputInjector();
        }

        public async Task RunAsync(int port, CancellationToken ct)
        {
            try
            {
                _server.Start(port);

                using var tcp = await _server.AcceptClientAsync(ct);
                using var conn = new TcpConnection(tcp);

                _log.Info($"Client connected: {conn.RemoteEndPoint}");

                var reader = new FramedMessageReader(conn);
                var writer = new FramedMessageWriter(conn);

                // ---- Handshake ----
                var (type, helloPayloadBytes) = await reader.ReadAsync(ct);
                if (type != MessageType.Hello)
                    throw new InvalidOperationException($"Expected HELLO, got {type}.");

                var hello = HelloPayload.Deserialize(helloPayloadBytes);

                if (hello.ProtocolVersion != ProtocolConstants.ProtocolVersion)
                    throw new InvalidOperationException(
                        $"Protocol version mismatch. Expected {ProtocolConstants.ProtocolVersion}, got {hello.ProtocolVersion}.");

                ushort width = (ushort)SystemParameters.PrimaryScreenWidth;
                ushort height = (ushort)SystemParameters.PrimaryScreenHeight;
                byte quality = hello.JpegQuality;

                var welcome = new WelcomePayload(
                    ProtocolConstants.ProtocolVersion,
                    flags: 0,
                    screenWidth: width,
                    screenHeight: height,
                    jpegQuality: quality);

                await writer.WriteAsync(MessageType.Welcome, welcome.Serialize(), ct);

                // ---- Main loop ----
                while (!ct.IsCancellationRequested)
                {
                    var (msgType, msgPayload) = await reader.ReadAsync(ct);

                    switch (msgType)
                    {
                        case MessageType.StartStream:
                            {
                                StopStream();
                                _log.Info("StartStream");
                                var start = StartStreamPayload.Deserialize(msgPayload);
                                byte fps = start.Fps;

                                StartStream(writer, quality, fps, ct);

                                _log.Info($"Streaming started at {fps} FPS");
                                break;
                            }

                        case MessageType.StopStream:
                            _log.Info("StopStream");
                            StopStream();
                            break;

                        case MessageType.InputMouse:
                            _log.Info("InputMouse");
                            _inputInjector.InjectMouse(MouseAbsInputPayload.Deserialize(msgPayload), width, height, hello.ClientViewWidth, hello.ClientViewHeight);
                            break;

                        case MessageType.InputKeyboard:
                            _log.Info("InputKeyboard");
                            _inputInjector.InjectKeyboard(KeyboardInputPayload.Deserialize(msgPayload));
                            break;

                        default:
                            _log.Info($"Ignoring message: {msgType}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                StopStream();

                // A bit nicer than string-contains, but keep your original behavior if you prefer.
                if (ex is IOException || ex is SocketException || ex.Message.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase))
                {
                    _log.Info("Client disconnected.");
                }
                else
                {
                    _log.Error("Host error", ex);
                }
            }
            finally
            {
                _server.Stop();
            }
        }

        private void StartStream(FramedMessageWriter writer, byte quality, byte fps, CancellationToken ct)
        {
            StopStream(); // If already streaming, restart cleanly.

            _streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _streamTask = StreamLoopAsync(writer, quality, fps, _streamCts.Token);
        }

        private void StopStream()
        {
            var cts = _streamCts;
            if (cts == null) return;

            _streamCts = null;

            try { cts.Cancel(); }
            finally { cts.Dispose(); }

            _streamTask = null;
            _log.Info("Streaming stopped.");
        }

        private async Task StreamLoopAsync(FramedMessageWriter writer, byte quality, byte fps, CancellationToken ct)
        {
            if (fps == 0)
                return;

            int delayMs = 1000 / fps;

            while (!ct.IsCancellationRequested)
            {
                byte[] jpegBytes = CapturePrimaryScreenJpeg(quality);

                await writer.WriteAsync(MessageType.FrameJpeg, jpegBytes, ct);
                await Task.Delay(delayMs, ct);
            }
        }

        private static byte[] CapturePrimaryScreenJpeg(byte quality)
        {
            int width = (int)SystemParameters.PrimaryScreenWidth;
            int height = (int)SystemParameters.PrimaryScreenHeight;

            IntPtr hDesktopWnd = GetDesktopWindow();
            IntPtr hDesktopDC = GetWindowDC(hDesktopWnd);
            IntPtr hMemDC = CreateCompatibleDC(hDesktopDC);
            IntPtr hBitmap = CreateCompatibleBitmap(hDesktopDC, width, height);
            IntPtr hOld = SelectObject(hMemDC, hBitmap);

            try
            {
                const int SRCCOPY = 0x00CC0020;
                const int CAPTUREBLT = 0x40000000;

                if (!BitBlt(hMemDC, 0, 0, width, height, hDesktopDC, 0, 0, SRCCOPY | CAPTUREBLT))
                    throw new InvalidOperationException("BitBlt failed.");

                var bmpSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bmpSource.Freeze();

                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(bmpSource));

                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            finally
            {
                SelectObject(hMemDC, hOld);
                DeleteObject(hBitmap);
                DeleteDC(hMemDC);
                ReleaseDC(hDesktopWnd, hDesktopDC);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(
            IntPtr hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            int dwRop);
    }
}