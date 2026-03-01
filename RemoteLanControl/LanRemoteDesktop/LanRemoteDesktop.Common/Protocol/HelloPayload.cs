using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;

namespace LanRemoteDesktop.Common.Protocol
{
    public sealed class HelloPayload
    {
        public ushort ProtocolVersion { get; }
        public ushort Flags { get; }
        public ushort ClientViewWidth { get; }
        public ushort ClientViewHeight { get; }
        public byte JpegQuality { get; }
        
        
        
        public HelloPayload(ushort protocolVersion, ushort flags, ushort clientViewWidth, ushort clientViewHeight, byte jpegQuality)
        {
            ClientViewWidth = clientViewWidth;
            ClientViewHeight = clientViewHeight;
            Flags = flags;
            JpegQuality = jpegQuality;
            ProtocolVersion = protocolVersion;
            
        }

        public byte[] Serialize()
        {
            const int size = 9; // Each ushort is 2 bytes and a byte is 1 byte so 2 * 4 + 1 = 9
            byte[] bytes = new byte[size];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0, 2), ProtocolVersion);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2,2), Flags);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4,2), ClientViewWidth);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6, 2), ClientViewHeight);
            bytes[8] = JpegQuality;
            return bytes;
        }

        public static HelloPayload Deserialize(ReadOnlySpan<byte> payload)
        {
            const int size = 9; // The size should be ushort * 4 + 1 and u short is 2 bytes so its 9 bytes all together
            if (payload.Length != size)
                throw new ArgumentException($"WELCOME payload must be {size} bytes, got {payload.Length}.");
            ushort protocolVersion = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2));
            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2));
            ushort clientViewWidth = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2));
            ushort clientViewHeight = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(6, 2));
            byte jpegQuality = payload[8];
            HelloPayload helloPayload = new HelloPayload(protocolVersion, flags, clientViewWidth, clientViewHeight, jpegQuality);
            return helloPayload;
        }
    }
}
