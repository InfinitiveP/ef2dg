using ImGuiNET;
using Silk.NET.Maths;
using System.Numerics;

namespace ef2dg
{
    public class ProjectSettingsWindow : EditorWindow
    {
        public override string Title => "Project Settings";

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.Begin(Title, ref IsOpen);

            if (ImGui.CollapsingHeader("General Configuration", ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool needsSave = false;

                float zoom = ProjectConfig.Data.DefaultCameraZoom;
                if (ImGui.DragFloat("Default Game Zoom", ref zoom, 0.05f, 0.1f, 10.0f))
                {
                    ProjectConfig.Data.DefaultCameraZoom = zoom;
                    needsSave = true;
                }

                var bgColor = ProjectConfig.Data.BackgroundColor;
                if (ImGui.ColorEdit4("Background Color", ref bgColor))
                {
                    ProjectConfig.Data.BackgroundColor = bgColor;
                    needsSave = true;
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Lighting is no longer global: every scene has its own settings
                ImGui.TextDisabled("2D Lighting is configured per scene in the Lighting window.");

                if (needsSave) ProjectConfig.Save();
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Input Bindings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var actions = new System.Collections.Generic.List<string>(InputManager.Bindings.Keys);

                foreach (var action in actions)
                {
                    var currentKey = InputManager.Bindings[action];

                    ImGui.PushID(action);
                    ImGui.Text(action);
                    ImGui.SameLine(100); 

                    if (state.WaitingForBindAction == action)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                        ImGui.Button("[ Press any key... ]", new Vector2(150, 0));
                        ImGui.PopStyleColor();

                        Silk.NET.Input.Key pressedKey = InputManager.GetAnyPressedKey();
                        
                        if (pressedKey != Silk.NET.Input.Key.Unknown)
                        {
                            if (pressedKey == Silk.NET.Input.Key.Escape)
                            {
                                state.WaitingForBindAction = null;
                            }
                            else
                            {
                                InputManager.Bindings[action] = pressedKey;
                                InputManager.SaveBindings();
                                state.WaitingForBindAction = null; 
                            }
                        }
                    }
                    else
                    {
                        if (ImGui.Button(currentKey.ToString(), new Vector2(150, 0)))
                        {
                            state.WaitingForBindAction = action; 
                        }
                    }

                    ImGui.PopID();
                }

                ImGui.Spacing();
                ImGui.Separator();
                
                string newAction = state.NewStateName;
                if (ImGui.InputText("##NewAction", ref newAction, 32)) state.NewStateName = newAction;
                
                ImGui.SameLine();
                if (ImGui.Button("Add Action"))
                {
                    if (!string.IsNullOrWhiteSpace(state.NewStateName) && !InputManager.Bindings.ContainsKey(state.NewStateName))
                    {
                        InputManager.Bindings[state.NewStateName] = Silk.NET.Input.Key.Unknown;
                        state.NewStateName = "";
                        InputManager.SaveBindings();
                    }
                }
            }

            ImGui.End();
        }
    }
}