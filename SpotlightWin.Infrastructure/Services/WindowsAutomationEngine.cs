using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpotlightWin.Infrastructure.Services
{
    public static class WindowsAutomationEngine
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        /// <summary>
        /// Moves cursor to X, Y coordinates and performs a left click
        /// </summary>
        public static void ClickAt(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
        }

        /// <summary>
        /// Types out text as keystrokes into whatever window is active
        /// </summary>
        public static void SendKeysText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            SendKeys.SendWait(text);
        }

        /// <summary>
        /// Launches Discord reliably using Windows URI or Local AppData executable
        /// </summary>
        public static void LaunchDiscord()
        {
            try
            {
                // Try URI Scheme first
                Process.Start(new ProcessStartInfo
                {
                    FileName = "discord://",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback to LocalAppData path
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string discordPath = Path.Combine(localAppData, "Discord", "Update.exe");

                if (File.Exists(discordPath))
                {
                    Process.Start(discordPath, "--processStart Discord.exe");
                }
            }
        }

        /// <summary>
        /// Opens Discord, waits for it to take focus, types the message, and presses Enter.
        /// </summary>
        public static async Task SendDiscordMessageAsync(string message)
        {
            LaunchDiscord();
            
            // Give Discord 1.5s to focus
            await Task.Delay(1500);

            // Type message and press Enter
            SendKeys.SendWait(message);
            SendKeys.SendWait("{ENTER}");
        }

        /// <summary>
        /// Opens Gmail composed directly in browser to bypass Windows Mail popups
        /// </summary>
        public static void OpenGmailCompose(string recipient, string subject, string body)
        {
            string url = $"https://mail.google.com/mail/?view=cm&fs=1&to={Uri.EscapeDataString(recipient)}&su={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}