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

            // Переменные для безопасного отложенного изменения списка вне цикла ImGui
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

                // Высота строки = высота иконок, иначе текст не по центру кнопок
                ImGui.SetNextItemAllowOverlap();
                if (ImGui.Selectable($"{entity.Name}##sel{i}", isSelected, ImGuiSelectableFlags.None,
                        new Vector2(0f, ImGui.GetTextLineHeight() + 2f)))
                {
                    state.SelectedEntity = entity;
                }
                bool rowHovered = ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

                // Двойной клик для фокуса камеры на объекте
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (ctx.GameCamera != null)
                    {
                        // Перемещаем X и Y камеры, оставляя высоту Z без изменений
                        ctx.GameCamera.Position = new Vector3D<float>(
                            entity.Position.X,
                            entity.Position.Y,
                            ctx.GameCamera.Position.Z
                        );
                    }
                }

                // Кнопки управления порядком и удаления по правому краю; яркие у строки под курсором и у выбранной
                bool dimmed = !rowHovered && !isSelected;
                ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - actionsWidth);

                // Вверх — сдвинуть на задний план / к началу списка
                if (EditorIcons.Button("##up", EditorIcon.ChevronUp, "Move up (draw behind)", i > 0, dimmed))
                {
                    swapIndexA = i;
                    swapIndexB = i - 1;
                }

                ImGui.SameLine(0f, spacing);
                // Вниз — сдвинуть на передний план / к концу списка
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

            // Безопасное выполнение перестановок и удалений
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