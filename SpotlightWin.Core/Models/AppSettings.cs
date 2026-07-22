namespace SpotlightWin.Core.Models
{
    public class AppSettings
    {
        public uint HotkeyModifiers { get; set; } = 0x0002; // MOD_CONTROL
        public uint HotkeyKey { get; set; } = 0x20; // Key Space
        public string HotkeyDisplayText { get; set; } = "Ctrl + Space";
        public double Opacity { get; set; } = 0.90; // 90% Opacity default
    }
}