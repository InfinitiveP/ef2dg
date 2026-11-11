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
        // НОВЫЕ ПЕРЕМЕННЫЕ ДЛЯ РЕДАКТОРА УРОВНЕЙ
        // ==========================================
        public CommandHistory History { get; set; } = new CommandHistory();
        
        public enum ToolMode { Select, PaintTiles, EraseTiles }
        public ToolMode CurrentTool { get; set; } = ToolMode.Select;

        public int SelectedTileLayerIndex { get; set; } = 1; 
        public string SelectedTilePath { get; set; } = "";
        public string CurrentAssetFolder { get; set; } = "assets";

        // Координаты клетки для отрисовки кисти
        public Vector2D<int>? HoveredGridCell { get; set; } = null;
        public string AudioSearchFilter { get; set; } = "";
        public string NewAudioKey { get; set; } = ""; // Для создания новых ключей SFX
        public int NewAudioFrame { get; set; } = 3;
        public string NewAudioSfxKey { get; set; } = "Hit";
        public float EditorCameraZoom { get; set; } = 1.0f;

        // Запросы на открытие диалогов файлов (из меню File, Toolbar и горячих клавиш)
        public bool RequestSaveAsDialog { get; set; } = false;
        public bool RequestOpenDialog { get; set; } = false;
    }
}