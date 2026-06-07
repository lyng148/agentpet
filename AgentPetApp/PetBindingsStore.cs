using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AgentPetCore;

namespace AgentPetApp
{
    public class PetBindings
    {
        public Dictionary<string, int> ByMood { get; set; } = new Dictionary<string, int>();

        public int ClipIndexFor(PetMood mood)
        {
            var key = mood.ToString().ToLowerInvariant();
            return ByMood.TryGetValue(key, out var idx) ? idx : 0;
        }

        public static PetBindings Defaults(int clipCount)
        {
            var order = new[] { PetMood.Idle, PetMood.Working, PetMood.Waiting, PetMood.Done, PetMood.Celebrate };
            var map = new Dictionary<string, int>();
            
            for (int i = 0; i < order.Length; i++)
            {
                var key = order[i].ToString().ToLowerInvariant();
                map[key] = clipCount > 0 ? Math.Min(i, clipCount - 1) : 0;
            }
            
            return new PetBindings { ByMood = map };
        }
    }

    public class PetBindingsStore
    {
        private static PetBindingsStore? _shared;
        public static PetBindingsStore Shared => _shared ??= new PetBindingsStore();

        private readonly Dictionary<string, PetBindings> _cache = new();
        private readonly string _settingsFile;

        private PetBindingsStore()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AgentPet");
            Directory.CreateDirectory(dir);
            _settingsFile = Path.Combine(dir, "pet_bindings.json");
            LoadAll();
        }

        public PetBindings Bindings(string packId, int clipCount)
        {
            if (_cache.TryGetValue(packId, out var cached)) return cached;
            
            var loaded = PetBindings.Defaults(clipCount);
            _cache[packId] = loaded;
            SaveAll(); // Save the newly created defaults immediately
            return loaded;
        }

        public int ClipIndex(string packId, int clipCount, PetMood mood)
        {
            return Math.Min(Bindings(packId, clipCount).ClipIndexFor(mood), Math.Max(clipCount - 1, 0));
        }

        public void SetClip(int clip, PetMood mood, string packId, int clipCount)
        {
            var current = Bindings(packId, clipCount);
            var key = mood.ToString().ToLowerInvariant();
            current.ByMood[key] = clip;
            SaveAll();
        }

        private void LoadAll()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            _cache[kvp.Key] = new PetBindings { ByMood = kvp.Value };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bindings: {ex.Message}");
            }
        }

        private void SaveAll()
        {
            try
            {
                var dictToSave = new Dictionary<string, Dictionary<string, int>>();
                foreach (var kvp in _cache)
                {
                    dictToSave[kvp.Key] = kvp.Value.ByMood;
                }
                var json = JsonSerializer.Serialize(dictToSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bindings: {ex.Message}");
            }
        }
    }
}
