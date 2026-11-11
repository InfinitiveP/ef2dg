using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace ef2dg
{
    // Main menu pinned to the top edge of the window: File / Edit / View / Run / Help and the Play button in the middle
    public class MainMenuBar
    {
        private bool _showShortcuts;
        private bool _showAbout;

        public void Draw(EditorContext ctx, EditorState state, IReadOnlyList<EditorWindow> windows, EditorLayout layout)
        {
            // Menu item highlight is blue, as in VS Code; other lists stay grey
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, EditorTheme.MenuSelection);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
            if (ImGui.BeginMainMenuBar())
            {
                DrawFileMenu(ctx, state);
                DrawEditMenu(ctx, state);
                DrawViewMenu(windows, layout);
                DrawRunMenu(ctx, state);
                DrawHelpMenu();
                DrawPlayButton(ctx, state);
                DrawSceneTitle(ctx);
                ImGui.EndMainMenuBar();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();

            DrawShortcutsWindow();
            DrawAboutWindow();
        }

        public void HandleShortcuts(EditorContext ctx, EditorState state)
        {
            var io = ImGui.GetIO();
            // While text is being typed, Ctrl+Z/Ctrl+S etc. belong to the input field
            if (io.WantTextInput || !io.KeyCtrl) return;

            if (Pressed(ImGuiKey.S))
            {
                if (io.KeyShift) state.RequestSaveAsDialog = true;
                else SceneFileDialogs.QuickSave(ctx, state);
            }
            else if (Pressed(ImGuiKey.O)) state.RequestOpenDialog = true;
            else if (Pressed(ImGuiKey.Z))
            {
                if (io.KeyShift) state.History.Redo();
                else state.History.Undo();
            }
            else if (Pressed(ImGuiKey.Y)) state.History.Redo();
            else if (Pressed(ImGuiKey.P)) EditorCommands.TogglePlayMode(ctx, state);
            else if (io.KeyShift && Pressed(ImGuiKey.N)) EditorCommands.CreateEmptyGameObject(ctx, state);
        }

        private static bool Pressed(ImGuiKey key) => ImGui.IsKeyPressed(key, false);

        private static void CenterNextWindow()
        {
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        }

        private static void DrawFileMenu(EditorContext ctx, EditorState state)
        {
            if (!ImGui.BeginMenu("File")) return;

            if (ImGui.BeginMenu("New Scene"))
            {
                foreach (var type in EditorCommands.GetSceneTypes())
                {
                    if (ImGui.MenuItem(type.Name)) EditorCommands.NewScene(ctx, state, type);
                }
                ImGui.EndMenu();
            }
            if (ImGui.MenuItem("Open...", "Ctrl+O")) state.RequestOpenDialog = true;
            ImGui.Separator();
            if (ImGui.MenuItem("Save", "Ctrl+S")) SceneFileDialogs.QuickSave(ctx, state);
            if (ImGui.MenuItem("Save As...", "Ctrl+Shift+S")) state.RequestSaveAsDialog = true;
            ImGui.Separator();
            if (ImGui.MenuItem("Exit", "Alt+F4")) ctx.Exit?.Invoke();

            ImGui.EndMenu();
        }

        private static void DrawEditMenu(EditorContext ctx, EditorState state)
        {
            if (!ImGui.BeginMenu("Edit")) return;

            if (ImGui.MenuItem("Undo", "Ctrl+Z")) state.History.Undo();
            if (ImGui.MenuItem("Redo", "Ctrl+Y")) state.History.Redo();
            ImGui.Separator();
            if (ImGui.MenuItem("Create Empty GameObject", "Ctrl+Shift+N")) EditorCommands.CreateEmptyGameObject(ctx, state);
            if (ImGui.MenuItem("Delete Selected", "", false, state.SelectedEntity != null)) EditorCommands.DeleteSelected(ctx, state);
            ImGui.Separator();
            if (ImGui.MenuItem("Select Tool", "", state.CurrentTool == EditorState.ToolMode.Select))
                state.CurrentTool = EditorState.ToolMode.Select;
            if (ImGui.MenuItem("Paint Tiles", "", state.CurrentTool == EditorState.ToolMode.PaintTiles))
                state.CurrentTool = EditorState.ToolMode.PaintTiles;
            if (ImGui.MenuItem("Erase Tiles", "", state.CurrentTool == EditorState.ToolMode.EraseTiles))
                state.CurrentTool = EditorState.ToolMode.EraseTiles;

            ImGui.EndMenu();
        }

        private static void DrawViewMenu(IReadOnlyList<EditorWindow> windows, EditorLayout layout)
        {
            if (!ImGui.BeginMenu("View")) return;

            foreach (var window in windows)
            {
                if (ImGui.MenuItem(window.Title, "", window.IsOpen)) window.IsOpen = !window.IsOpen;
            }
            ImGui.Separator();

            if (ImGui.MenuItem("Lighting Preview", "", DebugSettings.PreviewLightingInEditor))
                DebugSettings.PreviewLightingInEditor = !DebugSettings.PreviewLightingInEditor;

            if (ImGui.BeginMenu("Debug Overlays"))
            {
                if (ImGui.MenuItem("Player Hitbox", "F1", DebugSettings.ShowPlayerHitbox)) DebugSettings.ShowPlayerHitbox = !DebugSettings.ShowPlayerHitbox;
                if (ImGui.MenuItem("Enemy Hitbox", "F2", DebugSettings.ShowEnemyHitbox)) DebugSettings.ShowEnemyHitbox = !DebugSettings.ShowEnemyHitbox;
                if (ImGui.MenuItem("Map Hitbox", "F3", DebugSettings.ShowMapHitbox)) DebugSettings.ShowMapHitbox = !DebugSettings.ShowMapHitbox;
                if (ImGui.MenuItem("Attack Zone", "F4", DebugSettings.ShowAttackZone)) DebugSettings.ShowAttackZone = !DebugSettings.ShowAttackZone;
                if (ImGui.MenuItem("Performance Monitor", "F5", DebugSettings.ShowPerformanceMonitor)) DebugSettings.ShowPerformanceMonitor = !DebugSettings.ShowPerformanceMonitor;
                ImGui.EndMenu();
            }
            ImGui.Separator();

            if (ImGui.MenuItem("Reset Layout")) layout.RequestReset();

            ImGui.EndMenu();
        }

        private static void DrawRunMenu(EditorContext ctx, EditorState state)
        {
            if (!ImGui.BeginMenu("Run")) return;

            if (ImGui.MenuItem(state.IsPlayMode ? "Stop" : "Play", "Ctrl+P")) EditorCommands.TogglePlayMode(ctx, state);

            ImGui.EndMenu();
        }

        private void DrawHelpMenu()
        {
            if (!ImGui.BeginMenu("Help")) return;

            if (ImGui.MenuItem("Keyboard Shortcuts")) _showShortcuts = true;
            if (ImGui.MenuItem("About ef2dg")) _showAbout = true;

            ImGui.EndMenu();
        }

        // Button with a drawn icon: the font has no ▶ and ■ glyphs
        private static void DrawPlayButton(EditorContext ctx, EditorState state)
        {
            float height = ImGui.GetFrameHeight();
            var size = new Vector2(height * 1.6f, height);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - size.X) * 0.5f);

            var min = ImGui.GetCursorScreenPos();
            if (ImGui.InvisibleButton("##PlayStop", size)) EditorCommands.TogglePlayMode(ctx, state);
            bool hovered = ImGui.IsItemHovered();
            if (hovered) ImGui.SetTooltip(state.IsPlayMode ? "Stop (Ctrl+P)" : "Play (Ctrl+P)");

            var drawList = ImGui.GetWindowDrawList();
            var max = min + size;
            if (hovered) drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.1f)), 3f);

            var center = (min + max) * 0.5f;
            float icon = height * 0.32f;
            if (state.IsPlayMode)
            {
                drawList.AddRectFilled(center - new Vector2(icon, icon) * 0.85f, center + new Vector2(icon, icon) * 0.85f,
                    ImGui.GetColorU32(EditorTheme.StopRed), 1f);
            }
            else
            {
                drawList.AddTriangleFilled(
                    center + new Vector2(-icon * 0.8f, -icon),
                    center + new Vector2(icon, 0f),
                    center + new Vector2(-icon * 0.8f, icon),
                    ImGui.GetColorU32(EditorTheme.PlayGreen));
            }
        }

        private static void DrawSceneTitle(EditorContext ctx)
        {
            var scene = ctx.CurrentScene;
            string name = string.IsNullOrEmpty(scene.InstanceName) ? scene.GetType().Name : scene.InstanceName;
            string title = $"{name} - ef2dg";
            float x = ImGui.GetWindowWidth() - ImGui.CalcTextSize(title).X - 12f;
            if (x <= ImGui.GetCursorPosX()) return;

            ImGui.SameLine(x);
            ImGui.TextDisabled(title);
        }

        private void DrawShortcutsWindow()
        {
            if (!_showShortcuts) return;

            CenterNextWindow();
            if (ImGui.Begin("Keyboard Shortcuts", ref _showShortcuts, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGui.BeginTable("##shortcuts", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV))
                {
                    Row("Ctrl+S", "Save scene");
                    Row("Ctrl+Shift+S", "Save scene as...");
                    Row("Ctrl+O", "Open scene");
                    Row("Ctrl+Z / Ctrl+Y", "Undo / Redo");
                    Row("Ctrl+P", "Play / Stop");
                    Row("Ctrl+Shift+N", "Create empty GameObject");
                    Row("W A S D", "Move editor camera");
                    Row("Mouse wheel", "Zoom Scene View");
                    Row("F1 - F4", "Debug hitboxes");
                    Row("F5", "Performance monitor");
                    ImGui.EndTable();
                }
            }
            ImGui.End();

            static void Row(string keys, string action)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(EditorTheme.AccentText, keys);
                ImGui.TableNextColumn();
                ImGui.Text(action);
            }
        }

        private void DrawAboutWindow()
        {
            if (!_showAbout) return;

            CenterNextWindow();
            if (ImGui.Begin("About ef2dg", ref _showAbout, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("ef2dg Engine");
                ImGui.TextDisabled("2D engine and editor on C#, OpenGL 3.3 and Dear ImGui.");
            }
            ImGui.End();
        }
    }
}
