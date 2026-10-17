using System;
using System.IO;
using System.Text.Json;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Infrastructure.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;

        public AppSettings CurrentSettings { get; private set; }

        public SettingsService()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsDirectory = Path.Combine(appDataFolder, "OmniLaunch");
            _settingsFilePath = Path.Combine(settingsDirectory, "settings.json");

            CurrentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
            }

            // Fallback to default settings if file doesn't exist or load fails
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    // Ensures C:\Users\<User>\AppData\Roaming\OmniLaunch exists
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
                
                CurrentSettings = settings;
                Console.WriteLine("[SettingsService] Settings saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
            }
        }
    }
}