using LanRemoteDesktop.Client.Networking;
using LanRemoteDesktop.Common.Networking;
using LanRemoteDesktop.Common.Protocol;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Client.Controllers
{
    public sealed class ClientController
    {
        private readonly ClientConnector _connector = new ClientConnector();

        private Task? _rxTask;
        private ClientSession? _session;

        public event Action<byte[]>? FrameJpegReceived;

        public async Task<(WelcomePayload Welcome, ClientSession Session)> ConnectAndHandshakeAsync(
            string ip,
            int port,
            ushort viewW,
            ushort viewH,
            byte quality,
            CancellationToken ct)
        {
            ClientSession? session = null;

            try
            {
                var tcpClient = await _connector.ConnectAsync(ip, port, ct);
                var conn = new TcpConnection(tcpClient);

                session = new ClientSession(conn);

                // ---- HELLO ----
                HelloPayload hello = new HelloPayload(
                    ProtocolConstants.ProtocolVersion,
                    flags: 0,
                    clientViewWidth: viewW,
                    clientViewHeight: viewH,
                    jpegQuality: quality);

                await session.Writer.WriteAsync(MessageType.Hello, hello.Serialize(), ct);

                // ---- WELCOME ----
                var (type, welcomePayloadBytes) = await session.Reader.ReadAsync(ct);
                if (type != MessageType.Welcome)
                    throw new InvalidOperationException($"Expected WELCOME, got {type}.");

                var welcome = WelcomePayload.Deserialize(welcomePayloadBytes);

                if (welcome.ProtocolVersion != ProtocolConstants.ProtocolVersion)
                    throw new InvalidOperationException(
                        $"Protocol version mismatch. Expected {ProtocolConstants.ProtocolVersion}, got {welcome.ProtocolVersion}.");

                // ---- START STREAM ----
                StartStreamPayload startStream = new StartStreamPayload(30);
                await session.Writer.WriteAsync(MessageType.StartStream, startStream.Serialize(), ct);

                _session = session;
                _rxTask = Task.Run(() => ReceiveLoopAsync(session, ct));

                return (welcome, session);
            }
            catch
            {
                session?.Dispose();
                throw;
            }
        }

        private async Task ReceiveLoopAsync(ClientSession session, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var (type, payload) = await session.Reader.ReadAsync(ct);

                    if (type == MessageType.FrameJpeg)
                    {
                        try { FrameJpegReceived?.Invoke(payload); }
                        catch { /* ignore for MVP */ }
                    }
                    else
                    {
                        // ignore other types for now
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Connection closed"))
                {
                    _session = null;
                    break;
                }
                catch (IOException)
                {
                    _session = null;
                    break;
                }
                catch (SocketException)
                {
                    _session = null;
                    break;
                }
            }
        }
        public Task StopStreamAsync(CancellationToken ct = default)
        {
            if (_session == null) return Task.CompletedTask;
            return _session.Writer.WriteAsync(MessageType.StopStream, Array.Empty<byte>(), ct);
        }

        public Task SendMouseAsync(MouseAbsInputPayload payload)
        {
            if (_session == null)
                return Task.CompletedTask;

            return _session.Writer.WriteAsync(
                MessageType.InputMouse,
                payload.Serialize(),
                CancellationToken.None);
        }

        public Task SendKeyboardAsync(KeyboardInputPayload payload)
        {
            if (_session == null)
                return Task.CompletedTask;

            return _session.Writer.WriteAsync(
                MessageType.InputKeyboard,
                payload.Serialize(),
                CancellationToken.None);
        }
    }
}