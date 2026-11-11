using ImGuiNET;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace ef2dg
{
    // Dialogs for saving and loading levels. Opened by EditorState flags from the File menu, the Toolbar and hotkeys.
    public class SceneFileDialogs
    {
        // JSON files next to levels that are not scenes
        private static readonly string[] NonSceneFiles =
        {
            "bindings.json", "level_backup.json", "project_config.json", "lighting_presets.json", EditorLayout.SettingsFileName
        };

        // Default file name
        private string _fileNameInput = "level.json";

        // Ctrl+S: saves to the file the scene was loaded from, otherwise opens "Save As"
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

            // If the user forgot to type .json, add it automatically
            if (!_fileNameInput.EndsWith(".json")) _fileNameInput += ".json";

            if (ImGui.Button("Confirm Save", new Vector2(125, 0)) || submitted)
            {
                // Work only if the scene really exists
                if (ctx.CurrentScene != null)
                {
                    // Store the file name in the scene itself before serialization
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

            // List every available level file in the folder
            ImGui.Separator();
            ImGui.TextDisabled("Available levels:");

            if (ImGui.BeginChild("##levels", new Vector2(260, 220), ImGuiChildFlags.Border))
            {
                var jsonFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.json");
                foreach (var file in jsonFiles)
                {
                    string fileName = Path.GetFileName(file);

                    // Skip system files
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

            // The error modal opens on top of the load dialog
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

                // Read the metadata straight from the JSON
                string sTypeStr = doc.RootElement.TryGetProperty("SceneType", out var st) ? st.GetString() ?? "GameScene" : "GameScene";
                string targetCleanName = Path.GetFileNameWithoutExtension(fileName);
                string instanceNameStr = doc.RootElement.TryGetProperty("InstanceName", out var inProp) ? inProp.GetString() ?? targetCleanName : targetCleanName;

                // 1. LOOK FOR AN EXISTING TAB BY NAME
                Scene? existingScene = state.OpenedScenes.FirstOrDefault(s =>
                    s.InstanceName == instanceNameStr ||
                    s.InstanceName == targetCleanName ||
                    s.GetType().Name == targetCleanName);

                Scene targetSceneToLoad;

                // 2. MERGE OR CREATE
                if (existingScene != null && existingScene.GetType().Name == sTypeStr)
                {
                    // Use the already opened scene without creating a duplicate
                    targetSceneToLoad = existingScene;
                    targetSceneToLoad.Entities.Clear(); // Clear the old entities
                }
                else
                {
                    // Create a new one only if no such tab exists
                    Type? sceneType = Type.GetType($"ef2dg.{sTypeStr}");
                    targetSceneToLoad = (sceneType != null)
                        ? (Scene)Activator.CreateInstance(sceneType)!
                        : new GameScene();

                    state.OpenedScenes.Add(targetSceneToLoad); // Register the new tab
                }

                if (targetSceneToLoad is GameScene gs) gs.IsLoadedFromFile = true;
                targetSceneToLoad.InstanceName = instanceNameStr;
                targetSceneToLoad.Map = new Tilemap(0.5f);

                // Load the data
                if (!SceneSerializer.Load(targetSceneToLoad, fileName, ctx.Resources)) return false;

                state.SelectedEntity = null;
                // Switch focus to the updated tab
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
