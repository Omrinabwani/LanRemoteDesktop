using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public sealed class MouseAbsInputPayload
    {
        public MouseFlags Flags { get; }
        public ushort X { get; }
        public ushort Y { get; }

        public MouseAbsInputPayload(MouseFlags flags, ushort x, ushort y)
        {
            Flags = flags;
            X = x;
            Y = y;
        }
        public byte[] Serialize()
        {

            const int size = 5; // Each ushort is 2 bytes and a byte is 1 byte so 2 * 2 + 1 = 5
            byte[] bytes = new byte[size];
            bytes[0] = (byte)Flags;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(1, 2), X);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(3, 2), Y);
            return bytes;
        }

        public static MouseAbsInputPayload Deserialize(ReadOnlySpan<byte> payload)
        {
            const int size = 5; // Each ushort is 2 bytes and a byte is 1 byte so 2 * 2 + 1 = 5
            if (payload.Length != size)
                throw new ArgumentException($"MOUSE_INPUT payload must be {size} bytes, got {payload.Length}.");
            byte flags = payload[0];
            ushort x = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(1, 2));
            ushort y = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(3, 2));
            MouseAbsInputPayload mouseAbsInputPayload = new MouseAbsInputPayload((MouseFlags)flags, x, y);
            return mouseAbsInputPayload;
        }
    }
}
