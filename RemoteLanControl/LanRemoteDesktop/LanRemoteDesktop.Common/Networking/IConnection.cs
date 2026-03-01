using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Networking
{
     public interface IConnection : IDisposable
    {
        Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct);
        Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct);
        void Close();
        EndPoint? RemoteEndPoint { get; }
    }
}
