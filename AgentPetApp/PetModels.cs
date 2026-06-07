using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace AgentPetCore
{
    public class PetManifest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("spritesheetPath")]
        public string SpritesheetPath { get; set; } = string.Empty;
    }

    public class ImagePetPack
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string? Description { get; }
        public List<List<BitmapSource>> Clips { get; }
        public string Directory { get; }

        public ImagePetPack(string id, string displayName, string? description, List<List<BitmapSource>> clips, string directory)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Clips = clips;
            Directory = directory;
        }

        public int ClipCount => Clips.Count;

        public List<BitmapSource> GetClip(int index)
        {
            if (Clips.Count == 0) return new List<BitmapSource>();
            index = System.Math.Max(0, System.Math.Min(index, Clips.Count - 1));
            return Clips[index];
        }
    }
}
