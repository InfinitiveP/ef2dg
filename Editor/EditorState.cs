using Silk.NET.Maths;

namespace ef2dg
{
    public class EditorState
    {
        public GameObject? SelectedEntity { get; set; } = null;
        public Vector2D<float> DragOffset { get; set; } = new Vector2D<float>(0, 0);
        public string? WaitingForBindAction { get; set; } = null;
        public bool IsPlayMode { get; set; } = false;
        public string TextureSearchFilter { get; set; } = "";
        public string NewStateName { get; set; } = "NewState";
        public List<Scene> OpenedScenes { get; set; } = new List<Scene>();

        // ==========================================
        // LEVEL EDITOR VARIABLES
        // ==========================================
        public CommandHistory History { get; set; } = new CommandHistory();
        
        public enum ToolMode { Select, PaintTiles, EraseTiles }
        public ToolMode CurrentTool { get; set; } = ToolMode.Select;

        public int SelectedTileLayerIndex { get; set; } = 1; 
        public string SelectedTilePath { get; set; } = "";
        public string CurrentAssetFolder { get; set; } = "assets";

        // Cell coordinates for drawing the brush
        public Vector2D<int>? HoveredGridCell { get; set; } = null;
        public string AudioSearchFilter { get; set; } = "";
        public string NewAudioKey { get; set; } = ""; // For creating new SFX keys
        public int NewAudioFrame { get; set; } = 3;
        public string NewAudioSfxKey { get; set; } = "Hit";
        public float EditorCameraZoom { get; set; } = 1.0f;

        // Requests to open file dialogs (from the File menu, the Toolbar and hotkeys)
        public bool RequestSaveAsDialog { get; set; } = false;
        public bool RequestOpenDialog { get; set; } = false;
    }
}