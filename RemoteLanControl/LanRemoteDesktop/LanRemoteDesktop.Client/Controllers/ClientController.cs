using LanRemoteDesktop.Client.Networking;
using LanRemoteDesktop.Common.Networking;
using LanRemoteDesktop.Common.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Client.Controllers
{
    public sealed class ClientController
    {
        private readonly ClientConnector _connector = new ClientConnector();
        private Task? _rxTask;
        private ClientSession? _session;
        public event Action<byte[]>? FrameJpegReceived;
        public async Task<(WelcomePayload Welcome, ClientSession Session)> ConnectAndHandshakeAsync(string ip, int port, ushort viewW, ushort viewH, byte quality, CancellationToken ct)
        {
            ClientSession? session = null;
            try
            {
                var tcpClient = await _connector.ConnectAsync(ip, port, ct);
                var conn = new TcpConnection(tcpClient);
                session = new ClientSession(conn);
                HelloPayload hello = new HelloPayload(ProtocolConstants.ProtocolVersion, 0, viewW, viewH, quality);
                var helloPayload = hello.Serialize();
                await session.Writer.WriteAsync(MessageType.Hello, helloPayload, ct);
                var (type, welcomePayload) = await session.Reader.ReadAsync(ct);
                if (type != MessageType.Welcome)
                    throw new InvalidOperationException($"Expected WELCOME, got {type}.");
                var welcome = WelcomePayload.Deserialize(welcomePayload);
                if (welcome.ProtocolVersion != ProtocolConstants.ProtocolVersion)
                    throw new InvalidOperationException($"Protocol version mismatch. Expected {ProtocolConstants.ProtocolVersion}, got {welcome.ProtocolVersion}.");
                StartStreamPayload startStream = new StartStreamPayload(30);
                var startStreamPayload = startStream.Serialize();
                await session.Writer.WriteAsync(MessageType.StartStream, startStreamPayload, ct);
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
                        // Fire event
                        try
                        {
                            FrameJpegReceived?.Invoke(payload);
                        }
                        catch 
                        {
                            // Log later
                        }
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
            }
        }
        public Task SendMouseAsync(MouseInputPayload payload)
        {
            if (_session == null) return Task.CompletedTask;
            return _session.Writer.WriteAsync(MessageType.InputMouse, payload.Serialize(), CancellationToken.None);
        }
        public Task SendKeyboardAsync(KeyboardInputPayload payload)
        {
            if (_session == null) return Task.CompletedTask;
            return _session.Writer.WriteAsync(MessageType.InputKeyboard, payload.Serialize(), CancellationToken.None);
        }
    }
}
