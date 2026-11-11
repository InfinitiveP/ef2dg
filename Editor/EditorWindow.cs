namespace ef2dg
{
    // Base editor window class: a title for docking and a visibility flag for the View menu
    public abstract class EditorWindow
    {
        // Title = window ID in imgui.ini and in the docking layout; do not change it without resetting the layout
        public abstract string Title { get; }

        // A field, not a property: passed by ref to ImGui.Begin so the cross button closes the window
        public bool IsOpen = true;

        public abstract void Draw(EditorContext ctx, EditorState state);
    }
}
