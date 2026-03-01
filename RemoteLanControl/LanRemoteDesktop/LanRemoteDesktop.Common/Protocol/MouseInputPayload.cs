using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public sealed class MouseInputPayload
    {
        public MouseFlags Flags { get; }
        public short Dx { get; }
        public short Dy { get; }

        public MouseInputPayload(MouseFlags flags, short dx, short dy)
        {
            Flags = flags;
            Dx = dx;
            Dy = dy;
        }
        public byte[] Serialize()
        {

            const int size = 5; // Each ushort is 2 bytes and a byte is 1 byte so 2 * 2 + 1 = 5
            byte[] bytes = new byte[size];
            bytes[0] = (byte)Flags;
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(1, 2), Dx);
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(3, 2), Dy);
            return bytes;
        }

        public static MouseInputPayload Deserialize(ReadOnlySpan<byte> payload)
        {
            const int size = 5; // Each ushort is 2 bytes and a byte is 1 byte so 2 * 2 + 1 = 5
            if (payload.Length != size)
                throw new ArgumentException($"MOUSE_INPUT payload must be {size} bytes, got {payload.Length}.");
            byte flags = payload[0];
            short dx = BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(1, 2));
            short dy = BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(3, 2));
            MouseInputPayload mouseInputPayload = new MouseInputPayload((MouseFlags)flags, dx, dy);
            return mouseInputPayload;
        }
    }
}
