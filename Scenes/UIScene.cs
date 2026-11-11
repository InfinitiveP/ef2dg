using ImGuiNET;
using Silk.NET.Input;
using System;
using System.Numerics;

namespace ef2dg
{
    public class UIScene : Scene
    {
        protected ResourceManager _resources = null!;
        protected IInputContext _inputContext = null!;

        public override void Load(ResourceManager resources, IInputContext input)
        {
            _resources = resources;
            _inputContext = input;
            
            // Dummy map so the editor works
            Map = new Tilemap(0.5f);
            Map.Layers.Add(new TileLayer { Name = "UI_Layer", HasCollision = false });
        }

        public override void Update(float deltaTime) 
        {
            base.Update(deltaTime);
        }

        // This method is called from Engine.cs inside the ImGui context
        public void DrawMenuUI(Vector2 containerSize, Action<Scene> changeScene, Action<Scene> pushScene)
        {
            // Create an invisible canvas for UI elements
            ImGui.BeginChild("##MenuCanvas", containerSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground);

            // Iterate over every entity in the scene
            foreach (var entity in Entities)
            {
                var button = entity.GetComponent<ButtonComponent>();
                if (button != null)
                {
                    // Use the entity coordinates to position the button
                    // Convert local coordinates (for example, 0 to 1 or pixels) into the ImGui cursor position
                    ImGui.SetCursorPos(new Vector2(entity.Position.X, entity.Position.Y));

                    // Draw the button
                    if (ImGui.Button($"{button.Label}##{entity.GetHashCode()}", new Vector2(button.Size.X, button.Size.Y)))
                    {
                        // Handle the click: try to find the scene by name through reflection
                        if (!string.IsNullOrEmpty(button.TargetSceneName))
                        {
                            Engine.Instance.RequestSceneTransition(button.TargetSceneName, button.IsOverlay, button.Transition);
                        }
                    }
                }
            }

            ImGui.EndChild();
        }
    }
}
