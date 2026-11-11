using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace ef2dg
{
    // Lighting settings window for the current scene: location presets, darkness, bloom, colour grading and the source list.
    public class LightingWindow : EditorWindow
    {
        public override string Title => "Lighting";

        private const string CustomPresetsPath = "lighting_presets.json";
        private static readonly JsonSerializerOptions PresetJsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

        private List<ScenePreset>? _customPresets;
        private string _newPresetName = "My location";

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(360, 560), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(60, 80), ImGuiCond.FirstUseEver);
            ImGui.Begin(Title, ref IsOpen);

            var scene = ctx.CurrentScene;
            var lighting = scene.Lighting;
            string sceneName = string.IsNullOrEmpty(scene.InstanceName) ? scene.GetType().Name : scene.InstanceName;

            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), $"Scene: {sceneName}");
            ImGui.TextDisabled("Saved together with the level (File > Save, Ctrl+S)");
            ImGui.Spacing();

            bool enabled = lighting.Enabled;
            if (ImGui.Checkbox("Enable lighting", ref enabled)) lighting.Enabled = enabled;
            Tip("Lighting and post-effects for this scene only.\nEvery location has its own settings.");

            ImGui.SameLine();
            bool preview = DebugSettings.PreviewLightingInEditor;
            if (ImGui.Checkbox("In editor", ref preview)) DebugSettings.PreviewLightingInEditor = preview;
            Tip("Show lighting outside Play Mode.\nTurn it off if placing objects in the dark is inconvenient.");

            DrawPresets(scene);

            if (!lighting.Enabled)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.3f, 1.0f), "Lighting is off: enable it or pick a preset.");
            }

            ImGui.Spacing();
            if (ImGui.CollapsingHeader("Darkness (ambient light)", ImGuiTreeNodeFlags.DefaultOpen))
            {
                lighting.AmbientColor = Color3("Color##ambient", lighting.AmbientColor,
                    "The scene colour where no light source reaches.\nDark purple — abyss, dark blue — night, white — no darkening.");
                lighting.AmbientIntensity = Slider("Brightness##ambient", lighting.AmbientIntensity, 0f, 2f,
                    "Darkness colour multiplier. 0 — total darkness, 1 — exactly the chosen colour.");
            }

            if (ImGui.CollapsingHeader("Light sources", ImGuiTreeNodeFlags.DefaultOpen))
            {
                lighting.LightMultiplier = Slider("Overall brightness", lighting.LightMultiplier, 0f, 3f,
                    "Global multiplier for every LightComponent in this scene.");
                DrawLightList(ctx, state);
            }

            if (ImGui.CollapsingHeader("Glow (Bloom)", ImGuiTreeNodeFlags.DefaultOpen))
            {
                lighting.BloomStrength = Slider("Strength##bloom", lighting.BloomStrength, 0f, 3f,
                    "A soft halo around bright spots: orbs, fire, an overexposed floor. 0 — off.");
                lighting.BloomThreshold = Slider("Threshold##bloom", lighting.BloomThreshold, 0f, 2f,
                    "Brightness at which a pixel starts to glow. Lower — more details glow.");
                lighting.BloomRadius = Slider("Blur##bloom", lighting.BloomRadius, 0.5f, 4f,
                    "Halo width.");
            }

            if (ImGui.CollapsingHeader("Color grading"))
            {
                lighting.Exposure = Slider("Exposure", lighting.Exposure, 0.2f, 3f, "Overall frame brightness.");
                lighting.Contrast = Slider("Contrast", lighting.Contrast, 0.5f, 2f, "1 — unchanged.");
                lighting.Saturation = Slider("Saturation", lighting.Saturation, 0f, 2f, "0 — black and white, 1 — unchanged.");
                lighting.Tint = Color3("Tint", lighting.Tint, "Colour filter over the whole frame. White — unchanged.");
            }

            if (ImGui.CollapsingHeader("Vignette"))
            {
                lighting.VignetteStrength = Slider("Strength##vignette", lighting.VignetteStrength, 0f, 1f, "Darkening of the frame edges. 0 — off.");
                lighting.VignetteSize = Slider("Size##vignette", lighting.VignetteSize, 0f, 1f, "Where the darkening starts: smaller — closer to the centre.");
                lighting.VignetteColor = Color3("Color##vignette", lighting.VignetteColor, "Edge colour. Dark purple deepens the abyss atmosphere.");
            }

            ImGui.Spacing();
            if (ImGui.Button("Reset scene settings", new Vector2(-1, 0)))
            {
                scene.Lighting = new SceneLighting { Enabled = lighting.Enabled };
            }

            ImGui.End();
        }

        private void DrawPresets(Scene scene)
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##ScenePreset", "Apply a location preset..."))
            {
                ImGui.TextDisabled("Built-in");
                foreach (var preset in LightingPresets.Scenes)
                {
                    if (ImGui.Selectable(preset.Name)) scene.Lighting = preset.Settings.Clone();
                }

                var custom = GetCustomPresets();
                if (custom.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.TextDisabled("Saved");
                    foreach (var preset in custom)
                    {
                        if (ImGui.Selectable($"{preset.Name}##custom")) scene.Lighting = preset.Settings.Clone();
                    }
                }
                ImGui.EndCombo();
            }
            Tip("A ready set of settings for a location type. You can fine-tune it below afterwards.\nRight-click — delete a saved preset.");

            if (ImGui.BeginPopupContextItem("PresetContext"))
            {
                ImGui.TextDisabled("Delete a saved preset:");
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
                if (custom.Count == 0) ImGui.TextDisabled("No saved presets.");
                ImGui.EndPopup();
            }

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 130);
            ImGui.InputText("##NewPresetName", ref _newPresetName, 64);
            ImGui.SameLine();
            if (ImGui.Button("Save preset", new Vector2(-1, 0)) && !string.IsNullOrWhiteSpace(_newPresetName))
            {
                SaveCustomPreset(_newPresetName.Trim(), scene.Lighting);
            }
            Tip("Saves the current settings to lighting_presets.json\nso you can apply them to other levels of this location.");
        }

        private void DrawLightList(EditorContext ctx, EditorState state)
        {
            if (ImGui.Button("+ Add light source", new Vector2(-1, 0))) ImGui.OpenPopup("AddLightPopup");
            Tip("Creates an object with a LightComponent in the centre of the camera.");

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

                string label = light.Enabled ? entity.Name : $"{entity.Name} (off)";
                if (ImGui.Selectable(label, state.SelectedEntity == entity))
                {
                    state.SelectedEntity = entity;
                }
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    var center = light.WorldPosition;
                    ctx.GameCamera.Position = new Vector3D<float>(center.X, center.Y, ctx.GameCamera.Position.Z);
                }
                Tip("Click — select and edit in the Inspector.\nDouble click — show in the scene.");
                ImGui.PopID();
            }

            if (lightCount == 0) ImGui.TextDisabled("The scene has no light sources yet.");
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

        // LightComponent block in the Inspector: clear labels, limited ranges and presets instead of raw numbers
        public static void DrawLightInspector(LightComponent light, Scene scene)
        {
            if (!scene.Lighting.Enabled)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.3f, 1.0f), "Scene lighting is off (Lighting window).");
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##LightPreset", "Source preset..."))
            {
                foreach (var preset in LightingPresets.Lights)
                {
                    if (ImGui.Selectable(preset.Name)) preset.ApplyTo(light);
                }
                ImGui.EndCombo();
            }

            bool enabled = light.Enabled;
            if (ImGui.Checkbox("Enabled", ref enabled)) light.Enabled = enabled;

            var color = new Vector3(light.Color.X, light.Color.Y, light.Color.Z);
            color = Color3("Color", color, "Light colour.");
            light.Color = new Vector4D<float>(color.X, color.Y, color.Z, light.Color.W);

            light.Intensity = Slider("Brightness", light.Intensity, 0f, 5f, "Above 1 the surface is overexposed and starts to glow (bloom).");

            float radius = light.Radius;
            if (ImGui.DragFloat("Radius", ref radius, 0.02f, 0.05f, 30f, "%.2f")) light.Radius = MathF.Max(0.05f, radius);
            Tip("Light range in world units (one tile = 0.5).");

            var offset = new Vector2(light.Offset.X, light.Offset.Y);
            if (ImGui.DragFloat2("Offset", ref offset, 0.01f)) light.Offset = new Vector2D<float>(offset.X, offset.Y);
            Tip("Source offset from the object position, for example a flame above a torch.");

            light.Falloff = Slider("Edge falloff", light.Falloff, 0.2f, 4f, "Below 1 — an even spot, above 1 — light concentrated in the centre.");

            ImGui.SeparatorText("Glow core");
            light.Glow = Slider("Strength##glow", light.Glow, 0f, 4f, "A visible glowing point in the centre: orb, flame, crystal. 0 — none.");
            light.GlowSize = Slider("Size##glow", light.GlowSize, 0.02f, 1f, "Core size as a fraction of the radius.");

            ImGui.SeparatorText("Animation");
            light.FlickerAmount = Slider("Flicker", light.FlickerAmount, 0f, 1f, "Uneven fire flicker. 0 — none.");
            light.FlickerSpeed = Slider("Flicker speed", light.FlickerSpeed, 0f, 30f, "");
            light.PulseAmount = Slider("Pulse", light.PulseAmount, 0f, 1f, "Smooth “breathing” of magic and crystals. 0 — none.");
            light.PulseSpeed = Slider("Pulse speed", light.PulseSpeed, 0f, 5f, "Pulses per second.");
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
            // Ctrl+click on a slider lets you enter a value outside the range
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
