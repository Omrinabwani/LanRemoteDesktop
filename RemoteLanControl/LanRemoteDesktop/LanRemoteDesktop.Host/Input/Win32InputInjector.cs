using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LanRemoteDesktop.Host.Input
{
    public static class Win32Input
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;

        // Mouse flags
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // Keyboard flags
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // ========================
        // Mouse helpers
        // ========================

        public static void MouseMoveAbsolute(int x, int y, ushort screenWidth, ushort screenHeight)
        {
            int absX = (int)Math.Round(x * 65535.0 / (screenWidth - 1));
            int absY = (int)Math.Round(y * 65535.0 / (screenHeight - 1));

            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new INPUTUNION
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = absX,
                            dy = absY,
                            dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                        }
                    }
                }
            };

            SendOrThrow(inputs);
        }

        public static void MouseMoveRelative(int dx, int dy)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new INPUTUNION
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = dx,
                            dy = dy,
                            dwFlags = MOUSEEVENTF_MOVE
                        }
                    }
                }
            };

            SendOrThrow(inputs);
        }

        public static void MouseLeftDown() => SendOrThrow(MouseButton(MOUSEEVENTF_LEFTDOWN));
        public static void MouseLeftUp() => SendOrThrow(MouseButton(MOUSEEVENTF_LEFTUP));
        public static void MouseRightDown() => SendOrThrow(MouseButton(MOUSEEVENTF_RIGHTDOWN));
        public static void MouseRightUp() => SendOrThrow(MouseButton(MOUSEEVENTF_RIGHTUP));

        public static void MouseWheel(int delta)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new INPUTUNION
                    {
                        mi = new MOUSEINPUT
                        {
                            mouseData = unchecked((uint)delta),
                            dwFlags = MOUSEEVENTF_WHEEL
                        }
                    }
                }
            };

            SendOrThrow(inputs);
        }

        // ========================
        // Keyboard helpers
        // ========================

        public static void KeyDownVk(ushort vk)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vk,
                            dwFlags = 0
                        }
                    }
                }
            };

            SendOrThrow(inputs);
        }

        public static void KeyUpVk(ushort vk)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vk,
                            dwFlags = KEYEVENTF_KEYUP
                        }
                    }
                }
            };

            SendOrThrow(inputs);
        }

        public static void KeyDownScan(ushort scanCode)
        {
            SendOrThrow(new[] { KeyScan(scanCode, 0) });
        }

        public static void KeyUpScan(ushort scanCode)
        {
            SendOrThrow(new[] { KeyScan(scanCode, KEYEVENTF_KEYUP) });
        }

        private static INPUT[] MouseButton(uint flag) =>
            new[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new INPUTUNION
                    {
                        mi = new MOUSEINPUT { dwFlags = flag }
                    }
                }
            };

        private static INPUT KeyScan(ushort scanCode, uint extraFlags) =>
            new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = KEYEVENTF_SCANCODE | extraFlags
                    }
                }
            };

        private static void SendOrThrow(INPUT[] inputs)
        {
            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            if (sent != inputs.Length)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}