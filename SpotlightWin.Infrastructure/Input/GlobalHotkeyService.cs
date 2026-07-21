using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SpotlightWin.Infrastructure.Input
{
    /// <summary>
    /// Listens for Win32 global system hotkeys.
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        // --- WIN32 MODIFIERS REFERENCE ---
        // MOD_ALT     = 0x0001
        // MOD_CONTROL = 0x0002
        // MOD_SHIFT   = 0x0004
        // MOD_WIN     = 0x0008

        // Default setup: Ctrl + Space
        private const uint MODIFIERS = 0x0002; // MOD_CONTROL
        private const uint VK_KEY = 0x20;      // VK_SPACE

        private IntPtr _handle;
        private HwndSource? _source;
        private bool _disposed = false;

        public event Action? OnHotkeyPressed;

        public void Register(IntPtr windowHandle)
        {
            _handle = windowHandle;
            _source = HwndSource.FromHwnd(_handle);
            _source?.AddHook(HwndHook);
            
            bool registered = RegisterHotKey(_handle, HOTKEY_ID, MODIFIERS, VK_KEY);
            if (!registered)
            {
                int errorCode = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey. Win32 Error Code: {errorCode}");
            }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_handle != IntPtr.Zero)
                    {
                        UnregisterHotKey(_handle, HOTKEY_ID);
                    }
                    if (_source != null)
                    {
                        _source.RemoveHook(HwndHook);
                        _source.Dispose();
                        _source = null;
                    }
                }
                _disposed = true;
            }
        }
    }
}