using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public class FrameHeader
    {
        public MessageType Type { get; }
        public uint Length { get; }
        public FrameHeader(MessageType type, uint length)
        {
            if (length > ProtocolConstants.MaxPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(length));
            Type = type;
            Length = length;
        }
    }
}
