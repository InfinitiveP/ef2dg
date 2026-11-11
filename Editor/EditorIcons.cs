using ImGuiNET;
using System.Numerics;

namespace ef2dg
{
    public enum EditorIcon { ChevronUp, ChevronDown, Trash }

    // VS Code-style icon buttons drawn with lines: the editor font has no icon glyphs
    public static class EditorIcons
    {
        private const float Stroke = 1.5f;

        // dimmed — muted colour while the row is not hovered (like actions in the VS Code tree)
        public static bool Button(string id, EditorIcon icon, string tooltip, bool enabled = true, bool dimmed = false, bool danger = false)
        {
            float size = ImGui.GetTextLineHeight() + 2f;
            var min = ImGui.GetCursorScreenPos();
            var max = min + new Vector2(size, size);

            bool clicked = ImGui.InvisibleButton(id, new Vector2(size, size)) && enabled;
            bool hovered = enabled && ImGui.IsItemHovered();
            bool held = enabled && ImGui.IsItemActive();

            var drawList = ImGui.GetWindowDrawList();
            if (hovered)
            {
                var background = danger ? new Vector4(EditorTheme.StopRed.X, EditorTheme.StopRed.Y, EditorTheme.StopRed.Z, held ? 0.35f : 0.2f)
                                        : new Vector4(1f, 1f, 1f, held ? 0.16f : 0.1f);
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(background), 3f);
            }

            var style = ImGui.GetStyle();
            Vector4 color = !enabled ? style.Colors[(int)ImGuiCol.TextDisabled] * new Vector4(1f, 1f, 1f, 0.35f)
                : hovered ? (danger ? EditorTheme.StopRed : new Vector4(1f, 1f, 1f, 1f))
                : dimmed ? style.Colors[(int)ImGuiCol.TextDisabled]
                : style.Colors[(int)ImGuiCol.Text];

            DrawIcon(drawList, icon, (min + max) * 0.5f, size * 0.5f, ImGui.GetColorU32(color));

            if (hovered && !string.IsNullOrEmpty(tooltip)) ImGui.SetTooltip(tooltip);
            return clicked;
        }

        // Width of a row of count buttons, used to right-align them
        public static float RowWidth(int count) =>
            count * (ImGui.GetTextLineHeight() + 2f) + (count - 1) * ImGui.GetStyle().ItemSpacing.X * 0.5f;

        private static void DrawIcon(ImDrawListPtr drawList, EditorIcon icon, Vector2 center, float half, uint color)
        {
            // Centre on the middle of a pixel so thin lines are not blurred
            center = new Vector2(MathF.Floor(center.X) + 0.5f, MathF.Floor(center.Y) + 0.5f);
            float u = half * 0.62f;

            switch (icon)
            {
                case EditorIcon.ChevronUp:
                    drawList.AddLine(center + new Vector2(-u * 0.8f, u * 0.4f), center + new Vector2(0f, -u * 0.4f), color, Stroke);
                    drawList.AddLine(center + new Vector2(0f, -u * 0.4f), center + new Vector2(u * 0.8f, u * 0.4f), color, Stroke);
                    break;

                case EditorIcon.ChevronDown:
                    drawList.AddLine(center + new Vector2(-u * 0.8f, -u * 0.4f), center + new Vector2(0f, u * 0.4f), color, Stroke);
                    drawList.AddLine(center + new Vector2(0f, u * 0.4f), center + new Vector2(u * 0.8f, -u * 0.4f), color, Stroke);
                    break;

                case EditorIcon.Trash:
                    // Lid with a handle
                    drawList.AddLine(center + new Vector2(-u, -u * 0.62f), center + new Vector2(u, -u * 0.62f), color, Stroke);
                    drawList.AddRect(center + new Vector2(-u * 0.32f, -u * 0.95f), center + new Vector2(u * 0.32f, -u * 0.62f), color, 0f, ImDrawFlags.None, Stroke);
                    // Trapezoid body and two slots
                    drawList.AddQuad(
                        center + new Vector2(-u * 0.72f, -u * 0.4f), center + new Vector2(u * 0.72f, -u * 0.4f),
                        center + new Vector2(u * 0.56f, u), center + new Vector2(-u * 0.56f, u), color, Stroke);
                    drawList.AddLine(center + new Vector2(-u * 0.22f, -u * 0.15f), center + new Vector2(-u * 0.2f, u * 0.72f), color, Stroke * 0.8f);
                    drawList.AddLine(center + new Vector2(u * 0.22f, -u * 0.15f), center + new Vector2(u * 0.2f, u * 0.72f), color, Stroke * 0.8f);
                    break;
            }
        }
    }
}
