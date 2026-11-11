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
        
        // Variables for tracking the preview
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

            // If no import folder is selected, use the root
            if (string.IsNullOrEmpty(state.CurrentAssetFolder) || !Directory.Exists(state.CurrentAssetFolder))
            {
                state.CurrentAssetFolder = RootFolder;
            }

            // Compute the height: keep the bottom 120 pixels for the preview panel
            float footerHeight = 120;
            var contentSize = new Vector2(0, ImGui.GetContentRegionAvail().Y - footerHeight);

            // =========================================================
            // TOP PART: HIERARCHY TREE
            // =========================================================
            // Enable nice node connection lines (as in VS Code)
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 16.0f);
            
            ImGui.BeginChild("##AssetTree", contentSize, ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);
            
            // Show the root folder as a node that is open by default
            ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (state.CurrentAssetFolder == RootFolder) rootFlags |= ImGuiTreeNodeFlags.Selected;

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Yellowish root
            bool rootOpen = ImGui.TreeNodeEx($"[+] {RootFolder}", rootFlags);
            ImGui.PopStyleColor();

            if (ImGui.IsItemClicked())
            {
                state.CurrentAssetFolder = RootFolder;
                _selectedFilePath = null; // Reset the file focus
            }

            if (rootOpen)
            {
                // Start the recursive drawing of the contents
                RenderDirectoryTree(RootFolder, ctx, state);
                ImGui.TreePop();
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(); // IndentSpacing

            ImGui.Separator();

            // =========================================================
            // BOTTOM PART: PREVIEW PANEL
            // =========================================================
            ImGui.BeginChild("##AssetPreview", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
            
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "Preview:");
            ImGui.SameLine();

            if (_selectedFilePath != null && File.Exists(_selectedFilePath))
            {
                string fileName = Path.GetFileName(_selectedFilePath);
                string ext = Path.GetExtension(fileName).ToLower();

                // Shorten long names for display
                string dispName = fileName.Length > 25 ? fileName.Substring(0, 22) + "..." : fileName;
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), dispName);

                // Draw the preview depending on the extension
                if (ext == ".png")
                {
                    // Cache the preview texture so the disk is not hit every frame
                    if (_loadedPreviewPath != _selectedFilePath)
                    {
                        var tex = ctx.Resources.GetTexture(_selectedFilePath);
                        _previewTextureId = tex != null ? (nint)tex.Handle : 0;
                        _loadedPreviewPath = _selectedFilePath;
                    }

                    if (_previewTextureId != 0)
                    {
                        // Draw a thumbnail keeping the aspect ratio (max height 70px)
                        var tex = ctx.Resources.GetTexture(_selectedFilePath);
                        if (tex != null)
                        {
                            float aspect = (float)tex.Width / tex.Height;
                            float pHeight = 70.0f;
                            float pWidth = pHeight * aspect;
                            
                            // Centre the image vertically
                            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - pWidth) * 0.5f);
                            ImGui.Image(_previewTextureId, new Vector2(pWidth, pHeight), new Vector2(0, 0), new Vector2(1, 1));
                        }
                    }
                }
                else if (ext == ".wav" || ext == ".mp3")
                {
                    ImGui.Spacing();
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 100) * 0.5f);
                    
                    // Sound playback button
                    if (ImGui.Button("Play", new Vector2(100, 30)))
                    {
                        try 
                        {
                            // Call the engine's PlaySFX method,
                            // passing the file path, a zero position for UI sound and full volume
                            AudioLocator.Get().PlaySFX(_selectedFilePath, new Vector2D<float>(0, 0), 1.0f);
                        } 
                        catch (Exception ex) 
                        {
                            Console.WriteLine($"[Preview ERROR] Failed to play audio '{_selectedFilePath}': {ex.Message}");
                        }
                    }

                    ImGui.SameLine();
                    
                    // Instant stop button
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
        // RECURSIVE DIRECTORY TREE RENDERING
        // =========================================================
        private void RenderDirectoryTree(string currentPath, EditorContext ctx, EditorState state)
        {
            try
            {
                // 1. First list all subfolders
                var dirs = Directory.GetDirectories(currentPath);
                foreach (var dir in dirs)
                {
                    string dirName = Path.GetFileName(dir);
                    
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                    // Highlight the folder if it is the current import target
                    if (state.CurrentAssetFolder == dir) flags |= ImGuiTreeNodeFlags.Selected;

                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.7f, 1.0f, 1.0f)); // Light blue
                    bool isOpen = ImGui.TreeNodeEx($"[+] {dirName}##{dir}", flags);
                    ImGui.PopStyleColor();

                    // Clicking a folder makes it the drag-and-drop target
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

                // 2. Then list the files in the current folder
                var files = Directory.GetFiles(currentPath);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string ext = Path.GetExtension(fileName).ToLower();

                    // File colour
                    Vector4 fileColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                    if (ext == ".png") fileColor = new Vector4(0.5f, 1.0f, 0.5f, 1.0f); // Green
                    else if (ext == ".json") fileColor = new Vector4(1.0f, 0.6f, 0.2f, 1.0f); // Orange
                    else if (ext == ".wav" || ext == ".mp3") fileColor = new Vector4(1.0f, 0.4f, 0.8f, 1.0f); // Pink

                    ImGui.PushStyleColor(ImGuiCol.Text, fileColor);
                    
                    // Draw a leaf node without an arrow
                    ImGuiTreeNodeFlags leafFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanAvailWidth;
                    if (_selectedFilePath == file) leafFlags |= ImGuiTreeNodeFlags.Selected;

                    ImGui.TreeNodeEx($"{fileName}##{file}", leafFlags);
                    ImGui.PopStyleColor();

                    // Single click: select the file for the preview panel
                    if (ImGui.IsItemClicked())
                    {
                        _selectedFilePath = file;
                        // Automatically select the parent folder for convenient import
                        state.CurrentAssetFolder = currentPath; 
                    }

                    // Double click on a sprite: activate the tile brush
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