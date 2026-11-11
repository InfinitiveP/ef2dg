using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace ef2dg
{
    // Окно настройки освещения текущей сцены: пресеты локаций, темнота, bloom, цветокоррекция и список источников.
    public class LightingWindow : EditorWindow
    {
        public override string Title => "Lighting";

        private const string CustomPresetsPath = "lighting_presets.json";
        private static readonly JsonSerializerOptions PresetJsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

        private List<ScenePreset>? _customPresets;
        private string _newPresetName = "Моя локация";

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(360, 560), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(60, 80), ImGuiCond.FirstUseEver);
            ImGui.Begin(Title, ref IsOpen);

            var scene = ctx.CurrentScene;
            var lighting = scene.Lighting;
            string sceneName = string.IsNullOrEmpty(scene.InstanceName) ? scene.GetType().Name : scene.InstanceName;

            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), $"Сцена: {sceneName}");
            ImGui.TextDisabled("Сохраняется вместе с уровнем (File > Save, Ctrl+S)");
            ImGui.Spacing();

            bool enabled = lighting.Enabled;
            if (ImGui.Checkbox("Включить освещение", ref enabled)) lighting.Enabled = enabled;
            Tip("Освещение и пост-эффекты только для этой сцены.\nУ каждой локации свои настройки.");

            ImGui.SameLine();
            bool preview = DebugSettings.PreviewLightingInEditor;
            if (ImGui.Checkbox("В редакторе", ref preview)) DebugSettings.PreviewLightingInEditor = preview;
            Tip("Показывать освещение вне Play Mode.\nВыключите, если в темноте неудобно расставлять объекты.");

            DrawPresets(scene);

            if (!lighting.Enabled)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.3f, 1.0f), "Освещение выключено: включите его или выберите пресет.");
            }

            ImGui.Spacing();
            if (ImGui.CollapsingHeader("Темнота (общий свет)", ImGuiTreeNodeFlags.DefaultOpen))
            {
                lighting.AmbientColor = Color3("Цвет##ambient", lighting.AmbientColor,
                    "Каким цветом видна сцена там, куда не достаёт ни один источник.\nТёмно-фиолетовый — бездна, тёмно-синий — ночь, белый — без затемнения.");
                lighting.AmbientIntensity = Slider("Яркость##ambient", lighting.AmbientIntensity, 0f, 2f,
                    "Множитель цвета темноты. 0 — полная тьма, 1 — как выбрано в цвете.");
            }

            if (ImGui.CollapsingHeader("Источники света", ImGuiTreeNodeFlags.DefaultOpen))
            {
                lighting.LightMultiplier = Slider("Яркость всех", lighting.LightMultiplier, 0f, 3f,
                    "Общий множитель для всех LightComponent этой сцены.");
                DrawLightList(ctx, state);
            }

            if (ImGui.CollapsingHeader("Свечение (Bloom)", ImGuiTreeNodeFlags.DefaultOpen))
            {
                lighting.BloomStrength = Slider("Сила##bloom", lighting.BloomStrength, 0f, 3f,
                    "Мягкий ореол вокруг ярких мест: сфер, огня, пересвеченного пола. 0 — выключено.");
                lighting.BloomThreshold = Slider("Порог##bloom", lighting.BloomThreshold, 0f, 2f,
                    "С какой яркости пиксель начинает светиться. Меньше — светится больше деталей.");
                lighting.BloomRadius = Slider("Размытие##bloom", lighting.BloomRadius, 0.5f, 4f,
                    "Ширина ореола.");
            }

            if (ImGui.CollapsingHeader("Цветокоррекция"))
            {
                lighting.Exposure = Slider("Экспозиция", lighting.Exposure, 0.2f, 3f, "Общая яркость кадра.");
                lighting.Contrast = Slider("Контраст", lighting.Contrast, 0.5f, 2f, "1 — без изменений.");
                lighting.Saturation = Slider("Насыщенность", lighting.Saturation, 0f, 2f, "0 — чёрно-белое изображение, 1 — без изменений.");
                lighting.Tint = Color3("Оттенок", lighting.Tint, "Цветовой фильтр поверх всего кадра. Белый — без изменений.");
            }

            if (ImGui.CollapsingHeader("Виньетка"))
            {
                lighting.VignetteStrength = Slider("Сила##vignette", lighting.VignetteStrength, 0f, 1f, "Затемнение краёв кадра. 0 — выключено.");
                lighting.VignetteSize = Slider("Размер##vignette", lighting.VignetteSize, 0f, 1f, "Где начинается затемнение: меньше — ближе к центру.");
                lighting.VignetteColor = Color3("Цвет##vignette", lighting.VignetteColor, "Цвет краёв. Тёмно-фиолетовый усиливает атмосферу бездны.");
            }

            ImGui.Spacing();
            if (ImGui.Button("Сбросить настройки сцены", new Vector2(-1, 0)))
            {
                scene.Lighting = new SceneLighting { Enabled = lighting.Enabled };
            }

            ImGui.End();
        }

        private void DrawPresets(Scene scene)
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##ScenePreset", "Применить пресет локации..."))
            {
                ImGui.TextDisabled("Встроенные");
                foreach (var preset in LightingPresets.Scenes)
                {
                    if (ImGui.Selectable(preset.Name)) scene.Lighting = preset.Settings.Clone();
                }

                var custom = GetCustomPresets();
                if (custom.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.TextDisabled("Сохранённые");
                    foreach (var preset in custom)
                    {
                        if (ImGui.Selectable($"{preset.Name}##custom")) scene.Lighting = preset.Settings.Clone();
                    }
                }
                ImGui.EndCombo();
            }
            Tip("Готовый набор настроек для типа локации. Потом его можно подправить ниже.\nПКМ — удалить сохранённый пресет.");

            if (ImGui.BeginPopupContextItem("PresetContext"))
            {
                ImGui.TextDisabled("Удалить сохранённый пресет:");
                var custom = GetCustomPresets();
                for (int i = 0; i < custom.Count; i++)
                {
                    if (ImGui.Selectable($"X  {custom[i].Name}##del{i}"))
                    {
                        custom.RemoveAt(i);
                        WriteCustomPresets();
                        break;
                    }
                }
                if (custom.Count == 0) ImGui.TextDisabled("Нет сохранённых пресетов.");
                ImGui.EndPopup();
            }

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 130);
            ImGui.InputText("##NewPresetName", ref _newPresetName, 64);
            ImGui.SameLine();
            if (ImGui.Button("Сохранить пресет", new Vector2(-1, 0)) && !string.IsNullOrWhiteSpace(_newPresetName))
            {
                SaveCustomPreset(_newPresetName.Trim(), scene.Lighting);
            }
            Tip("Сохраняет текущие настройки в lighting_presets.json,\nчтобы применить их к другим уровням этой локации.");
        }

        private void DrawLightList(EditorContext ctx, EditorState state)
        {
            if (ImGui.Button("+ Добавить источник света", new Vector2(-1, 0))) ImGui.OpenPopup("AddLightPopup");
            Tip("Создаёт объект с LightComponent в центре камеры.");

            if (ImGui.BeginPopup("AddLightPopup"))
            {
                foreach (var preset in LightingPresets.Lights)
                {
                    if (ImGui.MenuItem(preset.Name)) CreateLight(ctx, state, preset);
                }
                ImGui.EndPopup();
            }

            int lightCount = 0;
            foreach (var entity in ctx.CurrentScene.Entities)
            {
                var light = entity.GetComponent<LightComponent>();
                if (light == null) continue;
                lightCount++;

                ImGui.PushID(entity.GetHashCode());
                var swatch = new Vector4(light.Color.X, light.Color.Y, light.Color.Z, 1f);
                ImGui.ColorButton("##swatch", swatch, ImGuiColorEditFlags.NoTooltip, new Vector2(14, 14));
                ImGui.SameLine();

                string label = light.Enabled ? entity.Name : $"{entity.Name} (выкл.)";
                if (ImGui.Selectable(label, state.SelectedEntity == entity))
                {
                    state.SelectedEntity = entity;
                }
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    var center = light.WorldPosition;
                    ctx.GameCamera.Position = new Vector3D<float>(center.X, center.Y, ctx.GameCamera.Position.Z);
                }
                Tip("Клик — выбрать и настроить в Inspector.\nДвойной клик — показать на сцене.");
                ImGui.PopID();
            }

            if (lightCount == 0) ImGui.TextDisabled("В сцене пока нет источников света.");
        }

        private static void CreateLight(EditorContext ctx, EditorState state, LightPreset preset)
        {
            var scene = ctx.CurrentScene;
            var entity = new GameObject
            {
                Name = preset.Name,
                Position = new Vector2D<float>(ctx.GameCamera.Position.X, ctx.GameCamera.Position.Y),
                ParentScene = scene
            };

            var light = new LightComponent();
            preset.ApplyTo(light);
            entity.AddComponent(light);

            scene.Entities.Add(entity);
            state.SelectedEntity = entity;
        }

        // Блок LightComponent в Inspector: понятные подписи, ограниченные диапазоны и пресеты вместо сырых чисел
        public static void DrawLightInspector(LightComponent light, Scene scene)
        {
            if (!scene.Lighting.Enabled)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.3f, 1.0f), "Освещение сцены выключено (окно Lighting).");
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##LightPreset", "Пресет источника..."))
            {
                foreach (var preset in LightingPresets.Lights)
                {
                    if (ImGui.Selectable(preset.Name)) preset.ApplyTo(light);
                }
                ImGui.EndCombo();
            }

            bool enabled = light.Enabled;
            if (ImGui.Checkbox("Включён", ref enabled)) light.Enabled = enabled;

            var color = new Vector3(light.Color.X, light.Color.Y, light.Color.Z);
            color = Color3("Цвет", color, "Цвет света.");
            light.Color = new Vector4D<float>(color.X, color.Y, color.Z, light.Color.W);

            light.Intensity = Slider("Яркость", light.Intensity, 0f, 5f, "Больше 1 — поверхность пересвечивается и начинает светиться (bloom).");

            float radius = light.Radius;
            if (ImGui.DragFloat("Радиус", ref radius, 0.02f, 0.05f, 30f, "%.2f")) light.Radius = MathF.Max(0.05f, radius);
            Tip("Дальность света в мировых единицах (один тайл = 0.5).");

            var offset = new Vector2(light.Offset.X, light.Offset.Y);
            if (ImGui.DragFloat2("Смещение", ref offset, 0.01f)) light.Offset = new Vector2D<float>(offset.X, offset.Y);
            Tip("Сдвиг источника от позиции объекта, например пламя над факелом.");

            light.Falloff = Slider("Спад к краю", light.Falloff, 0.2f, 4f, "Меньше 1 — ровное пятно, больше 1 — свет собран у центра.");

            ImGui.SeparatorText("Ядро свечения");
            light.Glow = Slider("Сила##glow", light.Glow, 0f, 4f, "Видимая светящаяся точка в центре: сфера, пламя, кристалл. 0 — нет.");
            light.GlowSize = Slider("Размер##glow", light.GlowSize, 0.02f, 1f, "Размер ядра как доля радиуса.");

            ImGui.SeparatorText("Анимация");
            light.FlickerAmount = Slider("Мерцание", light.FlickerAmount, 0f, 1f, "Неровное дрожание огня. 0 — нет.");
            light.FlickerSpeed = Slider("Скорость мерцания", light.FlickerSpeed, 0f, 30f, "");
            light.PulseAmount = Slider("Пульсация", light.PulseAmount, 0f, 1f, "Плавное «дыхание» магии и кристаллов. 0 — нет.");
            light.PulseSpeed = Slider("Скорость пульсации", light.PulseSpeed, 0f, 5f, "Пульсов в секунду.");
        }

        private List<ScenePreset> GetCustomPresets()
        {
            if (_customPresets != null) return _customPresets;

            _customPresets = new List<ScenePreset>();
            if (!File.Exists(CustomPresetsPath)) return _customPresets;
            try
            {
                _customPresets = JsonSerializer.Deserialize<List<ScenePreset>>(File.ReadAllText(CustomPresetsPath), PresetJsonOptions)
                    ?? new List<ScenePreset>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lighting] Failed to read {CustomPresetsPath}: {ex.Message}");
            }
            return _customPresets;
        }

        private void SaveCustomPreset(string name, SceneLighting settings)
        {
            var presets = GetCustomPresets();
            var snapshot = settings.Clone();
            snapshot.Enabled = true;

            int existing = presets.FindIndex(p => p.Name == name);
            if (existing >= 0) presets[existing].Settings = snapshot;
            else presets.Add(new ScenePreset { Name = name, Settings = snapshot });

            WriteCustomPresets();
            Console.WriteLine($"[Lighting] Preset saved: {name}");
        }

        private void WriteCustomPresets()
        {
            try
            {
                File.WriteAllText(CustomPresetsPath, JsonSerializer.Serialize(GetCustomPresets(), PresetJsonOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lighting] Failed to write {CustomPresetsPath}: {ex.Message}");
            }
        }

        private static float Slider(string label, float value, float min, float max, string tooltip)
        {
            // Ctrl+клик по слайдеру позволяет ввести значение за пределами диапазона
            ImGui.SliderFloat(label, ref value, min, max, "%.2f");
            Tip(tooltip);
            return value;
        }

        private static Vector3 Color3(string label, Vector3 value, string tooltip)
        {
            ImGui.ColorEdit3(label, ref value);
            Tip(tooltip);
            return value;
        }

        private static void Tip(string text)
        {
            if (!string.IsNullOrEmpty(text) && ImGui.IsItemHovered()) ImGui.SetTooltip(text);
        }
    }
}
