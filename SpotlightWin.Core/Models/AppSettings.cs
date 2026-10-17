namespace SpotlightWin.Core.Models
{
    public class AppSettings
    {
        // AI Provider Keys (Kept empty and safe!)
        public string GroqApiKey { get; set; } = string.Empty;
        public string GeminiApiKey { get; set; } = string.Empty;
        public string PreferredAiProvider { get; set; } = "Gemini"; // Or "Groq"

        // Window & UI Settings
        public double WindowOpacity { get; set; } = 0.95;
        public double Opacity { get; set; } = 0.90; // Fallback helper matching your zip code
        public bool StartWithWindows { get; set; } = false;

        // Windows Hotkey Mapping Definitions (Ctrl + Space Default)
        public string HotkeyText { get; set; } = "Ctrl + Space";
        public string HotkeyDisplayText { get; set; } = "Ctrl + Space";
        public uint HotkeyModifiers { get; set; } = 0x0002; // MOD_CONTROL
        public uint HotkeyKey { get; set; } = 0x20;       // VK_SPACE / Spacebar
    }
}
