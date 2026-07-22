using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotlightWin.Infrastructure.Services;

namespace SpotlightWin.UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private string _geminiApiKey = string.Empty;

        [ObservableProperty]
        private double _opacity = 0.90;

        public SettingsViewModel()
        {
            _settingsService = new SettingsService();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (_settingsService.CurrentSettings != null)
            {
                GeminiApiKey = _settingsService.CurrentSettings.GeminiApiKey ?? string.Empty;
                Opacity = _settingsService.CurrentSettings.Opacity;
            }
        }

        [RelayCommand]
        private void Save(Window? window)
        {
            if (_settingsService.CurrentSettings != null)
            {
                _settingsService.CurrentSettings.GeminiApiKey = GeminiApiKey;
                _settingsService.CurrentSettings.Opacity = Opacity;
                _settingsService.SaveSettings(_settingsService.CurrentSettings);
            }

            window?.Close();
        }
    }
}