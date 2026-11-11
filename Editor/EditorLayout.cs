using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ef2dg
{
    // VS Code-style editor layout: a docking area over the whole window between the main menu and the status bar,
    // the default layout and saving window visibility between launches.
    public class EditorLayout
    {
        public const string SettingsFileName = "editor_layout.json";

        // Increase when the default layout changes: everyone gets the new one on the next launch
        private const int CurrentLayoutVersion = 1;

        // ImGuiDockNodeFlags_DockSpace is an internal imgui flag missing from the ImGui.NET wrapper
        private const int DockSpaceNodeFlag = 1 << 10;

        private class LayoutSettings
        {
            public int LayoutVersion { get; set; }
            public Dictionary<string, bool> OpenWindows { get; set; } = new();
        }

        // Tabs that must be active in their panels after a layout reset (the last one gets focus)
        private static readonly string[] DefaultActiveTabs = { "Console (Terminal)", "Inspector", "Hierarchy", "Scene View" };

        private readonly IReadOnlyList<EditorWindow> _windows;
        private bool _resetRequested;
        private bool _focusDefaultTabs;
        private string _savedSnapshot = "";

        public EditorLayout(IReadOnlyList<EditorWindow> windows)
        {
            _windows = windows;
            Load();
        }

        public float StatusBarHeight => ImGui.GetTextLineHeight() + 8f;

        public void RequestReset() => _resetRequested = true;

        // Host window with the DockSpace. inHost is drawn inside the host: modal dialogs live there
        public void DrawDockHost(Action inHost)
        {
            var viewport = ImGui.GetMainViewport();
            var size = new Vector2(viewport.WorkSize.X, MathF.Max(1f, viewport.WorkSize.Y - StatusBarHeight));
            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(size);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
                        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                        ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoSavedSettings;
            ImGui.Begin("##EditorDockHost", flags);
            ImGui.PopStyleVar(3);

            uint dockId = ImGui.GetID("EditorDockSpace");
            if (_resetRequested)
            {
                _resetRequested = false;
                BuildDefaultLayout(dockId, size);
            }
            ImGui.DockSpace(dockId, Vector2.Zero, ImGuiDockNodeFlags.None);

            inHost();
            ImGui.End();
        }

        // Bottom status bar: blue in edit mode, orange in Play Mode (like debugging in VS Code)
        public void DrawStatusBar(EditorContext ctx, EditorState state)
        {
            var viewport = ImGui.GetMainViewport();
            float height = StatusBarHeight;
            ImGui.SetNextWindowPos(new Vector2(viewport.WorkPos.X, viewport.WorkPos.Y + viewport.WorkSize.Y - height));
            ImGui.SetNextWindowSize(new Vector2(viewport.WorkSize.X, height));
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, state.IsPlayMode ? EditorTheme.StatusBarPlay : EditorTheme.StatusBarEdit);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, (height - ImGui.GetTextLineHeight()) * 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(18f, 0f));
            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDocking |
                        ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav |
                        ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("##EditorStatusBar", flags);

            var scene = ctx.CurrentScene;
            string sceneName = string.IsNullOrEmpty(scene.InstanceName) ? scene.GetType().Name : scene.InstanceName;
            ImGui.Text(state.IsPlayMode ? "PLAY MODE" : "EDIT MODE");
            ImGui.SameLine();
            ImGui.Text(string.IsNullOrEmpty(scene.FilePath) ? $"{sceneName} (not saved)" : sceneName);
            ImGui.SameLine();
            ImGui.Text($"Entities: {scene.Entities.Count}");
            if (state.SelectedEntity != null)
            {
                ImGui.SameLine();
                ImGui.Text($"Selected: {state.SelectedEntity.Name}");
            }

            var camera = ctx.GameCamera;
            string right = $"Tool: {state.CurrentTool}    Camera: {camera.Position.X:F2}, {camera.Position.Y:F2}    Zoom: {camera.Zoom:F2}    {ImGui.GetIO().Framerate:F0} FPS";
            float rightX = ImGui.GetWindowWidth() - ImGui.CalcTextSize(right).X - 10f;
            if (rightX > ImGui.GetCursorPosX() + 40f)
            {
                ImGui.SameLine(rightX);
                ImGui.Text(right);
            }

            ImGui.End();
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(2);
        }

        // As in VS Code: hierarchy and assets on the left, the scene in the centre, the inspector on the right, the console at the bottom
        private void BuildDefaultLayout(uint dockId, Vector2 size)
        {
            DockBuilderRemoveNode(dockId);
            DockBuilderAddNode(dockId, DockSpaceNodeFlag);
            DockBuilderSetNodeSize(dockId, size);

            uint center = dockId;
            uint left = Split(ref center, ImGuiDir.Left, 0.18f);
            uint right = Split(ref center, ImGuiDir.Right, 0.25f);
            uint bottom = Split(ref center, ImGuiDir.Down, 0.28f);
            uint leftBottom = Split(ref left, ImGuiDir.Down, 0.45f);

            Dock("Hierarchy", left);
            Dock("Asset Browser", leftBottom);
            Dock("Scene View", center);
            Dock("Inspector", right);
            Dock("Lighting", right);
            Dock("Console (Terminal)", bottom);
            Dock("Tile Palette", bottom);
            Dock("Project Settings", bottom);
            // The Toolbar duplicates the menu and is hidden by default; if opened, it floats
            Dock("Toolbar", 0);
            DockBuilderFinish(dockId);

            foreach (var window in _windows) window.IsOpen = window.Title != "Toolbar";
            _focusDefaultTabs = true;
        }

        // Called after every window of the frame has been drawn
        public void EndFrame()
        {
            if (_focusDefaultTabs)
            {
                // Windows are already docked in this frame — focus picks the right tab in each panel
                _focusDefaultTabs = false;
                foreach (string title in DefaultActiveTabs) ImGui.SetWindowFocus(title);
            }

            SaveIfChanged();
        }

        private void SaveIfChanged()
        {
            string snapshot = BuildSnapshot();
            if (snapshot == _savedSnapshot) return;
            _savedSnapshot = snapshot;

            var settings = new LayoutSettings
            {
                LayoutVersion = CurrentLayoutVersion,
                OpenWindows = _windows.ToDictionary(w => w.Title, w => w.IsOpen)
            };
            try
            {
                File.WriteAllText(SettingsFileName, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditorLayout] Failed to save {SettingsFileName}: {ex.Message}");
            }
        }

        private void Load()
        {
            LayoutSettings? settings = null;
            if (File.Exists(SettingsFileName))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<LayoutSettings>(File.ReadAllText(SettingsFileName));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EditorLayout] Failed to read {SettingsFileName}: {ex.Message}");
                }
            }

            // First launch of a new layout: build it in the first frame
            if (settings == null || settings.LayoutVersion < CurrentLayoutVersion)
            {
                _resetRequested = true;
                return;
            }

            foreach (var window in _windows)
            {
                if (settings.OpenWindows.TryGetValue(window.Title, out bool open)) window.IsOpen = open;
            }
            _savedSnapshot = BuildSnapshot();
        }

        private string BuildSnapshot() => string.Join(",", _windows.Select(w => w.IsOpen ? "1" : "0"));

        // =========================================================
        // DockBuilder from imgui_internal.h: exported by the native cimgui.dll but not wrapped by ImGui.NET
        // =========================================================
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderAddNode")]
        private static extern uint DockBuilderAddNode(uint nodeId, int flags);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderRemoveNode")]
        private static extern void DockBuilderRemoveNode(uint nodeId);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderSetNodeSize")]
        private static extern void DockBuilderSetNodeSize(uint nodeId, Vector2 size);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderSplitNode")]
        private static extern unsafe uint DockBuilderSplitNode(uint nodeId, ImGuiDir splitDir, float sizeRatioForNodeAtDir,
            uint* outIdAtDir, uint* outIdAtOppositeDir);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderDockWindow")]
        private static extern unsafe void DockBuilderDockWindow(byte* windowName, uint nodeId);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderFinish")]
        private static extern void DockBuilderFinish(uint nodeId);

        // Splits off the part of node on the dir side and returns its id; the remaining part stays in node
        private static unsafe uint Split(ref uint node, ImGuiDir dir, float ratio)
        {
            uint atDir;
            uint opposite;
            DockBuilderSplitNode(node, dir, ratio, &atDir, &opposite);
            node = opposite;
            return atDir;
        }

        private static unsafe void Dock(string windowTitle, uint nodeId)
        {
            byte[] name = Encoding.UTF8.GetBytes(windowTitle + "\0");
            fixed (byte* namePtr = name) DockBuilderDockWindow(namePtr, nodeId);
        }
    }
}
