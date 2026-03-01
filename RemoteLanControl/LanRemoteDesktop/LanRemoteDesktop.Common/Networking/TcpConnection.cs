using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Networking
{
    public sealed class TcpConnection : IConnection
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        private int _closed; // 0 = open, 1 = closed/disposed

        public EndPoint? RemoteEndPoint => _client.Client?.RemoteEndPoint;

        // Wrap an already-connected TcpClient (recommended)
        public TcpConnection(TcpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // Ensure the socket is in a reasonable state for our use case
            _client.NoDelay = true; // good for low-latency control msgs; fine for JPEG frames too on LAN

            _stream = _client.GetStream();
        }

        public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
        {
            ThrowIfClosed();

            // NetworkStream.WriteAsync supports CancellationToken
            await _stream.WriteAsync(data, ct).ConfigureAwait(false);

            // Usually not required; NetworkStream flush is basically a no-op on sockets,
            // but leaving it out is fine.
            // await _stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct)
        {
            ThrowIfClosed();

            // Returns 0 on graceful close by remote
            return _stream.ReadAsync(buffer, ct).AsTask();
        }

        public void Close()
        {
            // idempotent close
            if (Interlocked.Exchange(ref _closed, 1) != 0)
                return;

            try { _stream.Close(); } catch { /* ignore */ }
            try { _client.Close(); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            Close();
            // Nothing else to do; Close() shuts down stream + client
        }

        private void ThrowIfClosed()
        {
            if (Volatile.Read(ref _closed) != 0)
                throw new ObjectDisposedException(nameof(TcpConnection));
        }
    }
}