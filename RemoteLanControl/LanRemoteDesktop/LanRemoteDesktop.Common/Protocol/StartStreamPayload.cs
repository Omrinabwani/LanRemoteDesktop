using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public sealed class StartStreamPayload
    {
        public byte Fps { get; }
        public StartStreamPayload(byte fps) 
        {
            if (fps == 0 || fps > 60)
                throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be between 1 and 60.");
            Fps = fps; 
        }
        public byte[] Serialize()
        {
            const int size = 1; // One byte for fps
            byte[] bytes = new byte[size];
            bytes[0] = Fps;
            return bytes;
        }
        public static StartStreamPayload Deserialize(ReadOnlySpan<byte> payload)
        {
            const int size = 1; // One byte for fps
            if (payload.Length != size)
                throw new ArgumentException($"START_STREAM payload must be {size} bytes, got {payload.Length}.");
            byte fps = payload[0];
            StartStreamPayload helloPayload = new StartStreamPayload(fps);
            return helloPayload;
        }
    }
}
