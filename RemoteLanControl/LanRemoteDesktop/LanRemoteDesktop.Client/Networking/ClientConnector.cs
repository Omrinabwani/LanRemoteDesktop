using LanRemoteDesktop.Common.Networking;
using LanRemoteDesktop.Common.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LanRemoteDesktop.Client.Networking
{
    public sealed class ClientConnector
    {
        public async Task<TcpClient> ConnectAsync(string ip, int port, CancellationToken ct)
        {
            if (!IPAddress.TryParse(ip, out var address))
                throw new ArgumentException("Invalid ip address..."); 
            var client = new TcpClient();
            await client.ConnectAsync(address, port, ct);
            client.NoDelay = true;
            return client;
        }
    }
}
