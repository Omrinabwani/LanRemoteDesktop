using LanRemoteDesktop.Common.Protocol;

namespace LanRemoteDesktop.Host.Input
{
    public sealed class InputInjector
    {
        public void InjectMouse(MouseAbsInputPayload p, ushort hostW, ushort hostH, ushort viewW, ushort viewH)
        {

            if ((p.Flags & MouseFlags.Move) != 0)
            {
                int hostX = (int)Math.Round(p.X * (hostW - 1) / (double)(viewW - 1));
                int hostY = (int)Math.Round(p.Y * (hostH - 1) / (double)(viewH - 1));
                Win32Input.MouseMoveAbsolute(hostX, hostY, hostW, hostH);
            }
            if ((p.Flags & MouseFlags.LeftDown) != 0)
                Win32Input.MouseLeftDown();

            if ((p.Flags & MouseFlags.LeftUp) != 0)
                Win32Input.MouseLeftUp();

            if ((p.Flags & MouseFlags.RightDown) != 0)
                Win32Input.MouseRightDown();

            if ((p.Flags & MouseFlags.RightUp) != 0)
                Win32Input.MouseRightUp();
        }

        public void InjectKeyboard(KeyboardInputPayload p)
        {
            if (p.IsDown)
                Win32Input.KeyDownVk(p.VirtualKey);
            else
                Win32Input.KeyUpVk(p.VirtualKey);
        }
    }
}