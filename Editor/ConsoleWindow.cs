using ImGuiNET;
using System.Numerics;

namespace ef2dg
{
    public class ConsoleWindow : EditorWindow
    {
        public override string Title => "Console (Terminal)";

        // Делаем перехватчик глобально доступным
        public static ConsoleInterceptor Interceptor { get; } = new ConsoleInterceptor();

        private bool _autoScroll = true;
        private int _oldLogCount = 0;

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(500, 250), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(20, 450), ImGuiCond.FirstUseEver);

            ImGui.Begin(Title, ref IsOpen);

            if (ImGui.Button("Clear"))
            {
                Interceptor.Clear();
            }
            ImGui.SameLine();
            ImGui.Checkbox("Auto-scroll", ref _autoScroll);

            ImGui.Separator();

            // Создаем внутреннюю область с прокруткой (размер 0,0 занимает всё оставшееся место окна)
            if (ImGui.BeginChild("LogRegion", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                // Блокируем коллекцию на время отрисовки, чтобы избежать крашей при асинхронном логгировании
                lock (Interceptor.Logs)
                {
                    foreach (var log in Interceptor.Logs)
                    {
                        // Подсветка важных сообщений
                        bool isError = log.Contains("ОШИБКА") || log.Contains("ERROR") || log.Contains("Failed");
                        bool isEvent = log.Contains("[EVENT]");
                        bool isWarning = log.Contains("WARNING") || log.Contains("заглушка");

                        if (isError) 
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f)); // Красный
                        else if (isEvent) 
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 1.0f, 1.0f)); // Голубой
                        else if (isWarning) 
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Желтый

                        // TextUnformatted работает быстрее всего и не ломается от символов вроде %
                        ImGui.TextUnformatted(log);

                        if (isError || isEvent || isWarning) 
                            ImGui.PopStyleColor();
                    }

                    // Если включен автоскролл и появились новые сообщения — автоматически крутим в самый низ
                    if (_autoScroll && Interceptor.Logs.Count > _oldLogCount)
                    {
                        ImGui.SetScrollHereY(1.0f);
                    }
                    _oldLogCount = Interceptor.Logs.Count;
                }
            }
            ImGui.EndChild();

            ImGui.End();
        }
    }
}