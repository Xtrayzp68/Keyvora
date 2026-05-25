namespace Keyvora.Desktop.Services;

using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

public static class KeyboardSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SendKey(Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        var virtualKey = (byte)KeyInterop.VirtualKeyFromKey(key);

        SendModifiers(modifiers, isDown: true);
        keybd_event(virtualKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        SendModifiers(modifiers, isDown: false);
    }

    public static void TypeText(string text)
    {
        foreach (var ch in text)
        {
            var keyLayout = GetKeyboardLayout(0);
            var scanResult = VkKeyScanEx(ch, keyLayout);

            if (scanResult == -1) continue;

            byte vk = (byte)(scanResult & 0xFF);
            bool shift = (scanResult & 0x100) != 0;

            if (shift)
                keybd_event((byte)Key.LeftShift, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (shift)
                keybd_event((byte)Key.LeftShift, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }

    private static void SendModifiers(ModifierKeys modifiers, bool isDown)
    {
        uint flags = isDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;

        if (modifiers.HasFlag(ModifierKeys.Control))
            keybd_event((byte)Key.LeftCtrl, 0, flags, UIntPtr.Zero);
        if (modifiers.HasFlag(ModifierKeys.Alt))
            keybd_event((byte)Key.LeftAlt, 0, flags, UIntPtr.Zero);
        if (modifiers.HasFlag(ModifierKeys.Shift))
            keybd_event((byte)Key.LeftShift, 0, flags, UIntPtr.Zero);
        if (modifiers.HasFlag(ModifierKeys.Windows))
            keybd_event((byte)Key.LWin, 0, flags, UIntPtr.Zero);
    }
}
