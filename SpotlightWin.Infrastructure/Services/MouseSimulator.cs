using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SpotlightWin.Infrastructure.Services
{
    public static class MouseSimulator
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public static async Task ClickAtAsync(double x, double y)
        {
            int posX = (int)x;
            int posY = (int)y;

            SetCursorPos(posX, posY);
            await Task.Delay(100);

            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)posX, (uint)posY, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)posX, (uint)posY, 0, 0);
        }
    }
}