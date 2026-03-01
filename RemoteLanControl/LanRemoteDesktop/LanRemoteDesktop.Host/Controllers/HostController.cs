using LanRemoteDesktop.Common.Networking;
using LanRemoteDesktop.Common.Protocol;
using LanRemoteDesktop.Host.Networking;
using System.Windows;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using LanRemoteDesktop.Common.Logger;
namespace LanRemoteDesktop.Host.Controllers
{
    public sealed class HostController
    {
        private CancellationTokenSource? _streamCts;
        private Task? _streamTask;
        private readonly HostServer _server = new HostServer();
        private readonly ILogger _log;
        public HostController(ILogger log) 
        {
            _log = log;
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
                var (type, helloPayload) = await reader.ReadAsync(ct);
                if (type != MessageType.Hello)
                    throw new InvalidOperationException($"Expected HELLO, got {type}.");
                var hello = HelloPayload.Deserialize(helloPayload);
                if (hello.ProtocolVersion != ProtocolConstants.ProtocolVersion)
                    throw new InvalidOperationException($"Protocol version mismatch. Expected {ProtocolConstants.ProtocolVersion}, got {hello.ProtocolVersion}.");
                ushort width = (ushort)SystemParameters.PrimaryScreenWidth;
                ushort height = (ushort)SystemParameters.PrimaryScreenHeight;
                byte quality = hello.JpegQuality;
                var welcome = new WelcomePayload(ProtocolConstants.ProtocolVersion,0, width, height, quality);
                var payloadBytes = welcome.Serialize();
                await writer.WriteAsync(MessageType.Welcome, payloadBytes, ct);
                while (!ct.IsCancellationRequested)
                {
                    var (msgType, msgPayload) = await reader.ReadAsync(ct);

                    switch (msgType)
                    {
                        case MessageType.StartStream:
                            _log.Info("StartStream");
                            if (_streamTask == null || _streamTask.IsCompleted)
                            {
                                var start = StartStreamPayload.Deserialize(msgPayload);
                                byte fps = start.Fps;
                                _streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                _streamTask = StreamLoopAsync(writer, quality, fps, _streamCts.Token);
                                _log.Info($"Streaming started at {fps} FPS");
                            }
                            else
                            {
                                StopStream();
                                var start = StartStreamPayload.Deserialize(msgPayload);
                                byte fps = start.Fps;
                                _streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                _streamTask = StreamLoopAsync(writer, quality, fps, _streamCts.Token);
                                _log.Info($"Streaming started at {fps} FPS");
                            }

                            break;

                        case MessageType.StopStream:
                            _log.Info("StopStream");
                            StopStream();
                            break;

                        default:
                            _log.Info($"Ignoring message: {msgType}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("An existing connection was forcibly closed by the remote host"))
                {
                    StopStream();
                    _log.Info("Client disconnected.");
                }
                else
                {
                    StopStream();
                    _log.Error("Host error", ex);
                }   
            }
            finally
            {
                _server.Stop(); 
            }
        }
        private void StopStream()
        {
            if (_streamCts != null)
            {
                _streamTask = null;
                _streamCts?.Cancel();
                _streamCts.Dispose();
                _streamCts = null;
            }
        }
        private async Task StreamLoopAsync(FramedMessageWriter writer, byte quality, byte fps, CancellationToken ct)
        {
            int delayMs = 1000 / fps;
            while (!ct.IsCancellationRequested)
            {
                var jpegBytes = CapturePrimaryScreenJpeg(quality);
                await writer.WriteAsync(MessageType.FrameJpeg, jpegBytes, ct);
                await Task.Delay(delayMs, ct);
            }
        }
        private byte[] CapturePrimaryScreenJpeg(byte quality)
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
                // Copy screen into memory bitmap
                if (!BitBlt(hMemDC, 0, 0, width, height, hDesktopDC, 0, 0, 0x00CC0020 | 0x40000000))
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
