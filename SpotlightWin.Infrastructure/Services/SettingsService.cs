using System;
using System.IO;
using System.Text.Json;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Infrastructure.Services
{
    public class SettingsService
    {
        private readonly string _filePath;

        public AppSettings CurrentSettings { get; private set; }

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "OmniLaunch");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "settings.json");

            CurrentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                CurrentSettings = settings;
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.ReadAllText(_filePath);
                File.WriteAllText(_filePath, json);
                Console.WriteLine("[SettingsService] Settings saved to disk.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
            }
        }
    }
}