using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.IO;
using System.Numerics;

namespace ef2dg
{
    public class TilemapEditorWindow : EditorWindow
    {
        public override string Title => "Tile Palette";

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(340, 750), ImGuiCond.FirstUseEver);
            ImGui.Begin(Title, ref IsOpen);

            var map = ctx.CurrentScene.Map;
            if (map == null || map.Layers == null || map.Layers.Count == 0) 
            { 
                ImGui.TextDisabled("Map or layers not found"); ImGui.End(); return; 
            }

            if (state.SelectedTileLayerIndex < 0 || state.SelectedTileLayerIndex >= map.Layers.Count)
                state.SelectedTileLayerIndex = 0;

            int tool = (int)state.CurrentTool;
            ImGui.RadioButton("Select", ref tool, (int)EditorState.ToolMode.Select); ImGui.SameLine();
            ImGui.RadioButton("Paint", ref tool, (int)EditorState.ToolMode.PaintTiles); ImGui.SameLine();
            ImGui.RadioButton("Erase", ref tool, (int)EditorState.ToolMode.EraseTiles);
            state.CurrentTool = (EditorState.ToolMode)tool;
            ImGui.Separator();

            ImGui.Text("Active Layer:");
            if (ImGui.BeginCombo("##LayerCombo", map.Layers[state.SelectedTileLayerIndex].Name))
            {
                for (int i = 0; i < map.Layers.Count; i++)
                    if (ImGui.Selectable(map.Layers[i].Name, state.SelectedTileLayerIndex == i)) state.SelectedTileLayerIndex = i;
                ImGui.EndCombo();
            }
            
            var activeLayer = map.Layers[state.SelectedTileLayerIndex];
            bool hasCol = activeLayer.HasCollision;
            if (ImGui.Checkbox("Layer Physics Enabled", ref hasCol)) activeLayer.HasCollision = hasCol;
            ImGui.Separator();

            ImGui.Text("Tiles from assets/Tiles:");
            if (Directory.Exists("assets/Tiles"))
            {
                var files = Directory.GetFiles("assets/Tiles", "*.png");
                for (int i = 0; i < files.Length; i++)
                {
                    string filePath = files[i].Replace("\\", "/");
                    var tex = Engine.Resources.GetTexture(filePath);

                    ImGui.PushID(i);
                    if (state.SelectedTilePath == filePath) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.8f, 0.2f, 1.0f));
                    else ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));

                    if (ImGui.ImageButton($"tile_{i}", (IntPtr)tex.Handle, new Vector2(50, 50)))
                    {
                        state.SelectedTilePath = filePath;
                        state.CurrentTool = EditorState.ToolMode.PaintTiles;
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopID();
                    if ((i + 1) % 4 != 0) ImGui.SameLine();
                }
                
                if (!string.IsNullOrEmpty(state.SelectedTilePath))
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), $"Tile Settings: {Path.GetFileName(state.SelectedTilePath)}");
                    
                    if (!map.TileConfigs.TryGetValue(state.SelectedTilePath, out var config))
                    {
                        var tex = Engine.Resources.GetTexture(state.SelectedTilePath);
                        config = new TileSettings { TextureScale = new Vector2D<float>(tex.Width / map.BasePixelSize, tex.Height / map.BasePixelSize) };
                        map.TileConfigs[state.SelectedTilePath] = config;
                    }

                    // IMAGE SETTINGS
                    System.Numerics.Vector2 tScale = new System.Numerics.Vector2(config.TextureScale.X, config.TextureScale.Y);
                    if (ImGui.DragFloat2("Texture Scale", ref tScale, 0.05f)) config.TextureScale = new Vector2D<float>(tScale.X, tScale.Y);

                    System.Numerics.Vector2 tOffset = new System.Numerics.Vector2(config.TextureOffset.X, config.TextureOffset.Y);
                    if (ImGui.DragFloat2("Texture Offset", ref tOffset, 0.05f)) config.TextureOffset = new Vector2D<float>(tOffset.X, tOffset.Y);

                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.8f, 1.0f), "Animation Settings:");
                    int fCount = config.FrameCount;
                    if (ImGui.DragInt("Frame Count", ref fCount, 1, 1, 32)) config.FrameCount = fCount;
                    float aSpeed = config.AnimationSpeed;
                    if (ImGui.DragFloat("Anim Speed", ref aSpeed, 0.1f, 0f, 60f)) config.AnimationSpeed = aSpeed;
                    ImGui.Spacing();

                    // Flip
                    bool fX = config.FlipX;
                    if (ImGui.Checkbox("Flip X", ref fX)) config.FlipX = fX;
                    ImGui.SameLine();
                    bool fY = config.FlipY;
                    if (ImGui.Checkbox("Flip Y", ref fY)) config.FlipY = fY;

                    ImGui.Spacing();

                    // COLLISION SETTINGS
                    int colType = (int)config.CollisionType;
                    string[] colNames = { "None (Ghost)", "Solid (Square)", "Slope Left (\\)", "Slope Right (/)" };
                    if (ImGui.BeginCombo("Tile Collision", colNames[colType]))
                    {
                        for (int i = 0; i < colNames.Length; i++)
                            if (ImGui.Selectable(colNames[i], colType == i)) config.CollisionType = (TileCollisionType)i;
                        ImGui.EndCombo();
                    }

                    if (config.CollisionType == TileCollisionType.Solid)
                    {
                        System.Numerics.Vector2 hScale = new System.Numerics.Vector2(config.HitboxScale.X, config.HitboxScale.Y);
                        if (ImGui.DragFloat2("Hitbox Scale", ref hScale, 0.05f)) config.HitboxScale = new Vector2D<float>(hScale.X, hScale.Y);

                        System.Numerics.Vector2 hOffset = new System.Numerics.Vector2(config.HitboxOffset.X, config.HitboxOffset.Y);
                        if (ImGui.DragFloat2("Hitbox Offset", ref hOffset, 0.05f)) config.HitboxOffset = new Vector2D<float>(hOffset.X, hOffset.Y);
                    }
                    else if (config.CollisionType != TileCollisionType.None)
                    {
                        ImGui.TextDisabled("Slopes are strictly locked to grid size.");
                    }
                }
            }
            ImGui.End();
        }
    }
}