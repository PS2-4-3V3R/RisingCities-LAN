using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Resources.Common.Scripts
{
    public class KeyEventInfo
    {
        public uint VkCode { get; init; }
        public EventType EventType { get; init; } = new();
        public DateTime Timestamp { get; init; }
        public KeyboardListener.KBDLLHOOKSTRUCT RawData { get; init; }
        public string KeyName { get; init; } = string.Empty;
        public ModifiersState Modifiers { get; init; } = new();
    }

    public class EventType
    {
        public string Text { get; set; } = string.Empty;
        public bool KeyDown { get; set; }
        public bool KeyUp { get; set; }
        public bool IsSystemKey { get; set; }
        public bool IsExtendedKey { get; set; }
    }

    public class ModifiersState
    {
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
    }

    public static class KeyboardListener
    {
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static HookProc _hookProc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static Action<KeyEventInfo>? _onKeyEvent;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int nSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public static void Start(Action<KeyEventInfo> onKeyEvent)
        {
            _onKeyEvent = onKeyEvent;
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, IntPtr.Zero, 0);

            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0) { }
        }
        public static void StartAsync(Action<KeyEventInfo> onKeyEvent)
        {
            new Thread(() =>
            {
                Start(onKeyEvent); // Llama al método actual que instala el hook y entra en el bucle
            })
            {
                IsBackground = true
            }.Start();
        }

        [DllImport("user32.dll")]
        private static extern bool PostQuitMessage(int nExitCode);
        public static void Stop()
        {
            UnhookWindowsHookEx(_hookID);
            PostQuitMessage(0);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _onKeyEvent != null)
            {
                var keyInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                var eventType = new EventType
                {
                    IsExtendedKey = (keyInfo.flags & 0x01) != 0
                };

                switch (wParam)
                {
                    case (IntPtr)WM_KEYDOWN:
                        eventType.Text = "KeyDown";
                        eventType.KeyDown = true;
                        break;
                    case (IntPtr)WM_SYSKEYDOWN:
                        eventType.Text = "KeyDown";
                        eventType.KeyDown = true;
                        eventType.IsSystemKey = true;
                        break;
                    case (IntPtr)WM_KEYUP:
                        eventType.Text = "KeyUp";
                        eventType.KeyUp = true;
                        break;
                    case (IntPtr)WM_SYSKEYUP:
                        eventType.Text = "KeyUp";
                        eventType.KeyUp = true;
                        eventType.IsSystemKey = true;
                        break;
                    default:
                        eventType.Text = "Unknown";
                        break;
                }

                var modifiers = new ModifiersState
                {
                    Ctrl = (GetKeyState(0x11) & 0x8000) != 0, // VK_CONTROL
                    Shift = (GetKeyState(0x10) & 0x8000) != 0, // VK_SHIFT
                    Alt = (GetKeyState(0x12) & 0x8000) != 0    // VK_MENU
                };

                string keyName = GetKeyName(keyInfo);

                _onKeyEvent(new KeyEventInfo
                {
                    VkCode = keyInfo.vkCode,
                    EventType = eventType,
                    Timestamp = DateTime.Now,
                    RawData = keyInfo,
                    KeyName = keyName,
                    Modifiers = modifiers
                });
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static string GetKeyName(KBDLLHOOKSTRUCT keyInfo)
        {
            uint scanCode = MapVirtualKey(keyInfo.vkCode, 0);
            int lParam = (int)(scanCode << 16);

            if ((keyInfo.flags & 0x01) != 0)
                lParam |= 1 << 24;

            StringBuilder sb = new StringBuilder(64);
            if (GetKeyNameText(lParam, sb, sb.Capacity) > 0)
                return sb.ToString();

            return $"VK_{keyInfo.vkCode}";
        }
    }
}
