using System.Windows.Media;

namespace SpotlightWin.Core.Models
{
    /// <summary>
    /// Data model representing a single search result item in the UI.
    /// </summary>
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }
        public int Score { get; set; }
        public string ExecutionPath { get; set; } = string.Empty;

        // 🤖 AI Side Preview Properties
        public bool IsAiResult { get; set; } = false;
        public string FullContent { get; set; } = string.Empty;
    }
}