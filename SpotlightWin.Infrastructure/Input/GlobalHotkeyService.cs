using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;

namespace SpotlightWin.Infrastructure.Input
{
    public class GlobalHotkeyService : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private IntPtr _hWnd;
        private HwndSource? _hwndSource;
        public event Action? OnHotkeyPressed;

        public uint CurrentModifiers { get; private set; } = MOD_CONTROL;
        public uint CurrentKey { get; private set; } = (uint)Keys.Space;

        public bool Register(IntPtr handle, uint modifiers = MOD_CONTROL, uint key = (uint)Keys.Space)
        {
            _hWnd = handle;
            Unregister();

            CurrentModifiers = modifiers;
            CurrentKey = key;

            bool success = RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, key);
            if (success)
            {
                if (_hwndSource == null)
                {
                    _hwndSource = HwndSource.FromHwnd(_hWnd);
                    _hwndSource?.AddHook(HwndHook);
                }
                Console.WriteLine($"[GlobalHotkeyService] Registered Hotkey (Modifiers: {modifiers}, Key: {key}).");
            }
            else
            {
                Console.WriteLine($"[GlobalHotkeyService] Failed to register hotkey.");
            }

            return success;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                OnHotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Unregister()
        {
            if (_hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, HOTKEY_ID);
            }
        }

        public void Dispose()
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(HwndHook);
                _hwndSource = null;
            }
            Unregister();
        }
    }
}