using ImGuiNET;
using System.Numerics;

namespace ef2dg
{
    // Dark theme in VS Code colours (Dark+)
    public static class EditorTheme
    {
        public static readonly Vector4 EditorBackground = Rgb(0x1E1E1E);
        public static readonly Vector4 StatusBarEdit = Rgb(0x007ACC);
        public static readonly Vector4 StatusBarPlay = Rgb(0xCC6633);
        public static readonly Vector4 MenuSelection = Rgb(0x094771);
        public static readonly Vector4 AccentText = Rgb(0x3794FF);
        public static readonly Vector4 PlayGreen = Rgb(0x89D185);
        public static readonly Vector4 StopRed = Rgb(0xF48771);

        public static void ApplyVsCodeDark()
        {
            ImGui.StyleColorsDark();
            var style = ImGui.GetStyle();

            // VS Code: square corners on panels and tabs, slight rounding on input fields
            style.WindowRounding = 0f;
            style.ChildRounding = 0f;
            style.FrameRounding = 2f;
            style.PopupRounding = 3f;
            style.ScrollbarRounding = 0f;
            style.GrabRounding = 2f;
            style.TabRounding = 0f;

            style.WindowBorderSize = 1f;
            style.ChildBorderSize = 1f;
            style.PopupBorderSize = 1f;
            style.FrameBorderSize = 0f;
            style.TabBorderSize = 0f;
            style.DockingSeparatorSize = 2f;

            style.WindowPadding = new Vector2(8f, 8f);
            style.FramePadding = new Vector2(6f, 4f);
            style.ItemSpacing = new Vector2(8f, 5f);
            style.ItemInnerSpacing = new Vector2(6f, 4f);
            style.IndentSpacing = 14f;
            style.ScrollbarSize = 12f;
            style.GrabMinSize = 10f;
            style.WindowTitleAlign = new Vector2(0f, 0.5f);
            // No collapse triangle in the window title
            style.WindowMenuButtonPosition = ImGuiDir.None;

            var colors = style.Colors;
            colors[(int)ImGuiCol.Text] = Rgb(0xCCCCCC);
            colors[(int)ImGuiCol.TextDisabled] = Rgb(0x8B8B8B);
            colors[(int)ImGuiCol.WindowBg] = Rgb(0x252526);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.PopupBg] = Rgb(0x252526);
            colors[(int)ImGuiCol.Border] = Rgb(0x3C3C3C);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f);

            colors[(int)ImGuiCol.FrameBg] = Rgb(0x3C3C3C);
            colors[(int)ImGuiCol.FrameBgHovered] = Rgb(0x464647);
            colors[(int)ImGuiCol.FrameBgActive] = Rgb(0x4B4B4D);

            colors[(int)ImGuiCol.TitleBg] = Rgb(0x252526);
            colors[(int)ImGuiCol.TitleBgActive] = Rgb(0x252526);
            colors[(int)ImGuiCol.TitleBgCollapsed] = Rgb(0x252526);
            colors[(int)ImGuiCol.MenuBarBg] = Rgb(0x3C3C3C);

            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.ScrollbarGrab] = Rgb(0x797979, 0.4f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = Rgb(0x646464, 0.7f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = Rgb(0xBFBFBF, 0.4f);

            colors[(int)ImGuiCol.CheckMark] = Rgb(0xE0E0E0);
            colors[(int)ImGuiCol.SliderGrab] = Rgb(0x0E639C);
            colors[(int)ImGuiCol.SliderGrabActive] = Rgb(0x1177BB);

            // Regular buttons are "secondary" grey, blue only when pressed
            colors[(int)ImGuiCol.Button] = Rgb(0x3A3D41);
            colors[(int)ImGuiCol.ButtonHovered] = Rgb(0x45494E);
            colors[(int)ImGuiCol.ButtonActive] = Rgb(0x0E639C);

            colors[(int)ImGuiCol.Header] = Rgb(0x37373D);
            colors[(int)ImGuiCol.HeaderHovered] = Rgb(0x45454B);
            colors[(int)ImGuiCol.HeaderActive] = Rgb(0x094771);

            colors[(int)ImGuiCol.Separator] = Rgb(0x3C3C3C);
            colors[(int)ImGuiCol.SeparatorHovered] = Rgb(0x007ACC);
            colors[(int)ImGuiCol.SeparatorActive] = Rgb(0x007ACC);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.ResizeGripHovered] = Rgb(0x007ACC, 0.6f);
            colors[(int)ImGuiCol.ResizeGripActive] = Rgb(0x007ACC);

            // Tabs: inactive ones are darker than the panel, the active one blends with the content
            colors[(int)ImGuiCol.Tab] = Rgb(0x2D2D2D);
            colors[(int)ImGuiCol.TabHovered] = Rgb(0x323233);
            colors[(int)ImGuiCol.TabActive] = Rgb(0x1E1E1E);
            colors[(int)ImGuiCol.TabUnfocused] = Rgb(0x2D2D2D);
            colors[(int)ImGuiCol.TabUnfocusedActive] = Rgb(0x1E1E1E);

            colors[(int)ImGuiCol.DockingPreview] = Rgb(0x007ACC, 0.45f);
            colors[(int)ImGuiCol.DockingEmptyBg] = Rgb(0x1E1E1E);

            colors[(int)ImGuiCol.TableHeaderBg] = Rgb(0x2D2D2D);
            colors[(int)ImGuiCol.TableBorderStrong] = Rgb(0x3C3C3C);
            colors[(int)ImGuiCol.TableBorderLight] = Rgb(0x2D2D2D);
            colors[(int)ImGuiCol.TableRowBg] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1f, 1f, 1f, 0.03f);

            colors[(int)ImGuiCol.TextSelectedBg] = Rgb(0x264F78);
            colors[(int)ImGuiCol.DragDropTarget] = Rgb(0x007ACC);
            colors[(int)ImGuiCol.NavHighlight] = Rgb(0x007FD4);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0f, 0f, 0f, 0.55f);
        }

        private static Vector4 Rgb(uint hex, float alpha = 1f) =>
            new Vector4(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, alpha);
    }
}
