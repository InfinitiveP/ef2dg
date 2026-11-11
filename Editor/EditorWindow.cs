namespace ef2dg
{
    // Базовый класс окна редактора: заголовок для докинга и флаг видимости для меню View
    public abstract class EditorWindow
    {
        // Заголовок = ID окна в imgui.ini и в раскладке докинга, менять его нельзя без сброса раскладки
        public abstract string Title { get; }

        // Поле, а не свойство: передаётся по ref в ImGui.Begin, чтобы крестик закрывал окно
        public bool IsOpen = true;

        public abstract void Draw(EditorContext ctx, EditorState state);
    }
}
