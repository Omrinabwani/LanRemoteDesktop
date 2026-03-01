using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    [Flags]
    public enum MouseFlags : byte
    {
        Move = 0x01,
        LeftDown = 0x02,
        LeftUp = 0x04,
        RightDown = 0x08,
        RightUp = 0x10,
    }
}
