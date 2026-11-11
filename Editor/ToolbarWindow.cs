using ImGuiNET;
using System.Numerics;

namespace ef2dg
{
    // Extra button panel. The same actions are in the main menu (File/Edit/Run), so it is hidden by default.
    public class ToolbarWindow : EditorWindow
    {
        public override string Title => "Toolbar";

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowPos(new Vector2(600, 60), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(399, 65)); // Made the window a bit wider
            ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize);

            if (ImGui.Button(state.IsPlayMode ? "STOP" : "PLAY", new Vector2(70, 30))) EditorCommands.TogglePlayMode(ctx, state);

            ImGui.SameLine();
            if (ImGui.Button("UNDO", new Vector2(50, 30))) state.History.Undo();
            ImGui.SameLine();
            if (ImGui.Button("REDO", new Vector2(50, 30))) state.History.Redo();

            ImGui.SameLine();
            if (ImGui.Button("SAVE AS...", new Vector2(90, 30))) state.RequestSaveAsDialog = true;

            ImGui.SameLine();
            if (ImGui.Button("LOAD...", new Vector2(90, 30))) state.RequestOpenDialog = true;

            ImGui.End();
        }
    }
}
