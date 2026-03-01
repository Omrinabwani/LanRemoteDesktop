using LanRemoteDesktop.Common.Protocol;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Networking
{
    public sealed class FramedMessageWriter
    {
        private readonly IConnection _conn;
        public FramedMessageWriter(IConnection conn)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));
            _conn = conn;
        }
        public async Task WriteAsync(MessageType type, ReadOnlyMemory<byte> payload, CancellationToken ct)
        {
            if (payload.Length > ProtocolConstants.MaxPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(payload));
            uint totalLength = (uint)(ProtocolConstants.HeaderSize + payload.Length);
            Byte[] data = new byte[totalLength];
            data[0] = (byte)type;
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(1, 4), (uint)payload.Length);
            payload.CopyTo(data.AsMemory(5));
            await _conn.SendAsync(data, ct);
        }
    }
}
