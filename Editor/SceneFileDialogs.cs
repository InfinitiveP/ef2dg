using ImGuiNET;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace ef2dg
{
    // Диалоги сохранения и загрузки уровней. Открываются флагами EditorState из меню File, Toolbar и горячих клавиш.
    public class SceneFileDialogs
    {
        // JSON-файлы рядом с уровнями, которые не являются сценами
        private static readonly string[] NonSceneFiles =
        {
            "bindings.json", "level_backup.json", "project_config.json", "lighting_presets.json", EditorLayout.SettingsFileName
        };

        // Имя файла по умолчанию
        private string _fileNameInput = "level.json";

        // Ctrl+S: сохраняет в файл, из которого сцена загружена, иначе открывает «Сохранить как»
        public static void QuickSave(EditorContext ctx, EditorState state)
        {
            var scene = ctx.CurrentScene;
            if (scene == null) return;

            if (string.IsNullOrEmpty(scene.FilePath))
            {
                state.RequestSaveAsDialog = true;
                return;
            }

            SceneSerializer.Save(scene, scene.FilePath);
            Console.WriteLine($"[Editor] Saved {scene.FilePath}");
        }

        public void Draw(EditorContext ctx, EditorState state)
        {
            if (state.RequestSaveAsDialog)
            {
                state.RequestSaveAsDialog = false;
                if (ctx.CurrentScene != null && !string.IsNullOrEmpty(ctx.CurrentScene.FilePath))
                    _fileNameInput = Path.GetFileName(ctx.CurrentScene.FilePath);
                ImGui.OpenPopup("Save Scene As");
            }
            if (state.RequestOpenDialog)
            {
                state.RequestOpenDialog = false;
                ImGui.OpenPopup("Open Scene");
            }

            DrawSavePopup(ctx);
            DrawLoadPopup(ctx, state);
        }

        private void DrawSavePopup(EditorContext ctx)
        {
            bool open = true;
            if (!ImGui.BeginPopupModal("Save Scene As", ref open, ImGuiWindowFlags.AlwaysAutoResize)) return;

            ImGui.Text("Enter file name to save:");
            ImGui.SetNextItemWidth(260);
            if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
            bool submitted = ImGui.InputText("##savefile", ref _fileNameInput, 128, ImGuiInputTextFlags.EnterReturnsTrue);

            // Если пользователь забыл написать .json, добавляем автоматически
            if (!_fileNameInput.EndsWith(".json")) _fileNameInput += ".json";

            if (ImGui.Button("Confirm Save", new Vector2(125, 0)) || submitted)
            {
                // Безопасно работаем только если сцена действительно существует
                if (ctx.CurrentScene != null)
                {
                    // Сохраняем имя файла в саму сцену перед сериализацией
                    string cleanName = Path.GetFileNameWithoutExtension(_fileNameInput);
                    ctx.CurrentScene.InstanceName = cleanName;

                    SceneSerializer.Save(ctx.CurrentScene, _fileNameInput);
                    Console.WriteLine($"[Editor] Saved {_fileNameInput}");
                }

                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(125, 0))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }

        private void DrawLoadPopup(EditorContext ctx, EditorState state)
        {
            bool open = true;
            if (!ImGui.BeginPopupModal("Open Scene", ref open, ImGuiWindowFlags.AlwaysAutoResize)) return;

            ImGui.Text("Select or enter file to load:");
            ImGui.SetNextItemWidth(260);
            bool submitted = ImGui.InputText("##loadfile", ref _fileNameInput, 128, ImGuiInputTextFlags.EnterReturnsTrue);

            // Выводим список всех доступных файлов уровней в папке
            ImGui.Separator();
            ImGui.TextDisabled("Available levels:");

            if (ImGui.BeginChild("##levels", new Vector2(260, 220), ImGuiChildFlags.Border))
            {
                var jsonFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.json");
                foreach (var file in jsonFiles)
                {
                    string fileName = Path.GetFileName(file);

                    // Пропускаем системные файлы
                    if (NonSceneFiles.Contains(fileName)) continue;

                    if (ImGui.Selectable(fileName, fileName == _fileNameInput, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _fileNameInput = fileName;
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) submitted = true;
                    }
                }
            }
            ImGui.EndChild();

            if (ImGui.Button("Confirm Load", new Vector2(125, 0)) || submitted)
            {
                if (TryLoad(ctx, state, _fileNameInput)) ImGui.CloseCurrentPopup();
                else ImGui.OpenPopup("LoadError");
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(125, 0))) ImGui.CloseCurrentPopup();

            // Модальное окно ошибки открывается поверх диалога загрузки
            if (ImGui.BeginPopupModal("LoadError", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "ERROR: Failed to load level!");
                ImGui.Text("The file might be corrupted or from an older version.");
                if (ImGui.Button("OK", new Vector2(120, 0))) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }

            ImGui.EndPopup();
        }

        private static bool TryLoad(EditorContext ctx, EditorState state, string fileName)
        {
            if (!File.Exists(fileName)) return false;

            try
            {
                string json = File.ReadAllText(fileName);
                using var doc = JsonDocument.Parse(json);

                // Читаем метаданные прямо из JSON
                string sTypeStr = doc.RootElement.TryGetProperty("SceneType", out var st) ? st.GetString() ?? "GameScene" : "GameScene";
                string targetCleanName = Path.GetFileNameWithoutExtension(fileName);
                string instanceNameStr = doc.RootElement.TryGetProperty("InstanceName", out var inProp) ? inProp.GetString() ?? targetCleanName : targetCleanName;

                // 1. ИЩЕМ СУЩЕСТВУЮЩУЮ ВКЛАДКУ ПО ИМЕНИ
                Scene? existingScene = state.OpenedScenes.FirstOrDefault(s =>
                    s.InstanceName == instanceNameStr ||
                    s.InstanceName == targetCleanName ||
                    s.GetType().Name == targetCleanName);

                Scene targetSceneToLoad;

                // 2. СЛИЯНИЕ ИЛИ СОЗДАНИЕ
                if (existingScene != null && existingScene.GetType().Name == sTypeStr)
                {
                    // Используем уже открытую сцену, не создавая дубликат
                    targetSceneToLoad = existingScene;
                    targetSceneToLoad.Entities.Clear(); // Вычищаем старых энтити
                }
                else
                {
                    // Создаем новую только если такой вкладки физически нет
                    Type? sceneType = Type.GetType($"ef2dg.{sTypeStr}");
                    targetSceneToLoad = (sceneType != null)
                        ? (Scene)Activator.CreateInstance(sceneType)!
                        : new GameScene();

                    state.OpenedScenes.Add(targetSceneToLoad); // Регистрируем новую вкладку
                }

                if (targetSceneToLoad is GameScene gs) gs.IsLoadedFromFile = true;
                targetSceneToLoad.InstanceName = instanceNameStr;
                targetSceneToLoad.Map = new Tilemap(0.5f);

                // Заливаем данные
                if (!SceneSerializer.Load(targetSceneToLoad, fileName, ctx.Resources)) return false;

                state.SelectedEntity = null;
                // Переключаем фокус на обновленную вкладку
                ctx.ChangeScene?.Invoke(targetSceneToLoad);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
