using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpotlightWin.Infrastructure.Helpers
{
    public static class IconHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Extracts the native Windows icon for a file or shortcut path.
        /// </summary>
        public static ImageSource? ExtractAssociatedIcon(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

                using Icon? sysIcon = Icon.ExtractAssociatedIcon(filePath);
                if (sysIcon == null) return null;

                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    sysIcon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Freeze enables WPF to share the image safely across threads
                bitmapSource.Freeze();

                // Clean up Win32 handle
                DestroyIcon(sysIcon.Handle);

                return bitmapSource;
            }
            catch
            {
                return null;
            }
        }
    }
}