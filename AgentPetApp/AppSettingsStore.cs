using System;
using System.IO;
using System.Text.Json;

namespace AgentPetApp
{
    public class AppSettings
    {
        public bool ShowPet { get; set; } = true;
        public bool ShowCountOnMenuBar { get; set; } = true;
        public bool ShowChatOnMenuBar { get; set; } = false;
        public string CurrentPetId { get; set; } = "windsurf";
        public double PetSize { get; set; } = 128.0; // S=64, M=128, L=256
        public double PetFps { get; set; } = 12.0;
        public bool IsDraggable { get; set; } = false;
        public bool ShowChatBubble { get; set; } = true;
        public double? PosX { get; set; }
        public double? PosY { get; set; }
    }

    public class AppSettingsStore
    {
        public static AppSettingsStore Shared { get; } = new AppSettingsStore();

        private string _settingsFile;
        public AppSettings Settings { get; private set; }

        public AppSettingsStore()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AgentPet");
            Directory.CreateDirectory(folder);
            _settingsFile = Path.Combine(folder, "settings.json");
            Load();
        }

        public void Load()
        {
            if (File.Exists(_settingsFile))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFile);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    Settings = new AppSettings();
                }
            }
            else
            {
                Settings = new AppSettings();
            }
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }
    }
}
