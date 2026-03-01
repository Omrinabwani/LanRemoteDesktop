using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
namespace LanRemoteDesktop.Common.Protocol
{
    public sealed class WelcomePayload
    {
        public ushort ProtocolVersion { get; }
        public ushort Flags { get; }
        public ushort ScreenWidth { get; }
        public ushort ScreenHeight { get; }
        public byte JpegQualityApplied { get; }
        
        
        
        public WelcomePayload(ushort protocolVersion, ushort flags, ushort screenWidth, ushort screenHeight, byte jpegQuality)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            Flags = flags;
            JpegQualityApplied = jpegQuality;
            ProtocolVersion = protocolVersion;
            
        }

        public byte[] Serialize()
        {

            const int size = 9; // Each ushort is 2 bytes and a byte is 1 byte so 2 * 4 + 1 = 9
            byte[] bytes = new byte[size];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0, 2), ProtocolVersion);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2,2), Flags);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4,2), ScreenWidth);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6, 2), ScreenHeight);
            bytes[8] = JpegQualityApplied;
            return bytes;
        }

        public static WelcomePayload Deserialize(ReadOnlySpan<byte> payload)
        {
            const int size = 9; // The size should be ushort * 4 + 1 and u short is 2 bytes so its 9 bytes all together
            if (payload.Length != size)
                throw new ArgumentException($"WELCOME payload must be {size} bytes, got {payload.Length}.");
            ushort protocolVersion = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2));
            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2));
            ushort screenWidth = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2));
            ushort screenHeight = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(6, 2));
            byte jpegQuality = payload[8];
            WelcomePayload welcomePayload = new WelcomePayload(protocolVersion, flags, screenWidth, screenHeight, jpegQuality);
            return welcomePayload;
        }
    }
}
