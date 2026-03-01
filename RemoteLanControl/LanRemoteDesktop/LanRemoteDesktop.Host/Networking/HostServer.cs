using LanRemoteDesktop.Common.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Host.Networking
{
    public sealed class HostServer : IDisposable
    {
        private TcpListener? _listener;
        private bool _isRunning = false;
        public void Start(int port)
        {
            if (_isRunning)
                throw new InvalidOperationException("Host already started.");
            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }
        public async Task<TcpClient> AcceptClientAsync(CancellationToken ct)
        {
            if (!_isRunning || _listener == null)
                throw new InvalidOperationException("Host not started.");
            var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            client.NoDelay = true;
            return client;
        }
        public void Stop()
        {
            if (!_isRunning || _listener == null)
                return;
            _listener?.Stop();
            _listener = null;
            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
