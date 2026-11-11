# Editor

🌐 **English** · [Русский](../../ef2dg_Docs/04_Editor_Tools/EditorUI.md)

`EditorUI` combines the Dear ImGui windows and the shared `EditorState`.

- `MainMenuBar` — the `File`, `Edit`, `View`, `Run`, `Help` menus, the Play/Stop button and hotkeys.
- `ToolbarWindow` — the same Play/Stop, Undo/Redo, save and load commands as a separate window (hidden by default).
- `HierarchyWindow` shows entities, creates new ones and selects the active one.
- `SceneViewWindow` shows the framebuffer and handles selection, dragging, tile painting and scene buttons.
- `InspectorWindow` edits the transform and simple public component properties through reflection.
- `TilemapEditorWindow` manages layers, tile settings and collision type.
- `AssetBrowserWindow` browses `assets`, picks the tile brush and previews audio.
- `LightingWindow` configures scene lighting and adds light sources.
- `ConsoleWindow` shows intercepted `Console.WriteLine` output; `ProjectSettingsWindow` saves `project_config.json`.

Levels are saved through [SceneSerializer](../03_Modules/SceneSerializer.md). Loading a JSON file creates or reuses a tab by `InstanceName`; it is not hot reload and should not be called that.

## Key limitations

- Play Mode works on the same objects as Edit Mode. Changes made during play are not rolled back after Stop.
- Undo and Redo cover only tile paint/erase commands. Transform, component, entity and settings changes cannot be undone.
- Closing a scene tab does not ask about unsaved changes.
- Dropping an external file onto the window copies it into the selected asset folder, overwriting a file with the same name.
- The size of the main framebuffer follows the free area of the `Scene View`, not the game window size.

Full description of every action: [Complete Editor Guide](Complete%20Editor%20Guide.md).
