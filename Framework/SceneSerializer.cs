using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Silk.NET.Maths;

namespace ef2dg
{
    public class TileLayerData
    {
        public string? Name { get; set; } = "";
        public bool HasCollision { get; set; } = false;
        
        public Dictionary<string, string> Grid { get; set; } = new();
    }

    public class TileSettingsData
    {
        public float TexScaleX { get; set; } = 1f;
        public float TexScaleY { get; set; } = 1f;
        public float TexOffsetX { get; set; } = 0f;
        public float TexOffsetY { get; set; } = 0f;
        public bool FlipX { get; set; } = false;
        public bool FlipY { get; set; } = false;
        public float HitboxScaleX { get; set; } = 1f;
        public float HitboxScaleY { get; set; } = 1f;
        public float HitboxOffsetX { get; set; } = 0f;
        public float HitboxOffsetY { get; set; } = 0f;
        public int CollisionType { get; set; } = 1;
        public int FrameCount { get; set; } = 1;
        public float AnimationSpeed { get; set; } = 5f;
    }

    public class LevelData
    {
        public string SceneType { get; set; } = "GameScene";
        public string InstanceName { get; set; } = "";
        public List<EntityData> Entities { get; set; } = new();
        public List<TileLayerData> TileLayers { get; set; } = new();
        public Dictionary<string, float[]>? TextureScales { get; set; } 
        public Dictionary<string, TileSettingsData> TileConfigs { get; set; } = new();
        // null у уровней, сохранённых до появления освещения по сценам
        public SceneLighting? Lighting { get; set; }
    }

    public class ComponentData
    {
        public string? TypeName { get; set; } = "";
        public Dictionary<string, float> FloatValues { get; set; } = new();
        public Dictionary<string, bool> BoolValues { get; set; } = new();
        public Dictionary<string, float[]> VectorValues { get; set; } = new();
        public Dictionary<string, string> StringValues { get; set; } = new();
        public Dictionary<string, int> IntValues { get; set; } = new();
        public Dictionary<string, TransitionSettingsData> TransitionValues { get; set; } = new();
    }

    public class TransitionSettingsData
    {
        public int Kind { get; set; }
        public float Duration { get; set; } = 0.5f;
        public float[] Color { get; set; } = new float[] { 0f, 0f, 0f, 1f };
    }

    public class EntityData
    {
        public string? Name { get; set; } = "New GameObject";
        public float X { get; set; }
        public float Y { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float Rotation { get; set; }
        public bool IsUI { get; set; }
        public bool AlwaysActive { get; set; }
        public List<ComponentData> Components { get; set; } = new(); 
    }

    public static class SceneSerializer
    {
        // IncludeFields нужен для System.Numerics.Vector3 в SceneLighting (X, Y, Z — поля, а не свойства)
        private static readonly JsonSerializerOptions SaveOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        private static readonly JsonSerializerOptions LoadOptions = new JsonSerializerOptions { IncludeFields = true };

        public static void Save(Scene scene, string filePath)
        {
            var data = new LevelData();

            data.SceneType = scene.GetType().Name;
            data.InstanceName = scene.InstanceName;
            data.Lighting = scene.Lighting;
            
            foreach (var entity in scene.Entities)
            {
                var ed = new EntityData
                {
                    Name = entity.Name,
                    X = entity.Position.X,
                    Y = entity.Position.Y,
                    ScaleX = entity.Scale.X,
                    ScaleY = entity.Scale.Y,
                    Rotation = entity.Rotation,
                    IsUI = entity.IsUI,
                    AlwaysActive = entity.AlwaysActive
                };

                foreach (var comp in entity.GetAllComponents())
                {
                    var cd = new ComponentData { TypeName = comp.GetType().Name };
                    var properties = comp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var prop in properties)
                    {
                        if (prop.GetSetMethod() == null) continue; 
                        if (prop.IsDefined(typeof(SceneTransientAttribute), true)) continue;
                        var value = prop.GetValue(comp);

                        if (value is float f) cd.FloatValues[prop.Name] = f;
                        else if (value is bool b) cd.BoolValues[prop.Name] = b;
                        else if (value is int iValue) cd.IntValues[prop.Name] = iValue;
                        else if (value is Vector2D<float> vec)
                        {
                            cd.VectorValues[prop.Name] = new float[] { vec.X, vec.Y };
                        }
                        else if (value is Vector4D<float> vec4)
                        {
                            cd.VectorValues[prop.Name] = new float[] { vec4.X, vec4.Y, vec4.Z, vec4.W };
                        }
                        else if (value is TransitionSettings transition)
                        {
                            cd.TransitionValues[prop.Name] = new TransitionSettingsData
                            {
                                Kind = (int)transition.Kind,
                                Duration = transition.Duration,
                                Color = new float[] { transition.Color.X, transition.Color.Y, transition.Color.Z, transition.Color.W }
                            };
                        }
                        else if (value is string s) cd.StringValues[prop.Name] = s;
                    }
                    ed.Components.Add(cd);
                }
                data.Entities.Add(ed);
            }

            if (scene.Map != null)
            {
                foreach (var layer in scene.Map.Layers)
                {
                    var ld = new TileLayerData { Name = layer.Name, HasCollision = layer.HasCollision };
                    foreach (var kvp in layer.Grid) ld.Grid[$"{kvp.Key.X},{kvp.Key.Y}"] = kvp.Value;
                    data.TileLayers.Add(ld);
                }
                
                foreach (var kvp in scene.Map.TileConfigs)
                {
                    data.TileConfigs[kvp.Key] = new TileSettingsData {
                        FrameCount = kvp.Value.FrameCount,
                        AnimationSpeed = kvp.Value.AnimationSpeed,
                        TexScaleX = kvp.Value.TextureScale.X, TexScaleY = kvp.Value.TextureScale.Y,
                        TexOffsetX = kvp.Value.TextureOffset.X, TexOffsetY = kvp.Value.TextureOffset.Y,
                        FlipX = kvp.Value.FlipX, FlipY = kvp.Value.FlipY,
                        HitboxScaleX = kvp.Value.HitboxScale.X, HitboxScaleY = kvp.Value.HitboxScale.Y,
                        HitboxOffsetX = kvp.Value.HitboxOffset.X, HitboxOffsetY = kvp.Value.HitboxOffset.Y,
                        CollisionType = (int)kvp.Value.CollisionType
                    };
                }
            }
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, SaveOptions));
            scene.FilePath = filePath;
        }

        public static bool Load(Scene scene, string filePath, ResourceManager resources)
        {
            if (!File.Exists(filePath)) return false;
            try
            {
                var data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath), LoadOptions);
                if (data == null) return false;

                if (!string.IsNullOrEmpty(data.InstanceName))
                {
                    scene.InstanceName = data.InstanceName;
                }

                scene.Lighting = data.Lighting ?? SceneLighting.CreateLegacy(scene);
                scene.FilePath = filePath;

                scene.Entities.Clear();

                if (scene.Map != null)
                {
                    scene.Map.Layers.Clear();
                    foreach (var ld in data.TileLayers)
                    {
                        var layer = new TileLayer { Name = ld.Name ?? "Layer", HasCollision = ld.HasCollision };
                        foreach (var kvp in ld.Grid)
                        {
                            var p = kvp.Key.Split(',');
                            layer.Grid[new Vector2D<int>(int.Parse(p[0]), int.Parse(p[1]))] = kvp.Value ?? "";
                        }
                        scene.Map.Layers.Add(layer);
                    }

                    bool hasForeground = false;
                    foreach (var l in scene.Map.Layers) if (l.Name == "Foreground") hasForeground = true;
                    if (!hasForeground)
                    {
                        scene.Map.Layers.Add(new TileLayer { Name = "Foreground", HasCollision = false });
                    }
                    
                    scene.Map.TileConfigs.Clear();
                    if (data.TileConfigs != null && data.TileConfigs.Count > 0) 
                    {
                        foreach (var kvp in data.TileConfigs)
                        {
                            scene.Map.TileConfigs[kvp.Key] = new TileSettings {
                                FrameCount = kvp.Value.FrameCount,
                                AnimationSpeed = kvp.Value.AnimationSpeed,
                                TextureScale = new Vector2D<float>(kvp.Value.TexScaleX, kvp.Value.TexScaleY),
                                TextureOffset = new Vector2D<float>(kvp.Value.TexOffsetX, kvp.Value.TexOffsetY),
                                FlipX = kvp.Value.FlipX, FlipY = kvp.Value.FlipY,
                                HitboxScale = new Vector2D<float>(kvp.Value.HitboxScaleX, kvp.Value.HitboxScaleY),
                                HitboxOffset = new Vector2D<float>(kvp.Value.HitboxOffsetX, kvp.Value.HitboxOffsetY),
                                CollisionType = (TileCollisionType)kvp.Value.CollisionType
                            };
                        }
                    }
                    else if (data.TextureScales != null)
                    {
                        foreach (var kvp in data.TextureScales)
                        {
                            scene.Map.TileConfigs[kvp.Key] = new TileSettings { TextureScale = new Vector2D<float>(kvp.Value[0], kvp.Value[1]) };
                        }
                    }
                }

                foreach (var ed in data.Entities)
                {
                    var entity = new GameObject();
                    entity.ParentScene = scene;
                    entity.Name = ed.Name ?? "New GameObject";
                    entity.Position = new Vector2D<float>(ed.X, ed.Y);
                    entity.Scale = new Vector2D<float>(ed.ScaleX, ed.ScaleY);
                    entity.Rotation = ed.Rotation;
                    entity.IsUI = ed.IsUI;
                    entity.AlwaysActive = ed.AlwaysActive;

                    foreach (var cd in ed.Components)
                    {
                        if (string.IsNullOrEmpty(cd.TypeName)) continue; 
                        Type? compType = Type.GetType($"ef2dg.{cd.TypeName}");
                        if (compType == null) continue;

                        var targetComp = (Component)Activator.CreateInstance(compType)!;
                        entity.AddComponentDynamic(targetComp, compType);

                        var properties = targetComp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in properties)
                        {
                            if (prop.GetSetMethod() == null) continue;
                            if (prop.IsDefined(typeof(SceneTransientAttribute), true)) continue;
                            switch (prop.PropertyType)
                            {
                                case Type t when t == typeof(float):
                                    if (cd.FloatValues.TryGetValue(prop.Name, out float f)) prop.SetValue(targetComp, f);
                                    break;
                                case Type t when t == typeof(bool):
                                    if (cd.BoolValues.TryGetValue(prop.Name, out bool b)) prop.SetValue(targetComp, b);
                                    break;
                                case Type t when t == typeof(int):
                                    if (cd.IntValues.TryGetValue(prop.Name, out int val)) prop.SetValue(targetComp, val);
                                    break;
                                case Type t when t == typeof(Vector2D<float>):
                                    if (cd.VectorValues.TryGetValue(prop.Name, out float[]? v) && v != null)
                                        prop.SetValue(targetComp, new Vector2D<float>(v[0], v[1]));
                                    break;
                                case Type t when t == typeof(Vector4D<float>):
                                    if (cd.VectorValues.TryGetValue(prop.Name, out float[]? v4) && v4 != null && v4.Length >= 4)
                                        prop.SetValue(targetComp, new Vector4D<float>(v4[0], v4[1], v4[2], v4[3]));
                                    break;
                                case Type t when t == typeof(string):
                                    if (cd.StringValues.TryGetValue(prop.Name, out string? s) && s != null)
                                        prop.SetValue(targetComp, s);
                                    break;
                                case Type t when t == typeof(TransitionSettings):
                                    if (cd.TransitionValues.TryGetValue(prop.Name, out TransitionSettingsData? transitionData) && transitionData != null)
                                    {
                                        float[] color = transitionData.Color ?? new float[] { 0f, 0f, 0f, 1f };
                                        if (color.Length < 4) color = new float[] { 0f, 0f, 0f, 1f };
                                        prop.SetValue(targetComp, new TransitionSettings
                                        {
                                            Kind = Enum.IsDefined(typeof(TransitionKind), transitionData.Kind)
                                                ? (TransitionKind)transitionData.Kind
                                                : TransitionKind.Instant,
                                            Duration = MathF.Max(0f, transitionData.Duration),
                                            Color = new Vector4D<float>(color[0], color[1], color[2], color[3])
                                        });
                                    }
                                    break;
                            }
                        }
                    }

                    var phys = entity.GetComponent<PhysicsBody>();
                    if (phys != null && scene.Map != null) phys.Map = scene.Map;

                    var sprite = entity.GetComponent<SpriteComponent>();
                    if (sprite != null && sprite.Animations.Count > 0)
                    {
                        string defaultAnim = sprite.Animations.ContainsKey("Idle") ? "Idle" : new List<string>(sprite.Animations.Keys)[0];
                        sprite.Play(defaultAnim, true);
                    }

                    if (scene is GameScene gs && entity.GetComponent<PlayerController>() != null)
                    {
                        gs.Player = entity;
                    }

                    scene.Entities.Add(entity);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SceneSerializer] Load error: {ex.Message}");
                return false;
            }
        }
    }
}
