using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public static class ProtocolConstants
    {
        public const ushort ProtocolVersion = 1;

        public const int HeaderSize = 5; // 1 + 4

        public const int MaxPayloadBytes = 8 * 1024 * 1024; // 8MB safe MVP
        public const byte DefaultJpegQuality = 70; // This is the default quality we want
    }
}
