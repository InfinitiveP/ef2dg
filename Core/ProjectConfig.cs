using System.IO;
using System.Numerics;
using System.Text.Json;

namespace ef2dg
{
    public class ConfigData
    {
        public float DefaultCameraZoom { get; set; } = 1.0f;
        public Vector4 BackgroundColor { get; set; } = new Vector4(0.2f, 0.3f, 0.3f, 1.0f);
        // Obsolete global light settings: lighting is now set per scene (SceneLighting).
        // Used only for levels saved before this change.
        public Vector4 AmbientLightColor { get; set; } = new Vector4(0.1f, 0.1f, 0.15f, 1.0f);
        public bool EnableLighting { get; set; } = false;
    }

    public static class ProjectConfig
    {
        public static ConfigData Data { get; set; } = new ConfigData();

        public static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            string json = JsonSerializer.Serialize(Data, options);
            File.WriteAllText("project_config.json", json);
        }

        public static void Load()
        {
            if (File.Exists("project_config.json"))
            {
                string json = File.ReadAllText("project_config.json");
                var options = new JsonSerializerOptions { IncludeFields = true };
                Data = JsonSerializer.Deserialize<ConfigData>(json, options) ?? new ConfigData();
            }
            else
            {
                Save();
            }
        }
    }
}