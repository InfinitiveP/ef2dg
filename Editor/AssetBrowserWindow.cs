using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.IO;
using System.Numerics;

namespace ef2dg
{
    public class AssetBrowserWindow : EditorWindow
    {
        public override string Title => "Asset Browser";

        private const string RootFolder = "assets";
        
        // Переменные для отслеживания предпросмотра
        private string? _selectedFilePath = null;
        private nint _previewTextureId = 0;
        private string? _loadedPreviewPath = null;

        public override void Draw(EditorContext ctx, EditorState state)
        {
            ImGui.SetNextWindowSize(new Vector2(350, 500), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(20, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin(Title, ref IsOpen);

            if (!Directory.Exists(RootFolder))
            {
                Directory.CreateDirectory(RootFolder);
            }

            // Если папка для импорта не выбрана, ставим корневую
            if (string.IsNullOrEmpty(state.CurrentAssetFolder) || !Directory.Exists(state.CurrentAssetFolder))
            {
                state.CurrentAssetFolder = RootFolder;
            }

            // Вычисляем высоту: оставляем нижние 120 пикселей под панель предпросмотра
            float footerHeight = 120;
            var contentSize = new Vector2(0, ImGui.GetContentRegionAvail().Y - footerHeight);

            // =========================================================
            // ВЕРХНЯЯ ЧАСТЬ: ДЕРЕВО ИЕРАРХИИ (HIERARCHY TREE)
            // =========================================================
            // Включаем красивые линии соединения узлов (как в VS Code)
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 16.0f);
            
            ImGui.BeginChild("##AssetTree", contentSize, ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);
            
            // Выводим корневую папку как открытый по умолчанию узел
            ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (state.CurrentAssetFolder == RootFolder) rootFlags |= ImGuiTreeNodeFlags.Selected;

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Желтоватый корень
            bool rootOpen = ImGui.TreeNodeEx($"[+] {RootFolder}", rootFlags);
            ImGui.PopStyleColor();

            if (ImGui.IsItemClicked())
            {
                state.CurrentAssetFolder = RootFolder;
                _selectedFilePath = null; // Сбрасываем фокус файла
            }

            if (rootOpen)
            {
                // Запускаем рекурсивную отрисовку содержимого
                RenderDirectoryTree(RootFolder, ctx, state);
                ImGui.TreePop();
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(); // IndentSpacing

            ImGui.Separator();

            // =========================================================
            // НИЖНЯЯ ЧАСТЬ: ПАНЕЛЬ ПРЕДПРОСМОТРА (PREVIEW PANEL)
            // =========================================================
            ImGui.BeginChild("##AssetPreview", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
            
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "Preview:");
            ImGui.SameLine();

            if (_selectedFilePath != null && File.Exists(_selectedFilePath))
            {
                string fileName = Path.GetFileName(_selectedFilePath);
                string ext = Path.GetExtension(fileName).ToLower();

                // Сокращаем длинные имена для вывода
                string dispName = fileName.Length > 25 ? fileName.Substring(0, 22) + "..." : fileName;
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), dispName);

                // Отрисовка превью в зависимости от расширения
                if (ext == ".png")
                {
                    // Кэшируем загрузку текстуры превью, чтобы не грузить диск каждый кадр
                    if (_loadedPreviewPath != _selectedFilePath)
                    {
                        var tex = ctx.Resources.GetTexture(_selectedFilePath);
                        _previewTextureId = tex != null ? (nint)tex.Handle : 0;
                        _loadedPreviewPath = _selectedFilePath;
                    }

                    if (_previewTextureId != 0)
                    {
                        // Рисуем миниатюру с сохранением соотношения сторон (макс высота 70px)
                        var tex = ctx.Resources.GetTexture(_selectedFilePath);
                        if (tex != null)
                        {
                            float aspect = (float)tex.Width / tex.Height;
                            float pHeight = 70.0f;
                            float pWidth = pHeight * aspect;
                            
                            // Центрируем картинку по вертикали
                            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - pWidth) * 0.5f);
                            ImGui.Image(_previewTextureId, new Vector2(pWidth, pHeight), new Vector2(0, 0), new Vector2(1, 1));
                        }
                    }
                }
                else if (ext == ".wav" || ext == ".mp3")
                {
                    ImGui.Spacing();
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 100) * 0.5f);
                    
                    // Кнопка проигрывания звука
                    if (ImGui.Button("Play", new Vector2(100, 30)))
                    {
                        try 
                        {
                            // ИСПРАВЛЕНИЕ: Вызываем актуальный метод PlaySFX твоего движка,
                            // передавая путь к файлу, нулевую позицию для UI-звука и полную громкость
                            AudioLocator.Get().PlaySFX(_selectedFilePath, new Vector2D<float>(0, 0), 1.0f);
                        } 
                        catch (Exception ex) 
                        {
                            Console.WriteLine($"[Preview ERROR] Failed to play audio '{_selectedFilePath}': {ex.Message}");
                        }
                    }

                    ImGui.SameLine();
                    
                    // Кнопка мгновенной остановки
                    if (ImGui.Button("Stop", new Vector2(80, 30)))
                    {
                        try 
                        {
                            AudioLocator.Get().StopAll();
                        } 
                        catch (Exception ex) 
                        {
                            Console.WriteLine($"[Preview ERROR] Failed to stop audio: {ex.Message}");
                        }
                    }
                }
                else if (ext == ".json")
                {
                    ImGui.TextDisabled(" Scene file (Level data) ");
                }
                else
                {
                    ImGui.TextDisabled(" No preview available ");
                }
            }
            else
            {
                ImGui.TextDisabled(" None selected ");
            }

            ImGui.EndChild();
            ImGui.End();
        }

        // =========================================================
        // РЕКУРСИВНЫЙ РЕНДЕР ДЕРЕВА ДИРЕКТОРИЙ
        // =========================================================
        private void RenderDirectoryTree(string currentPath, EditorContext ctx, EditorState state)
        {
            try
            {
                // 1. Сначала выводим все подпапки
                var dirs = Directory.GetDirectories(currentPath);
                foreach (var dir in dirs)
                {
                    string dirName = Path.GetFileName(dir);
                    
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                    // Подсвечиваем папку, если она выбрана как текущая для импорта
                    if (state.CurrentAssetFolder == dir) flags |= ImGuiTreeNodeFlags.Selected;

                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.7f, 1.0f, 1.0f)); // Голубой
                    bool isOpen = ImGui.TreeNodeEx($"[+] {dirName}##{dir}", flags);
                    ImGui.PopStyleColor();

                    // Клик по папке делает её целевой для Drag-and-Drop
                    if (ImGui.IsItemClicked())
                    {
                        state.CurrentAssetFolder = dir;
                        _selectedFilePath = null;
                    }

                    if (isOpen)
                    {
                        RenderDirectoryTree(dir, ctx, state);
                        ImGui.TreePop();
                    }
                }

                // 2. Затем выводим файлы в текущей папке
                var files = Directory.GetFiles(currentPath);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string ext = Path.GetExtension(fileName).ToLower();

                    // Определение цвета файла
                    Vector4 fileColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                    if (ext == ".png") fileColor = new Vector4(0.5f, 1.0f, 0.5f, 1.0f); // Зеленый
                    else if (ext == ".json") fileColor = new Vector4(1.0f, 0.6f, 0.2f, 1.0f); // Оранжевый
                    else if (ext == ".wav" || ext == ".mp3") fileColor = new Vector4(1.0f, 0.4f, 0.8f, 1.0f); // Розовый

                    ImGui.PushStyleColor(ImGuiCol.Text, fileColor);
                    
                    // Рисуем узел-лист (Leaf) без стрелочки
                    ImGuiTreeNodeFlags leafFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanAvailWidth;
                    if (_selectedFilePath == file) leafFlags |= ImGuiTreeNodeFlags.Selected;

                    ImGui.TreeNodeEx($"{fileName}##{file}", leafFlags);
                    ImGui.PopStyleColor();

                    // Одинарный клик: выбираем файл для панели предпросмотра
                    if (ImGui.IsItemClicked())
                    {
                        _selectedFilePath = file;
                        // Автоматически выделяем родительскую папку для удобства импорта
                        state.CurrentAssetFolder = currentPath; 
                    }

                    // Двойной клик по спрайту: активация кисти тайлов
                    if (ext == ".png" && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        state.SelectedTilePath = file.Replace('\\', '/');
                        state.CurrentTool = EditorState.ToolMode.PaintTiles;
                        Console.WriteLine($"[AssetBrowser] Activated tile brush: {state.SelectedTilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                ImGui.TextDisabled($"Error: {ex.Message}");
            }
        }
    }
}