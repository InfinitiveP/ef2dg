using System.Collections.Generic;
using System.Numerics;

namespace ef2dg
{
    // Освещение и пост-обработка одной сцены (локации). Сохраняется внутри JSON уровня.
    public class SceneLighting
    {
        public bool Enabled { get; set; } = false;

        // Цвет «темноты»: каким цветом и насколько ярко видна сцена там, куда не достаёт ни один источник
        public Vector3 AmbientColor { get; set; } = new Vector3(0.1f, 0.1f, 0.15f);
        public float AmbientIntensity { get; set; } = 1.0f;

        // Общий множитель яркости всех LightComponent сцены
        public float LightMultiplier { get; set; } = 1.0f;

        // Bloom: мягкое свечение вокруг ярких пикселей
        public float BloomStrength { get; set; } = 0.5f;
        public float BloomThreshold { get; set; } = 0.8f;
        public float BloomRadius { get; set; } = 1.5f;

        // Цветокоррекция итогового кадра
        public float Exposure { get; set; } = 1.0f;
        public float Contrast { get; set; } = 1.0f;
        public float Saturation { get; set; } = 1.0f;
        public Vector3 Tint { get; set; } = Vector3.One;

        // Затемнение краёв кадра
        public float VignetteStrength { get; set; } = 0.35f;
        public float VignetteSize { get; set; } = 0.45f;
        public Vector3 VignetteColor { get; set; } = Vector3.Zero;

        // Все поля — значения, поэтому поверхностной копии достаточно
        public SceneLighting Clone() => (SceneLighting)MemberwiseClone();

        // Уровни, сохранённые до появления освещения по сценам, получают прежние глобальные настройки проекта
        public static SceneLighting CreateLegacy(Scene scene)
        {
            var ambient = ProjectConfig.Data.AmbientLightColor;
            return new SceneLighting
            {
                Enabled = ProjectConfig.Data.EnableLighting && scene is not UIScene,
                AmbientColor = new Vector3(ambient.X, ambient.Y, ambient.Z),
                BloomStrength = 0f,
                VignetteStrength = 0f
            };
        }
    }

    public class LightPreset
    {
        public string Name { get; init; } = "";
        public Vector3 Color { get; init; } = Vector3.One;
        public float Intensity { get; init; } = 1f;
        public float Radius { get; init; } = 1.5f;
        public float Falloff { get; init; } = 1f;
        public float Glow { get; init; }
        public float GlowSize { get; init; } = 0.15f;
        public float FlickerAmount { get; init; }
        public float FlickerSpeed { get; init; } = 8f;
        public float PulseAmount { get; init; }
        public float PulseSpeed { get; init; } = 1f;

        public void ApplyTo(LightComponent light)
        {
            light.Color = new Silk.NET.Maths.Vector4D<float>(Color.X, Color.Y, Color.Z, 1f);
            light.Intensity = Intensity;
            light.Radius = Radius;
            light.Falloff = Falloff;
            light.Glow = Glow;
            light.GlowSize = GlowSize;
            light.FlickerAmount = FlickerAmount;
            light.FlickerSpeed = FlickerSpeed;
            light.PulseAmount = PulseAmount;
            light.PulseSpeed = PulseSpeed;
        }
    }

    public class ScenePreset
    {
        public string Name { get; set; } = "";
        public SceneLighting Settings { get; set; } = new SceneLighting();
    }

    public static class LightingPresets
    {
        // Радиусы в мировых единицах: один тайл карты = 0.5
        public static readonly IReadOnlyList<LightPreset> Lights = new List<LightPreset>
        {
            new() { Name = "Факел", Color = new(1.0f, 0.62f, 0.28f), Intensity = 1.3f, Radius = 1.8f, Falloff = 1.3f,
                    Glow = 0.7f, GlowSize = 0.1f, FlickerAmount = 0.22f, FlickerSpeed = 9f },
            new() { Name = "Свеча", Color = new(1.0f, 0.78f, 0.45f), Intensity = 1.0f, Radius = 0.9f, Falloff = 1.4f,
                    Glow = 0.6f, GlowSize = 0.1f, FlickerAmount = 0.15f, FlickerSpeed = 12f },
            new() { Name = "Магическая сфера", Color = new(0.78f, 0.55f, 1.0f), Intensity = 1.3f, Radius = 1.4f, Falloff = 1.4f,
                    Glow = 1.4f, GlowSize = 0.12f, PulseAmount = 0.15f, PulseSpeed = 0.6f },
            new() { Name = "Кристалл", Color = new(0.45f, 0.8f, 1.0f), Intensity = 1.1f, Radius = 1.2f, Falloff = 1.4f,
                    Glow = 1.0f, GlowSize = 0.12f, PulseAmount = 0.1f, PulseSpeed = 0.4f },
            new() { Name = "Ядовитое свечение", Color = new(0.5f, 1.0f, 0.35f), Intensity = 1.2f, Radius = 1.4f, Falloff = 1.3f,
                    Glow = 1.0f, GlowSize = 0.12f, PulseAmount = 0.12f, PulseSpeed = 0.8f },
            new() { Name = "Лава", Color = new(1.0f, 0.38f, 0.12f), Intensity = 1.4f, Radius = 2.2f, Falloff = 1.0f,
                    Glow = 0.4f, GlowSize = 0.25f, FlickerAmount = 0.08f, FlickerSpeed = 2.5f },
            new() { Name = "Лунный свет", Color = new(0.62f, 0.72f, 1.0f), Intensity = 0.8f, Radius = 4.0f, Falloff = 1.8f },
            new() { Name = "Аура игрока", Color = new(1.0f, 0.95f, 0.85f), Intensity = 0.5f, Radius = 1.6f, Falloff = 1.6f },
        };

        public static readonly IReadOnlyList<ScenePreset> Scenes = new List<ScenePreset>
        {
            new() { Name = "Бездна (фиолетовая)", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.42f, 0.30f, 0.62f), BloomStrength = 0.7f, BloomThreshold = 0.75f, BloomRadius = 1.5f,
                Exposure = 1.05f, Contrast = 1.08f, Saturation = 1.15f, Tint = new(1.0f, 0.96f, 1.04f),
                VignetteStrength = 0.5f, VignetteSize = 0.4f, VignetteColor = new(0.05f, 0.02f, 0.1f)
            }},
            new() { Name = "Подземелье с факелами", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.16f, 0.14f, 0.2f), BloomStrength = 0.6f, BloomThreshold = 0.8f, BloomRadius = 1.5f,
                Saturation = 0.95f, Tint = new(1.04f, 1.0f, 0.94f), VignetteStrength = 0.55f, VignetteSize = 0.35f
            }},
            new() { Name = "Пещера", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.22f, 0.25f, 0.3f), BloomStrength = 0.35f, BloomThreshold = 0.85f,
                Contrast = 1.05f, Saturation = 0.8f, VignetteStrength = 0.5f, VignetteSize = 0.4f
            }},
            new() { Name = "Лунная ночь", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.28f, 0.36f, 0.58f), BloomStrength = 0.5f, BloomThreshold = 0.7f,
                Saturation = 0.7f, Tint = new(0.92f, 0.97f, 1.08f), VignetteStrength = 0.45f, VignetteColor = new(0f, 0.02f, 0.08f)
            }},
            new() { Name = "Закат", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.85f, 0.6f, 0.55f), BloomStrength = 0.4f, BloomThreshold = 0.85f,
                Saturation = 1.05f, Tint = new(1.06f, 0.98f, 0.92f), VignetteStrength = 0.35f, VignetteColor = new(0.15f, 0.05f, 0.08f)
            }},
            new() { Name = "Ядовитое болото", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.25f, 0.4f, 0.2f), BloomStrength = 0.7f, BloomThreshold = 0.7f,
                Saturation = 1.1f, Tint = new(0.95f, 1.05f, 0.92f), VignetteStrength = 0.5f, VignetteColor = new(0.02f, 0.08f, 0.02f)
            }},
            new() { Name = "Преисподняя", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = new(0.5f, 0.18f, 0.12f), BloomStrength = 0.9f, BloomThreshold = 0.7f,
                Contrast = 1.12f, Saturation = 1.15f, Tint = new(1.08f, 0.95f, 0.9f), VignetteStrength = 0.55f, VignetteColor = new(0.12f, 0f, 0f)
            }},
            new() { Name = "День (без затемнения)", Settings = new SceneLighting
            {
                Enabled = true, AmbientColor = Vector3.One, BloomStrength = 0.2f, BloomThreshold = 0.95f, VignetteStrength = 0.15f
            }},
        };
    }
}
