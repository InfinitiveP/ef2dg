using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace ef2dg
{
    public class SceneViewWindow : EditorWindow
    {
        public override string Title => "Scene View";

        private static bool _isDraggingInViewport = false;
        private Vector2 _viewportScreenPosition;
        private Vector2 _viewportSize;
        private bool _hasViewport;

        // The window was closed through the View menu: the game cursor must not count as inside the old viewport
        public void MarkHidden() => _hasViewport = false;

        public void UpdateRuntimePointer(EditorState state, Camera camera)
        {
            if (!_hasViewport || _viewportSize.X <= 0f || _viewportSize.Y <= 0f)
            {
                InputManager.SetUiPointerState(default, default, false, false, false, false, 0f, 0f);
                return;
            }

            Vector2 mouse = ImGui.GetMousePos();
            float relX = mouse.X - _viewportScreenPosition.X;
            float relY = mouse.Y - _viewportScreenPosition.Y;
            bool inside = state.IsPlayMode && relX >= 0f && relX <= _viewportSize.X && relY >= 0f && relY <= _viewportSize.Y;
            float ndcX = (relX / _viewportSize.X) * 2f - 1f;
            float ndcY = 1f - (relY / _viewportSize.Y) * 2f;
            const float uiViewHeight = 2f;
            float uiViewWidth = uiViewHeight * camera.AspectRatio;
            var worldPosition = new Vector2D<float>(
                camera.Position.X + ndcX * camera.ViewWidth * 0.5f,
                camera.Position.Y + ndcY * camera.ViewHeight * 0.5f);
            InputManager.SetUiPointerState(
                new Vector2D<float>(ndcX * uiViewWidth * 0.5f, ndcY * uiViewHeight * 0.5f),
                worldPosition,
                inside,
                ImGui.IsMouseDown(ImGuiMouseButton.Left),
                ImGui.IsMouseDown(ImGuiMouseButton.Right),
                ImGui.IsMouseDown(ImGuiMouseButton.Middle),
                ImGui.GetIO().MouseWheel,
                ImGui.GetIO().MouseWheelH);
        }

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(300, 20), ImGuiCond.FirstUseEver);
            // Central area background like the VS Code editor
            ImGui.PushStyleColor(ImGuiCol.WindowBg, EditorTheme.EditorBackground);
            ImGui.Begin(Title, ref IsOpen);
            ImGui.PopStyleColor();

            // =========================================================
            // DYNAMIC SCENE REGISTRY (SCENE SELECTOR)
            // =========================================================
            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "Editor Viewport:");
            ImGui.SameLine();

            if (state.OpenedScenes.Count == 0 && ctx.CurrentScene != null)
            {
                state.OpenedScenes.Add(ctx.CurrentScene);
            }

            Scene? sceneToClose = null;

            for (int i = 0; i < state.OpenedScenes.Count; i++)
            {
                var scene = state.OpenedScenes[i];
                bool isActive = (ctx.CurrentScene == scene);

                if (isActive) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1.0f));
                
                string tabLabel = string.IsNullOrEmpty(scene.InstanceName) ? scene.GetType().Name : scene.InstanceName;
                
                if (ImGui.Button($"{tabLabel}##tab{i}"))
                {
                    if (!isActive) ctx.ChangeScene?.Invoke(scene);
                }
                if (isActive) ImGui.PopStyleColor();

                if (ImGui.BeginPopupContextItem($"Context##tab{i}"))
                {
                    ImGui.TextDisabled("Rename Scene:");
                    string newName = scene.InstanceName;
                    
                    if (ImGui.InputText($"##rename{i}", ref newName, 64))
                    {
                        scene.InstanceName = newName;
                    }
                    ImGui.EndPopup();
                }

                if (state.OpenedScenes.Count > 1)
                {
                    ImGui.SameLine(0, 2);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.2f, 0.2f, 0.5f));
                    if (ImGui.Button($"X##close{i}")) sceneToClose = scene;
                    ImGui.PopStyleColor();
                }
                ImGui.SameLine();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 1.0f));
            if (ImGui.Button("+##addScene"))
            {
                ImGui.OpenPopup("CreateScenePopup");
            }
            ImGui.PopStyleColor();

            if (sceneToClose != null)
            {
                state.OpenedScenes.Remove(sceneToClose);
                if (ctx.CurrentScene == sceneToClose && state.OpenedScenes.Count > 0)
                {
                    ctx.ChangeScene?.Invoke(state.OpenedScenes[0]);
                }
            }

            if (ImGui.BeginPopup("CreateScenePopup"))
            {
                ImGui.TextDisabled("Select Base Scene Template:");
                ImGui.Separator();

                foreach (var type in EditorCommands.GetSceneTypes())
                {
                    if (ImGui.MenuItem(type.Name)) EditorCommands.NewScene(ctx, state, type);
                }
                ImGui.EndPopup();
            }

            ImGui.Separator();
            // =========================================================
            
            var availableSize = ImGui.GetContentRegionAvail();
            if (availableSize.X > 0 && availableSize.Y > 0)
            {
                ctx.Fbo.Resize((int)availableSize.X, (int)availableSize.Y);
                var imageScreenPos = ImGui.GetCursorScreenPos();
                _viewportScreenPosition = imageScreenPos;
                _viewportSize = availableSize;
                _hasViewport = true;
                
                // 1. Render the game texture
                ImGui.Image((IntPtr)ctx.Fbo.TextureId, availableSize, new Vector2(0, 1), new Vector2(1, 0));
                
                var mousePos = ImGui.GetMousePos();
                float relX = mousePos.X - imageScreenPos.X;
                float relY = mousePos.Y - imageScreenPos.Y;

                bool isPlayMode = state.IsPlayMode;
                bool isHoveringViewport = false;

                // 2. MOUSE CAPTURE FOR THE VIEWPORT
                if (!isPlayMode)
                {
                    ImGui.SetCursorScreenPos(imageScreenPos);
                    ImGui.SetNextItemAllowOverlap();
                    ImGui.InvisibleButton("##ScenePickButton", availableSize);
                    isHoveringViewport = ImGui.IsItemHovered();
                }
                else
                {
                    isHoveringViewport = (relX >= 0 && relX <= availableSize.X && relY >= 0 && relY <= availableSize.Y);
                }

                if (isHoveringViewport && !isPlayMode)
                {
                    float wheel = ImGui.GetIO().MouseWheel;
                    if (wheel != 0)
                    {
                        // Zoom in/out by 10% per wheel "click"
                        ctx.GameCamera.Zoom -= wheel * 0.1f * ctx.GameCamera.Zoom;
                        
                        // Clamp between 0.1x (maximum zoom-in) and 20.0x (maximum zoom-out)
                        ctx.GameCamera.Zoom = MathF.Max(0.1f, MathF.Min(20.0f, ctx.GameCamera.Zoom));
                    }
                }

                if (isHoveringViewport && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _isDraggingInViewport = true;
                }
                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _isDraggingInViewport = false;
                }

                // 3. DRAWING UI BUTTONS (FULLY DATA-DRIVEN)
                if (ctx.CurrentScene != null)
                {
                    var endCursorPos = ImGui.GetCursorScreenPos();

                    void ExecuteButtonTransition(ButtonComponent b)
                    {
                        if (string.IsNullOrEmpty(b.TargetSceneName)) return;
                        ctx.RequestSceneTransition?.Invoke(b.TargetSceneName, b.IsOverlay, b.Transition);
                    }

                    foreach (var entity in ctx.CurrentScene.Entities)
                    {
                        var btn = entity.GetComponent<ButtonComponent>();
                        if (btn != null)
                        {
                            var btnScreenPos = new Vector2(imageScreenPos.X + entity.Position.X, imageScreenPos.Y + entity.Position.Y);
                            bool isBtnHovered = (relX >= entity.Position.X && relX <= entity.Position.X + btn.Size.X &&
                                                 relY >= entity.Position.Y && relY <= entity.Position.Y + btn.Size.Y);

                            // Update the component's hover state
                            btn.IsHovered = isHoveringViewport && isBtnHovered;

                            var sprite = entity.GetComponent<SpriteComponent>();
                            
                            // OPTION A: draw the SpriteComponent texture without touching the animation logic
                            if (sprite != null)
                            {
                                if (sprite.Texture != null)
                                {
                                    float uvWidth = sprite.FrameWidth > 0 ? (float)sprite.FrameWidth / sprite.Texture.Width : 1.0f;
                                    float uvOffsetX = sprite.CurrentFrame * uvWidth;
                                    Vector2 uv0 = new Vector2(uvOffsetX, 0.0f);
                                    Vector2 uv1 = new Vector2(uvOffsetX + uvWidth, 1.0f);

                                    ImGui.SetCursorScreenPos(btnScreenPos);
                                    ImGui.Image((IntPtr)sprite.Texture.Handle, new Vector2(btn.Size.X, btn.Size.Y), uv0, uv1);

                                    if (isPlayMode && btn.IsHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                                    {
                                        ExecuteButtonTransition(btn);
                                    }
                                }
                            }
                            // OPTION B: standard ImGui text button
                            else
                            {
                                ImGui.SetCursorScreenPos(btnScreenPos);
                                if (!isPlayMode)
                                {
                                    ImGui.BeginDisabled();
                                    ImGui.Button($"{btn.Label}##{entity.GetHashCode()}", new Vector2(btn.Size.X, btn.Size.Y));
                                    ImGui.EndDisabled();
                                }
                                else
                                {
                                    if (ImGui.Button($"{btn.Label}##{entity.GetHashCode()}", new Vector2(btn.Size.X, btn.Size.Y)))
                                    {
                                        ExecuteButtonTransition(btn);
                                    }
                                }
                            }
                        }
                    }
                    ImGui.SetCursorScreenPos(endCursorPos);
                }

                float ndcX = (relX / availableSize.X) * 2.0f - 1.0f;
                float ndcY = 1.0f - (relY / availableSize.Y) * 2.0f;
                float camWorldHeight = ctx.GameCamera.ViewHeight;
                float camWorldWidth = ctx.GameCamera.ViewWidth;

                float worldX = ctx.GameCamera.Position.X + ndcX * (camWorldWidth / 2.0f);
                float worldY = ctx.GameCamera.Position.Y + ndcY * (camWorldHeight / 2.0f);

                // 4. ENTITY SELECTION (EDITOR ONLY)
                if (isHoveringViewport && ctx.CurrentScene != null && !isPlayMode)
                {
                    if (state.CurrentTool == EditorState.ToolMode.Select)
                    {
                        state.HoveredGridCell = null;

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            GameObject? clickedObj = null;
                            for (int i = ctx.CurrentScene.Entities.Count - 1; i >= 0; i--)
                            {
                                var entity = ctx.CurrentScene.Entities[i];
                                var btn = entity.GetComponent<ButtonComponent>();

                                if (btn != null)
                                {
                                    float left = entity.Position.X;
                                    float top = entity.Position.Y;
                                    float right = left + btn.Size.X;
                                    float bottom = top + btn.Size.Y;

                                    if (relX >= left && relX <= right && relY >= top && relY <= bottom)
                                    {
                                        clickedObj = entity;
                                        break;
                                    }
                                }
                                else
                                {
                                    float width = 0.2f, height = 0.2f, offsetX = 0, offsetY = 0;
                                    var pb = entity.GetComponent<PhysicsBody>();
                                    if (pb != null)
                                    {
                                        width = pb.HitboxSize.X; height = pb.HitboxSize.Y;
                                        offsetX = pb.HitboxOffset.X; offsetY = pb.HitboxOffset.Y;
                                    }

                                    float left = entity.Position.X + offsetX - width / 2f;
                                    float right = entity.Position.X + offsetX + width / 2f;
                                    float top = entity.Position.Y + offsetY + height / 2f;
                                    float bottom = entity.Position.Y + offsetY - height / 2f;

                                    if (worldX >= left && worldX <= right && worldY >= bottom && worldY <= top)
                                    {
                                        clickedObj = entity;
                                        break;
                                    }
                                }
                            }
                            
                            state.SelectedEntity = clickedObj;
                            if (clickedObj != null)
                            {
                                if (clickedObj.GetComponent<ButtonComponent>() != null)
                                    state.DragOffset = new Vector2D<float>(clickedObj.Position.X - relX, clickedObj.Position.Y - relY);
                                else
                                    state.DragOffset = new Vector2D<float>(clickedObj.Position.X - worldX, clickedObj.Position.Y - worldY);
                            }
                        }
                    }
                    else if (state.CurrentTool == EditorState.ToolMode.PaintTiles || state.CurrentTool == EditorState.ToolMode.EraseTiles)
                    {
                        var map = ctx.CurrentScene.Map;
                        if (map != null)
                        {
                            int gridX = (int)MathF.Floor(worldX / map.TileSizeInWorld);
                            int gridY = (int)MathF.Floor(worldY / map.TileSizeInWorld);
                            var gridPos = new Vector2D<int>(gridX, gridY);
                            
                            state.HoveredGridCell = gridPos;

                            if (ImGui.IsMouseDown(ImGuiMouseButton.Left) && map.Layers.Count > state.SelectedTileLayerIndex)
                            {
                                var layer = map.Layers[state.SelectedTileLayerIndex];
                                string targetTilePath = state.CurrentTool == EditorState.ToolMode.PaintTiles ? state.SelectedTilePath : "";

                                layer.Grid.TryGetValue(gridPos, out string? currentPath);
                                if (currentPath != targetTilePath)
                                {
                                    var cmd = new PaintTileCommand(layer, gridPos, targetTilePath);
                                    state.History.ExecuteCommand(cmd);
                                }
                            }
                        }
                    }
                }
                else
                {
                    state.HoveredGridCell = null;
                }

                // 5. RELIABLE DRAGGING
                if (_isDraggingInViewport && state.SelectedEntity != null && state.CurrentTool == EditorState.ToolMode.Select && !isPlayMode)
                {
                    var btn = state.SelectedEntity.GetComponent<ButtonComponent>();
                    if (btn != null)
                    {
                        state.SelectedEntity.Position = new Vector2D<float>(relX + state.DragOffset.X, relY + state.DragOffset.Y);
                    }
                    else
                    {
                        state.SelectedEntity.Position = new Vector2D<float>(worldX + state.DragOffset.X, worldY + state.DragOffset.Y);
                        var pb = state.SelectedEntity.GetComponent<PhysicsBody>();
                        if (pb != null) pb.Velocity = new Vector2D<float>(0, 0);
                    }
                }

                // 6. OVERLAY: CAMERA COORDINATES IN THE CORNER
                string camText = $"Camera: ({ctx.GameCamera.Position.X:F2}, {ctx.GameCamera.Position.Y:F2})";
                var textSize = ImGui.CalcTextSize(camText);
                var camTextPos = new Vector2(imageScreenPos.X + availableSize.X - textSize.X - 10, imageScreenPos.Y + 10);

                ImGui.SetCursorScreenPos(new Vector2(camTextPos.X + 1, camTextPos.Y + 1));
                ImGui.TextColored(new Vector4(0.0f, 0.0f, 0.0f, 1.0f), camText);
                
                ImGui.SetCursorScreenPos(camTextPos);
                ImGui.TextColored(new Vector4(1.0f, 0.9f, 0.2f, 1.0f), camText);
            }
            ImGui.End();
        }
    }
}
