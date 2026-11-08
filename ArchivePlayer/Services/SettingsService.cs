using ArchivePlayer.Models;
using System.Text.Json;

namespace ArchivePlayer.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;

        public event EventHandler? SettingsChanged;

        public SettingsService(IWebHostEnvironment env)
        {
            _settingsFilePath = Path.Combine(env.ContentRootPath, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}