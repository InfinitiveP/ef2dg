using ImGuiNET;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ef2dg
{
    public static unsafe class ImGuiConfigurator
    {
        // Статический массив глифов: латиница, кириллица и типографские символы
        private static readonly ushort[] _customRanges = new ushort[]
        {
            0x0020, 0x00FF, // ASCII и латиница
            0x0400, 0x052F, // Кириллица (русский, украинский)
            0x2000, 0x206F, // Пунктуация: длинное тире, многоточие, кавычки
            0x2190, 0x21FF, // Стрелки
            0x25A0, 0x25FF, // Геометрические фигуры (▶ ■), если они есть в шрифте
            0               // Обязательный нуль-терминатор
        };

        // Дескриптор для вечной фиксации указателя в памяти C#
        private static GCHandle _rangesHandle;

        private static readonly string SystemUiFontPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");

        public static void ConfigureIO()
        {
            var io = ImGui.GetIO();

            // Включаем навигацию и поддержку докинга окон
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            // Устаревшая ручная таблица io.KeyMap ПОЛНОСТЬЮ УДАЛЕНА.
            // Современный Silk.NET ImGuiController маршрутизирует клавиши автоматически.

            EditorTheme.ApplyVsCodeDark();

            // Загрузка кастомного шрифта: Segoe UI, как в VS Code на Windows, иначе шрифт проекта
            string fontPath = File.Exists(SystemUiFontPath) ? SystemUiFontPath : "assets/fonts/Roboto-Medium.ttf";

            if (File.Exists(fontPath))
            {
                io.Fonts.Clear();

                // Надежно фиксируем массив в неуправляемой памяти
                if (!_rangesHandle.IsAllocated)
                {
                    _rangesHandle = GCHandle.Alloc(_customRanges, GCHandleType.Pinned);
                }
                IntPtr rangesPtr = _rangesHandle.AddrOfPinnedObject();

                // Загружаем шрифт с поддержкой русского/украинского языков
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