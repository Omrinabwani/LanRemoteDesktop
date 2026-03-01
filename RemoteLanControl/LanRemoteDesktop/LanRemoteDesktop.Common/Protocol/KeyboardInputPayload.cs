using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public sealed class KeyboardInputPayload
    {
        public ushort VirtualKey { get; }
        public bool IsDown { get; }
        public byte[] Serialize()
        {

            const int size = 3; // Each ushort is 2 bytes and a byte is 1 byte so 1 * 2 + 1 = 3
            byte[] bytes = new byte[size];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0, 2), VirtualKey);
            if (IsDown)
                bytes[2] = 1;
            else
                bytes[2] = 0;
            return bytes;
        }
        public KeyboardInputPayload(ushort virtualKey, bool isDown)
        {
            VirtualKey = virtualKey;
            IsDown = isDown;
        }

        public static KeyboardInputPayload Deserialize(ReadOnlySpan<byte> payload)
        {
            const int size = 3; // Each ushort is 2 bytes and a byte is 1 byte so 1 * 2 + 1 = 3
            if (payload.Length != size)
                throw new ArgumentException($"KEYBOARD_INPUT payload must be {size} bytes, got {payload.Length}.");
            ushort virtualKey = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2));
            bool isDown = payload[2] != 0;

            KeyboardInputPayload keyboardInputPayload = new KeyboardInputPayload(virtualKey, isDown);
            return keyboardInputPayload;
        }
    }

}
