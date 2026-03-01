using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanRemoteDesktop.Common.Protocol
{
    public enum MessageType : byte
    {
        Hello = 0x01,
        Welcome = 0x02,
        StartStream = 0x03,
        StopStream = 0x04,
        FrameJpeg = 0x10,
        InputMouse = 0x20,
        InputKeyboard = 0x21,

    }
}
