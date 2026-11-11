using ImGuiNET;
using System.Numerics;

namespace ef2dg
{
    public class ConsoleWindow : EditorWindow
    {
        public override string Title => "Console (Terminal)";

        // Make the interceptor globally accessible
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

            // Create an inner scrolling region (size 0,0 takes all remaining window space)
            if (ImGui.BeginChild("LogRegion", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                // Lock the collection while drawing to avoid crashes with asynchronous logging
                lock (Interceptor.Logs)
                {
                    foreach (var log in Interceptor.Logs)
                    {
                        // Highlight important messages; the Russian keywords match logs written by Russian-language game code
                        bool isError = log.Contains("ОШИБКА") || log.Contains("ERROR") || log.Contains("Failed");
                        bool isEvent = log.Contains("[EVENT]");
                        bool isWarning = log.Contains("WARNING") || log.Contains("заглушка");

                        if (isError) 
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f)); // Red
                        else if (isEvent) 
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 1.0f, 1.0f)); // Light blue
                        else if (isWarning) 
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Yellow

                        // TextUnformatted is the fastest and does not break on characters like %
                        ImGui.TextUnformatted(log);

                        if (isError || isEvent || isWarning) 
                            ImGui.PopStyleColor();
                    }

                    // If auto-scroll is on and new messages arrived, scroll to the very bottom
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