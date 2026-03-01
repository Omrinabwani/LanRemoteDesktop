using LanRemoteDesktop.Common.Networking;

namespace LanRemoteDesktop.Client.Controllers
{
    public sealed class ClientSession : IDisposable
    {
        public TcpConnection Connection { get; }
        public FramedMessageReader Reader { get; }
        public FramedMessageWriter Writer { get; }

        public ClientSession(TcpConnection conn)
        {
            Connection = conn;
            Reader = new FramedMessageReader(conn);
            Writer = new FramedMessageWriter(conn);
        }

        public void Dispose() => Connection.Dispose();
    }
}