using ImGuiNET;
using Silk.NET.Maths;
using System.Numerics;

namespace ef2dg
{
    public class HierarchyWindow : EditorWindow
    {
        public override string Title => "Hierarchy";

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(20, 20), ImGuiCond.FirstUseEver);
            ImGui.Begin(Title, ref IsOpen);

            if (ImGui.Button("Create Empty GameObject"))
            {
                var newObj = new GameObject() { Position = new Vector2D<float>(0, 0) };
                newObj.Name = $"Entity {ctx.CurrentScene.Entities.Count}"; 
                newObj.ParentScene = ctx.CurrentScene;
                ctx.CurrentScene.Entities.Add(newObj);
            }
            ImGui.Separator();

            // Variables for safely deferring list changes outside the ImGui loop
            int swapIndexA = -1;
            int swapIndexB = -1;
            int removeIndex = -1;

            int entityCount = ctx.CurrentScene.Entities.Count;
            float actionsWidth = EditorIcons.RowWidth(3);
            float spacing = ImGui.GetStyle().ItemSpacing.X * 0.5f;

            for (int i = 0; i < entityCount; i++)
            {
                var entity = ctx.CurrentScene.Entities[i];
                bool isSelected = (state.SelectedEntity == entity);

                ImGui.PushID(i);

                // Row height = icon height, otherwise the text is not centred on the buttons
                ImGui.SetNextItemAllowOverlap();
                if (ImGui.Selectable($"{entity.Name}##sel{i}", isSelected, ImGuiSelectableFlags.None,
                        new Vector2(0f, ImGui.GetTextLineHeight() + 2f)))
                {
                    state.SelectedEntity = entity;
                }
                bool rowHovered = ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

                // Double click focuses the camera on the object
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (ctx.GameCamera != null)
                    {
                        // Move the camera's X and Y, keeping the Z height unchanged
                        ctx.GameCamera.Position = new Vector3D<float>(
                            entity.Position.X,
                            entity.Position.Y,
                            ctx.GameCamera.Position.Z
                        );
                    }
                }

                // Reorder and delete buttons on the right edge; bright for the hovered and the selected row
                bool dimmed = !rowHovered && !isSelected;
                ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - actionsWidth);

                // Up — move to the background / towards the start of the list
                if (EditorIcons.Button("##up", EditorIcon.ChevronUp, "Move up (draw behind)", i > 0, dimmed))
                {
                    swapIndexA = i;
                    swapIndexB = i - 1;
                }

                ImGui.SameLine(0f, spacing);
                // Down — move to the foreground / towards the end of the list
                if (EditorIcons.Button("##down", EditorIcon.ChevronDown, "Move down (draw in front)", i < entityCount - 1, dimmed))
                {
                    swapIndexA = i;
                    swapIndexB = i + 1;
                }

                ImGui.SameLine(0f, spacing);
                if (EditorIcons.Button("##delete", EditorIcon.Trash, "Delete", true, dimmed, danger: true))
                {
                    removeIndex = i;
                }

                ImGui.PopID();
            }

            // Safely perform reorders and deletions
            if (swapIndexA != -1 && swapIndexB != -1)
            {
                var temp = ctx.CurrentScene.Entities[swapIndexA];
                ctx.CurrentScene.Entities[swapIndexA] = ctx.CurrentScene.Entities[swapIndexB];
                ctx.CurrentScene.Entities[swapIndexB] = temp;
            }

            if (removeIndex != -1)
            {
                var entityToRemove = ctx.CurrentScene.Entities[removeIndex];
                if (state.SelectedEntity == entityToRemove) state.SelectedEntity = null;
                ctx.CurrentScene.Entities.RemoveAt(removeIndex);
            }

            ImGui.End();
        }
    }
}