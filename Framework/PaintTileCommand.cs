using Silk.NET.Maths;

namespace ef2dg
{
    public class PaintTileCommand : ICommand
    {
        private TileLayer _layer;
        private Vector2D<int> _gridPos;
        private string _oldPath;
        private string _newPath;

        public PaintTileCommand(TileLayer layer, Vector2D<int> gridPos, string newPath)
        {
            _layer = layer;
            _gridPos = gridPos;
            _newPath = newPath;
            _oldPath = _layer.Grid.ContainsKey(_gridPos) ? _layer.Grid[_gridPos] : "";
        }

        public void Execute()
        {
            if (!string.IsNullOrEmpty(_newPath)) _layer.Grid[_gridPos] = _newPath;
            else _layer.Grid.Remove(_gridPos);
        }

        public void Undo()
        {
            if (!string.IsNullOrEmpty(_oldPath)) _layer.Grid[_gridPos] = _oldPath;
            else _layer.Grid.Remove(_gridPos);
        }
    }
}