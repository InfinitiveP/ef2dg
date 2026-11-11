using ImGuiNET;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ef2dg
{
    public static unsafe class ImGuiConfigurator
    {
        // Static glyph array: Latin, Cyrillic and typographic symbols
        private static readonly ushort[] _customRanges = new ushort[]
        {
            0x0020, 0x00FF, // ASCII and Latin
            0x0400, 0x052F, // Cyrillic (Russian, Ukrainian)
            0x2000, 0x206F, // Punctuation: em dash, ellipsis, quotes
            0x2190, 0x21FF, // Arrows
            0x25A0, 0x25FF, // Geometric shapes (▶ ■), if the font has them
            0               // Mandatory zero terminator
        };

        // Handle that pins the array in C# memory for the lifetime of the app
        private static GCHandle _rangesHandle;

        private static readonly string SystemUiFontPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");

        public static void ConfigureIO()
        {
            var io = ImGui.GetIO();

            // Enable navigation and window docking
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            // The obsolete manual io.KeyMap table has been REMOVED COMPLETELY.
            // The modern Silk.NET ImGuiController routes keys automatically.

            EditorTheme.ApplyVsCodeDark();

            // Custom font: Segoe UI, as in VS Code on Windows, otherwise the project font
            string fontPath = File.Exists(SystemUiFontPath) ? SystemUiFontPath : "assets/fonts/Roboto-Medium.ttf";

            if (File.Exists(fontPath))
            {
                io.Fonts.Clear();

                // Reliably pin the array in unmanaged memory
                if (!_rangesHandle.IsAllocated)
                {
                    _rangesHandle = GCHandle.Alloc(_customRanges, GCHandleType.Pinned);
                }
                IntPtr rangesPtr = _rangesHandle.AddrOfPinnedObject();

                // Load a font that supports Russian/Ukrainian
                io.Fonts.AddFontFromFileTTF(fontPath, 16.0f, default, rangesPtr);
                
                Console.WriteLine($"[ImGuiConfig] Successfully loaded Unicode font: {fontPath}");
            }
            else
            {
                Console.WriteLine($"[ImGuiConfig] WARNING: Font file NOT FOUND at '{fontPath}'. Using default ASCII font.");
            }
        }
    }
}