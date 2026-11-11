using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Numerics;

namespace ef2dg
{
    public class InspectorWindow : EditorWindow
    {
        public override string Title => "Inspector";

        private static Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(1120, 20), ImGuiCond.FirstUseEver);
            ImGui.Begin(Title, ref IsOpen);

            if (state.SelectedEntity == null)
            {
                ImGui.TextDisabled("Select an entity on Scene.");
                ImGui.End();
                return;
            }

            // ПЕРЕИМЕНОВАНИЕ ОБЪЕКТА
            string objName = state.SelectedEntity.Name;
            if (ImGui.InputText("##ObjectName", ref objName, 64))
            {
                state.SelectedEntity.Name = objName;
            }

            bool isUI = state.SelectedEntity.IsUI;
            if (ImGui.Checkbox("Is UI (HUD Element)", ref isUI)) 
            {
                state.SelectedEntity.IsUI = isUI;
            }

            bool alwaysActive = state.SelectedEntity.AlwaysActive;
            if (ImGui.Checkbox("Always Active", ref alwaysActive))
            {
                state.SelectedEntity.AlwaysActive = alwaysActive;
            }
            
            ImGui.Separator();

            DrawTransformHeader(state.SelectedEntity);

            Component? componentToRemove = null;

            foreach (var component in state.SelectedEntity.GetAllComponents())
            {
                if (ImGui.CollapsingHeader(component.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.Button($"Remove {component.GetType().Name}", new Vector2(-1, 20)))
                    {
                        componentToRemove = component;
                    }
                    ImGui.Spacing();

                    if (component is LightComponent light)
                        LightingWindow.DrawLightInspector(light, ctx.CurrentScene);
                    else
                        DrawComponentProperties(component);

                    // Если это Спрайт, рисуем его кастомные поля
                    if (component is SpriteComponent sprite)
                    {
                        // ==========================================
                            // ПАНЕЛЬ ПАРАМЕТРОВ (Теперь только для ЧТЕНИЯ / ТЕСТА)
                            // ==========================================
                            ImGui.Separator();
                            ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.8f, 0.8f, 1.0f), "Animator Parameters (Auto-detected)");
                            
                            foreach (var kvp in sprite.Parameters)
                            {
                                var p = kvp.Value;
                                if (p.Type == ParameterType.Float)
                                {
                                    ImGui.Text($"{p.Name}: {p.FloatValue:F2}");
                                }
                                else if (p.Type == ParameterType.Bool)
                                {
                                    ImGui.Text($"{p.Name}: {p.BoolValue}");
                                }
                                else if (p.Type == ParameterType.Trigger)
                                {
                                    ImGui.Text($"{p.Name}: Trigger");
                                    ImGui.SameLine(ImGui.GetWindowWidth() - 70);
                                    if (ImGui.Button($"Test##{p.Name}")) p.TriggerValue = true; // Оставили только кнопку теста!
                                }
                            }

                            // ==========================================
                            // БЛОК СОСТОЯНИЙ (Остался без изменений)
                            // ==========================================
                            ImGui.Separator();
                            ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1.0f), "Animation States");

                            string? stateToRemove = null;
                            foreach (var stateName in new List<string>(sprite.Animations.Keys))
                            {
                                var animState = sprite.Animations[stateName];
                                if (ImGui.TreeNodeEx(stateName, ImGuiTreeNodeFlags.Framed))
                                {
                                    string texPath = animState.TexturePath;
                                    if (ImGui.BeginCombo($"Texture##{stateName}", texPath))
                                    {
                                        // ИСПРАВЛЕНИЕ: Локальная переменная для ref
                                        string filter = state.TextureSearchFilter;
                                        if (ImGui.InputText("Search", ref filter, 100)) state.TextureSearchFilter = filter;
                                        
                                        if (Directory.Exists("assets"))
                                        {
                                            var files = Directory.GetFiles("assets", "*.png", SearchOption.AllDirectories);
                                            foreach (var file in files)
                                            {
                                                string normPath = file.Replace("\\", "/");
                                                if (!string.IsNullOrEmpty(state.TextureSearchFilter) && !normPath.Contains(state.TextureSearchFilter, StringComparison.OrdinalIgnoreCase)) continue;
                                                if (ImGui.Selectable(normPath, texPath == normPath))
                                                {
                                                    animState.TexturePath = normPath;
                                                    sprite.Play(stateName, true); 
                                                }
                                            }
                                        }
                                        ImGui.EndCombo();
                                    }

                                    int fw = animState.FrameWidth;
                                    if (ImGui.DragInt($"FrameWidth##{stateName}", ref fw, 1)) { animState.FrameWidth = fw; sprite.Play(stateName, true); }

                                    float spd = animState.AnimationSpeed;
                                    if (ImGui.DragFloat($"Speed##{stateName}", ref spd, 0.1f)) { animState.AnimationSpeed = spd; sprite.Play(stateName, true); }

                                    bool loop = animState.IsLooping;
                                    if (ImGui.Checkbox($"Loop##{stateName}", ref loop)) { animState.IsLooping = loop; sprite.Play(stateName, true); }
                                    if (ImGui.Button($"Play State##{stateName}")) sprite.Play(stateName, true);
                                    ImGui.SameLine();
                                    if (ImGui.Button($"Delete##{stateName}")) stateToRemove = stateName;

                                    ImGui.Spacing();
                                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "Audio Triggers (Frame -> SFX Key):");

                                    int frameToRemove = -1;
                                    foreach (var kvp in animState.SoundEvents)
                                    {
                                        ImGui.PushID($"se_{stateName}_{kvp.Key}");
                                        ImGui.Text($"Frame {kvp.Key}  ->  {kvp.Value}");
                                        
                                        ImGui.SameLine(180);
                                        if (ImGui.Button("X")) frameToRemove = kvp.Key;
                                        ImGui.PopID();
                                    }
                                    if (frameToRemove != -1) animState.SoundEvents.Remove(frameToRemove);

                                    // Блок добавления нового триггера
                                    int targetFrame = state.NewAudioFrame;
                                    ImGui.SetNextItemWidth(50);
                                    if (ImGui.InputInt("Frame##SE", ref targetFrame, 0)) state.NewAudioFrame = targetFrame;

                                    ImGui.SameLine();
                                    string targetKey = state.NewAudioSfxKey;
                                    ImGui.SetNextItemWidth(70);
                                    if (ImGui.InputText("Key##SE", ref targetKey, 32)) state.NewAudioSfxKey = targetKey;

                                    ImGui.SameLine();
                                    if (ImGui.Button("+ Link Sound") && !string.IsNullOrWhiteSpace(state.NewAudioSfxKey))
                                    {
                                        // Привязываем введенный ключ (например, "Hit") к нужному кадру
                                        animState.SoundEvents[state.NewAudioFrame] = state.NewAudioSfxKey;
                                    }
                                    ImGui.Separator();

                                    // Интерфейс для настройки Переходов
                                    ImGui.Spacing();
                                    ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.4f, 1.0f), "Transitions:");
                                    
                                    for (int j = 0; j < animState.Transitions.Count; j++)
                                    {
                                        var trans = animState.Transitions[j];
                                        ImGui.PushID($"trans_{stateName}_{j}");
                                        
                                        ImGui.SetNextItemWidth(60);
                                        string pName = trans.ParameterName;
                                        if (ImGui.InputText("##Param", ref pName, 32)) trans.ParameterName = pName;
                                        
                                        ImGui.SameLine(); ImGui.SetNextItemWidth(30);
                                        string op = trans.ConditionOperator;
                                        if (ImGui.InputText("##Op", ref op, 4)) trans.ConditionOperator = op;
                                        
                                        ImGui.SameLine(); ImGui.SetNextItemWidth(40);
                                        float thresh = trans.Threshold;
                                        if (ImGui.DragFloat("##Val", ref thresh, 0.1f)) trans.Threshold = thresh;
                                        
                                        ImGui.SameLine(); ImGui.Text("->");
                                        
                                        ImGui.SameLine(); ImGui.SetNextItemWidth(60);
                                        string target = trans.TargetState;
                                        if (ImGui.InputText("##Target", ref target, 32)) trans.TargetState = target;

                                        ImGui.SameLine();
                                        if (ImGui.Button("X")) { animState.Transitions.RemoveAt(j); j--; }
                                        
                                        ImGui.PopID();
                                    }

                                    if (ImGui.Button($"+ Add Rule##{stateName}"))
                                    {
                                        animState.Transitions.Add(new AnimationTransition { ConditionOperator = ">" });
                                    }
                                    ImGui.Spacing();
                                    ImGui.Separator();

                                    ImGui.TreePop();
                                }
                            }
                            if (stateToRemove != null) sprite.Animations.Remove(stateToRemove);

                            ImGui.Spacing();
                            
                            // ИСПРАВЛЕНИЕ: Локальная переменная для ref
                            string newState = state.NewStateName;
                            if (ImGui.InputText("##NewState", ref newState, 64)) state.NewStateName = newState;
                            
                            ImGui.SameLine();
                            if (ImGui.Button("Add State"))
                            {
                                if (!string.IsNullOrWhiteSpace(state.NewStateName) && !sprite.Animations.ContainsKey(state.NewStateName))
                                {
                                    sprite.Animations[state.NewStateName] = new AnimationState();
                                    state.NewStateName = "";
                                }
                            }
                    }
                    // =========================================================
                    // КАСТОМНЫЙ ИНТЕРФЕЙС ДЛЯ AUDIO SOURCE COMPONENT
                    // =========================================================
                    if (component is AudioSourceComponent audio)
                    {
                        ImGui.Separator();
                        ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "Audio File Selectors");

                        // 1. ВЫПАДАЛКА ДЛЯ ФОНОВОЙ МУЗЫКИ (DefaultSoundPath)
                        ImGui.Text("Background / Default Track:");
                        string currentPath = audio.DefaultSoundPath;
                        if (ImGui.BeginCombo("##AudioDefaultCombo", string.IsNullOrEmpty(currentPath) ? "None" : currentPath))
                        {
                            string filter = state.AudioSearchFilter;
                            if (ImGui.InputText("Search##Audio", ref filter, 100)) state.AudioSearchFilter = filter;

                            if (System.IO.Directory.Exists("assets"))
                            {
                                // Ищем все аудиофайлы (wav, ogg, mp3)
                                var files = System.IO.Directory.GetFiles("assets", "*.*", System.IO.SearchOption.AllDirectories)
                                    .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || 
                                                f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || 
                                                f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase));

                                foreach (var file in files)
                                {
                                    string normPath = file.Replace("\\", "/");
                                    if (!string.IsNullOrEmpty(state.AudioSearchFilter) && !normPath.Contains(state.AudioSearchFilter, StringComparison.OrdinalIgnoreCase)) 
                                        continue;

                                    if (ImGui.Selectable(normPath, currentPath == normPath))
                                    {
                                        audio.DefaultSoundPath = normPath;
                                    }
                                }
                            }
                            ImGui.EndCombo();
                        }

                        // Кнопка теста для фонового трека
                        ImGui.SameLine();
                        if (ImGui.Button("Play##TestBg") && !string.IsNullOrEmpty(audio.DefaultSoundPath))
                        {
                            if (audio.IsMusic) AudioLocator.Get().PlayMusic(audio.DefaultSoundPath, audio.Volume);
                            else AudioLocator.Get().PlaySFX(audio.DefaultSoundPath, audio.Owner.Position, audio.Volume);
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("Stop##TestBg"))
                        {
                            AudioLocator.Get().StopMusic();
                            audio.ResetPlayState(); // Сбрасываем флаг компонента
                        }

                        // 2. СЛОВАРЬ ЗВУКОВЫХ ЭФФЕКТОВ (SFX)
                        ImGui.Spacing();
                        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.2f, 1.0f), "Sound Effects Dictionary (SFX):");

                        string? sfxToRemove = null;
                        var sfxKeys = new List<string>(audio.SoundClips.Keys);

                        foreach (var key in sfxKeys)
                        {
                            ImGui.PushID($"sfx_{key}");
                            ImGui.Text(key);
                            ImGui.SameLine(100);

                            string clipPath = audio.SoundClips[key];
                            ImGui.SetNextItemWidth(150);
                            
                            // Индивидуальная выпадалка для каждого SFX
                            if (ImGui.BeginCombo("##SfxCombo", string.IsNullOrEmpty(clipPath) ? "None" : clipPath))
                            {
                                string filter = state.AudioSearchFilter;
                                if (ImGui.InputText("Search##Sfx", ref filter, 100)) state.AudioSearchFilter = filter;

                                if (System.IO.Directory.Exists("assets"))
                                {
                                    var files = System.IO.Directory.GetFiles("assets", "*.*", System.IO.SearchOption.AllDirectories)
                                        .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || 
                                                    f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || 
                                                    f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase));
                                    foreach (var file in files)
                                    {
                                        string normPath = file.Replace("\\", "/");
                                        if (!string.IsNullOrEmpty(state.AudioSearchFilter) && !normPath.Contains(state.AudioSearchFilter, StringComparison.OrdinalIgnoreCase)) 
                                            continue;

                                        if (ImGui.Selectable(normPath, clipPath == normPath))
                                        {
                                            audio.SoundClips[key] = normPath;
                                        }
                                    }
                                }
                                ImGui.EndCombo();
                            }

                            ImGui.SameLine();
                            if (ImGui.Button("▶")) audio.PlayOneShot(key); // Быстрый тест SFX

                            ImGui.SameLine();
                            if (ImGui.Button("X")) sfxToRemove = key;

                            ImGui.PopID();
                        }

                        if (sfxToRemove != null) audio.SoundClips.Remove(sfxToRemove);

                        // ДОБАВЛЕНИЕ НОВОГО КЛЮЧА SFX (Например: "Hit", "Jump")
                        ImGui.Spacing();
                        string newKey = state.NewAudioKey;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.InputText("##NewSfxKey", ref newKey, 32)) state.NewAudioKey = newKey;
                        
                        ImGui.SameLine();
                        if (ImGui.Button("+ Add SFX Key") && !string.IsNullOrWhiteSpace(state.NewAudioKey))
                        {
                            if (!audio.SoundClips.ContainsKey(state.NewAudioKey))
                            {
                                audio.SoundClips[state.NewAudioKey] = "";
                                state.NewAudioKey = "";
                            }
                        }
                        ImGui.Separator();
                    }
                }
            }

            if (componentToRemove != null) state.SelectedEntity.RemoveComponent(componentToRemove);

            DrawAddComponentPopup(ctx, state.SelectedEntity);

            ImGui.End();
        }

        private void DrawTransformHeader(GameObject entity)
        {
            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                Vector2 pos = new Vector2(entity.Position.X, entity.Position.Y);
                if (ImGui.DragFloat2("Position", ref pos, 0.05f)) entity.Position = new Vector2D<float>(pos.X, pos.Y);

                Vector2 scale = new Vector2(entity.Scale.X, entity.Scale.Y);
                if (ImGui.DragFloat2("Scale", ref scale, 0.05f)) entity.Scale = new Vector2D<float>(scale.X, scale.Y);

                float rot = entity.Rotation;
                if (ImGui.DragFloat("Rotation", ref rot, 1f)) entity.Rotation = rot;
            }
        }

        private void DrawComponentProperties(Component component)
        {
            Type compType = component.GetType();

            // Кешуємо PropertyInfo[], щоб не викликати Reflection щокадру
            if (!_propertyCache.TryGetValue(compType, out var properties))
            {
                properties = compType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                _propertyCache[compType] = properties;
            }

            foreach (var prop in properties)
            {
                if (prop.GetSetMethod() == null) continue;
                if (prop.IsDefined(typeof(SceneTransientAttribute), true)) continue;
                
                if (prop.Name == "AnimationsJson" || prop.Name == "ParametersJson" || prop.Name == "FrameWidth" || prop.Name == "AnimationSpeed" || prop.Name == "SoundClipsJson" || prop.Name == "DefaultSoundPath") continue;

                var value = prop.GetValue(component);
                if (value == null) continue;

                if (value is float fValue)
                {
                    if (ImGui.DragFloat(prop.Name, ref fValue, 0.1f)) prop.SetValue(component, fValue);
                }
                else if (value is bool bValue)
                {
                    if (ImGui.Checkbox(prop.Name, ref bValue)) prop.SetValue(component, bValue);
                }
                else if (value is int iValue)
                {
                    if (ImGui.DragInt(prop.Name, ref iValue, 1)) prop.SetValue(component, iValue);
                }
                else if (value is Vector2D<float> vValue)
                {
                    Vector2 vec = new Vector2(vValue.X, vValue.Y);
                    if (ImGui.DragFloat2(prop.Name, ref vec, 0.1f)) prop.SetValue(component, new Vector2D<float>(vec.X, vec.Y));
                }
                else if (value is Vector4D<float> v4Value)
                {
                    Vector4 col = new Vector4(v4Value.X, v4Value.Y, v4Value.Z, v4Value.W);
                    if (ImGui.ColorEdit4(prop.Name, ref col))
                    {
                        prop.SetValue(component, new Vector4D<float>(col.X, col.Y, col.Z, col.W));
                    }
                }
                else if (value is TransitionSettings transition)
                {
                    if (ImGui.TreeNode(prop.Name))
                    {
                        string currentKind = transition.Kind.ToString();
                        if (ImGui.BeginCombo("Effect", currentKind))
                        {
                            foreach (TransitionKind kind in Enum.GetValues(typeof(TransitionKind)))
                            {
                                bool selected = transition.Kind == kind;
                                if (ImGui.Selectable(kind.ToString(), selected)) transition.Kind = kind;
                                if (selected) ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }

                        float duration = transition.Duration;
                        if (ImGui.DragFloat("Duration", ref duration, 0.05f, 0.05f, 10f))
                        {
                            transition.Duration = MathF.Max(0.05f, duration);
                        }

                        Vector4 color = new Vector4(transition.Color.X, transition.Color.Y, transition.Color.Z, transition.Color.W);
                        if (ImGui.ColorEdit4("Color", ref color))
                        {
                            transition.Color = new Vector4D<float>(color.X, color.Y, color.Z, color.W);
                        }

                        ImGui.TreePop();
                    }
                }
                else if (value is string sValue)
                {
                    if (ImGui.InputText(prop.Name, ref sValue, 256)) prop.SetValue(component, sValue);
                }
            }
        }

        private void DrawAddComponentPopup(EditorContext ctx, GameObject entity)
        {
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
            if (ImGui.Button("Add Component", new Vector2(-1, 30))) ImGui.OpenPopup("AddComponentPopup");

            if (ImGui.BeginPopup("AddComponentPopup"))
            {
                var componentTypes = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(Component)) && !t.IsAbstract);

                foreach (var type in componentTypes)
                {
                    if (entity.GetAllComponents().Any(c => c.GetType() == type)) continue;

                    if (ImGui.MenuItem(type.Name))
                    {
                        var newComponent = (Component)Activator.CreateInstance(type)!;
                        if (newComponent is PhysicsBody pb) 
                        {
                            pb.Map = ctx.CurrentScene.Map;
                            pb.HitboxSize = new Vector2D<float>(0.5f, 0.5f);
                        }
                        entity.AddComponentDynamic(newComponent, type);
                    }
                }
                ImGui.EndPopup();
            }
        }
    }
}
