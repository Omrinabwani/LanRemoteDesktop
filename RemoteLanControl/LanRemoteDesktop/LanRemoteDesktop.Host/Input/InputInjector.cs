using LanRemoteDesktop.Common.Protocol;

namespace LanRemoteDesktop.Host.Input
{
    public sealed class InputInjector
    {
        public void InjectMouse(MouseInputPayload p)
        {
            if ((p.Flags & MouseFlags.Move) != 0)
                Win32Input.MouseMoveRelative(p.Dx, p.Dy);

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