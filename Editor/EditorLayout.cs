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
    // Раскладка редактора в стиле VS Code: область докинга на всё окно между главным меню и строкой состояния,
    // раскладка по умолчанию и сохранение видимости окон между запусками.
    public class EditorLayout
    {
        public const string SettingsFileName = "editor_layout.json";

        // Увеличить, если поменялась раскладка по умолчанию: у всех применится новая при следующем запуске
        private const int CurrentLayoutVersion = 1;

        // ImGuiDockNodeFlags_DockSpace — внутренний флаг imgui, в обёртке ImGui.NET его нет
        private const int DockSpaceNodeFlag = 1 << 10;

        private class LayoutSettings
        {
            public int LayoutVersion { get; set; }
            public Dictionary<string, bool> OpenWindows { get; set; } = new();
        }

        // Вкладки, которые после сброса раскладки должны быть активными в своих панелях (последняя получает фокус)
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

        // Окно-хост с DockSpace. inHost рисуется внутри хоста: там живут модальные диалоги
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

        // Строка состояния внизу: синяя в режиме редактирования, оранжевая в Play Mode (как отладка в VS Code)
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

        // Как в VS Code: слева иерархия и ассеты, по центру сцена, справа инспектор, снизу консоль
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
            // Toolbar дублирует меню и по умолчанию скрыт; если его открыть, он будет плавающим
            Dock("Toolbar", 0);
            DockBuilderFinish(dockId);

            foreach (var window in _windows) window.IsOpen = window.Title != "Toolbar";
            _focusDefaultTabs = true;
        }

        // Вызывается после отрисовки всех окон кадра
        public void EndFrame()
        {
            if (_focusDefaultTabs)
            {
                // Окна уже пристыкованы в этом кадре — фокус выбирает нужную вкладку в каждой панели
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

            // Первый запуск новой раскладки: строим её в первом кадре
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
        // DockBuilder из imgui_internal.h: экспортируется нативной cimgui.dll, но не обёрнут в ImGui.NET
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

        // Отрезает от node часть со стороны dir и возвращает её id; в node остаётся оставшаяся часть
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
