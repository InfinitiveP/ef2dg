using ImGuiNET;
using Silk.NET.Maths;

namespace ef2dg
{
    public struct EditorContext
    {
        public Scene CurrentScene;
        public Framebuffer Fbo;
        public Camera GameCamera;
        public Vector2D<int> WindowSize;
        public ResourceManager Resources;
        public Action<Scene> ChangeScene;
        public Action<Scene> PushScene;
        public Action PopScene;
        public Func<string, bool, TransitionSettings?, bool> RequestSceneTransition;
        public Action Exit;
    }

    public class EditorUI
    {
        private EditorState _state = new EditorState();

        // Initialize our independent window classes
        private ToolbarWindow _toolbar = new ToolbarWindow();
        private HierarchyWindow _hierarchy = new HierarchyWindow();
        private SceneViewWindow _sceneView = new SceneViewWindow();
        private InspectorWindow _inspector = new InspectorWindow();
        private ProjectSettingsWindow _projectSettings = new ProjectSettingsWindow();
        private TilemapEditorWindow _tilemapEditor = new TilemapEditorWindow();
        private ConsoleWindow _consoleWindow = new ConsoleWindow();
        private AssetBrowserWindow _assetBrowser = new AssetBrowserWindow();
        private LightingWindow _lighting = new LightingWindow();

        private readonly List<EditorWindow> _windows;
        private readonly EditorLayout _layout;
        private readonly MainMenuBar _menuBar = new MainMenuBar();
        private readonly SceneFileDialogs _fileDialogs = new SceneFileDialogs();

        public bool IsPlayMode => _state.IsPlayMode;
        public EditorState State => _state;

        public EditorUI()
        {
            // Order matters: windows are drawn in this order (Scene View picks an object before the Inspector), and the View menu lists them the same way
            _windows = new List<EditorWindow>
            {
                _hierarchy, _sceneView, _inspector, _lighting, _assetBrowser,
                _consoleWindow, _tilemapEditor, _projectSettings, _toolbar
            };
            _layout = new EditorLayout(_windows);
        }

        public void UpdateRuntimePointer(Camera camera) => _sceneView.UpdateRuntimePointer(_state, camera);

        public void Draw(EditorContext ctx)
        {
            if (ctx.CurrentScene == null) return;

            _menuBar.HandleShortcuts(ctx, _state);
            // Menu first: it reserves the top of the screen, and the docking area takes the remaining space
            _menuBar.Draw(ctx, _state, _windows, _layout);
            _layout.DrawDockHost(() => _fileDialogs.Draw(ctx, _state));
            _layout.DrawStatusBar(ctx, _state);

            foreach (var window in _windows)
            {
                if (window.IsOpen) window.Draw(ctx, _state);
            }
            if (!_sceneView.IsOpen) _sceneView.MarkHidden();

            _layout.EndFrame();
        }
    }
}
