using LanRemoteDesktop.Common.Protocol;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Networking
{
    public class FramedMessageReader
    {
        private readonly IConnection _conn;
        public FramedMessageReader(IConnection conn)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));
            _conn = conn;
        }
        public async Task<(MessageType Type, byte[] Payload)> ReadAsync(CancellationToken ct)
        {
            byte[] header = new byte[ProtocolConstants.HeaderSize];
            await ReadExactlyAsync(header, ct);
            MessageType type = (MessageType)header[0];
            uint payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(1, 4));
            if (payloadLength < 0 || payloadLength > ProtocolConstants.MaxPayloadBytes)
                throw new InvalidOperationException($"Invalid payload length: {payloadLength}");
            byte[] payload;
            if (payloadLength == 0)
                payload = Array.Empty<byte>();
            else
                payload = new byte[payloadLength];

            if (payloadLength > 0)
                await ReadExactlyAsync(payload, ct).ConfigureAwait(false);

            return (type, payload);

        }
        private async Task ReadExactlyAsync(byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            int read = 0;
            while (offset < buffer.Length)
            {
                read = await _conn.ReceiveAsync(buffer.AsMemory(offset), ct);
                if (read == 0)
                    throw new InvalidOperationException("Connection closed while reading.");

                offset += read;
            }
        }
    }
}
