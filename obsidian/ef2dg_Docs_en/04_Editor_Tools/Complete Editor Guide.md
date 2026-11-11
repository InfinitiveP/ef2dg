# Complete Editor Guide

🌐 **English** · [Русский](../../ef2dg_Docs/04_Editor_Tools/Полное%20руководство%20по%20редактору.md)

The editor is built into the running engine and drawn every frame with Dear ImGui. It edits the current [Scene](../02_Architecture/Scene.md) instance directly: there is no separate document copy, transaction or automatic recovery.

## Workflow overview

1. Run the project from its folder.
2. Open an existing level with `File → Open...` (`Ctrl+O`) or create a scene with `File → New Scene` or the `+` button in the `Scene View`.
3. Pick the scene in the `Scene View` tabs.
4. Create entities in `Hierarchy`, add components in `Inspector`, paint the map with `Tile Palette`.
5. Save the scene with `File → Save` (`Ctrl+S`) or `File → Save As...` (`Ctrl+Shift+S`).
6. Press the green Play button in the middle of the menu bar (`Ctrl+P`) to test, and press it again to return to editing.

> [!CAUTION]
> **Play Mode is not isolated**
> Play does not clone the scene. Game logic changes the same objects that are open in the editor. After Stop, positions, deleted entities and public component state stay changed. Save the level before testing; to go back, reload the JSON.

## Main menu and window layout

The interface is styled after VS Code (the `EditorTheme` theme, the Segoe UI font when available on the system, otherwise Roboto from `assets/fonts`).

- **Main menu** (`MainMenuBar`) is always pinned to the top of the window: `File`, `Edit`, `View`, `Run`, `Help`. The Play/Stop button sits in the middle, the current scene name on the right.
- **Docking area** (`EditorLayout`) fills the window between the menu and the status bar. Windows can be dragged by their tab, docked to the edges, stacked into tabs and torn off into floating windows.
- **Closing and opening windows**: the cross in a title bar closes a window, `View` lists all windows with checkmarks. Window visibility is stored in `editor_layout.json`, position and docking in `imgui.ini`.
- **Default layout** (`View → Reset Layout`): `Hierarchy` and `Asset Browser` on the left, `Scene View` in the centre, `Inspector` and `Lighting` on the right, `Console`, `Tile Palette` and `Project Settings` at the bottom. The same layout is applied on the first launch of a new layout version (`EditorLayout.CurrentLayoutVersion`).
- **Status bar** at the bottom: blue in edit mode and orange in Play Mode; it shows the scene, entity count, selected object, tool, camera, zoom and FPS.
- **Escape** is not intercepted by the engine: in the editor it closes menus and dialogs, in Play Mode it is free for game bindings. Exit with `File → Exit` or `Alt+F4`.

Hotkeys: `Ctrl+S`, `Ctrl+Shift+S`, `Ctrl+O`, `Ctrl+Z`/`Ctrl+Y`, `Ctrl+P`, `Ctrl+Shift+N`; the full list is in `Help → Keyboard Shortcuts`. While a text field is active, `Ctrl` shortcuts and WASD camera movement are disabled.

## File, Edit and Run commands

The same actions are available in the `Toolbar` window (hidden by default, enabled from `View`).

### Play and Stop

Play stores the current editor `Camera.Zoom` in `EditorCameraZoom`, applies `ProjectConfig.Data.DefaultCameraZoom` and enables `FixedUpdate`/`Update`. Stop disables game updates and restores the editor zoom. Audio is paused when leaving Play Mode and resumed on return.

### Undo and Redo

The command history contains only `PaintTileCommand`: painting and erasing cells in the `Scene View`. Renaming, transforms, adding/removing entities or components, tile settings and level loading are not recorded.

A new command after Undo clears the Redo stack. The history belongs to `EditorState` as a whole, so it is not tied to a specific scene tab.

### Save and Save As

`Save` writes the scene to the file it was loaded from or last saved to (`Scene.FilePath`). If the scene has never been saved, `Save As` opens.

In `Save As` the name field automatically gets the `.json` extension. On confirmation the name without extension is written to `Scene.InstanceName`, and then [SceneSerializer](../03_Modules/SceneSerializer.md) saves the current scene. There is no overwrite confirmation and no unsaved-changes check.

The file is created relative to the current working directory. Use simple unique names without paths and always start the editor from the project folder.

### Open

The list contains every `*.json` in the working directory except service files: `bindings.json`, `level_backup.json`, `project_config.json`, `lighting_presets.json`, `editor_layout.json`. Double-clicking opens a file immediately.

The loader reads `SceneType` and `InstanceName`. If `OpenedScenes` already has a scene with a matching name and type, that instance is reused: its old entities are cleared and replaced with the file contents. Otherwise the type is looked up as `ef2dg.<SceneType>`; if that fails, a `GameScene` is created.

## Scene View and scene tabs

The first active scene is added to `OpenedScenes` automatically. Tab buttons switch the context through `Engine.ChangeScene`. A tab's context menu lets you change `InstanceName`; the cross closes a tab without saving and without confirmation. The last tab cannot be closed.

The `+` button lists every non-abstract class derived from `Scene` in the current assembly. For a new `GameScene` the editor immediately sets `IsLoadedFromFile = true` so that the default player is not created. Because `GameScene.Load` returns early, such a scene may end up without a `Tilemap`; in practice it is safer to create a new level from a valid JSON template.

### Navigation and selection

- The mouse wheel over the viewport changes `Zoom` by 10% of the current value; range `0.1–20`.
- Double-clicking an entity in `Hierarchy` moves the camera to it.
- In `Select` mode a left click selects an entity, iterating the list from the end: a visually later object wins.
- For a regular entity the selection area is built around `Position` using `Scale`.
- For `ButtonComponent`, coordinates and `Size` are screen pixels from the top-left corner of the viewport.
- Dragging changes the position directly. A `PhysicsBody` has its velocity reset.

### Map tools

`Paint` writes the selected texture path into a cell of the active layer, `Erase` writes an empty string. The cell coordinate is computed from world coordinates and `TileSizeInWorld`. The brush highlight is shown only in Edit Mode.

## Hierarchy

`Create Empty GameObject` creates an object at `(0, 0)`, assigns `ParentScene`, the name `Entity N`, and appends it to the end of `Entities`.

The icons to the right of a name (drawn by `EditorIcons`): up and down arrows reorder the list, the bin deletes the entity. Order affects drawing and mouse selection: entities are drawn in list order and picked in reverse order. Deletion is immediate; there is no cleanup lifecycle method and no Undo. The icons are dimmed until the row is hovered or selected.

## Inspector

The top part edits `Name`, `IsUI`, `AlwaysActive`, `Position`, `Scale` and `Rotation`. All components of the object are listed below.

The generic reflection panel supports public properties with a setter of these types:

- `float` — `DragFloat` with a step of `0.1`;
- `bool` — checkbox;
- `int` — `DragInt`;
- `Vector2D<float>` — two floats;
- `Vector4D<float>` — colour picker;
- `string` — a string of up to 256 characters.

Showing a property in the Inspector does not guarantee it is saved: JSON supports a narrower set, see [Level Format and Serialization](../03_Modules/Level%20Format%20and%20Serialization.md). Computed properties without a setter are not editable. `AnimationsJson`, `ParametersJson`, `SoundClipsJson`, `DefaultSoundPath`, `FrameWidth` and `AnimationSpeed` are hidden from the generic panel because they have dedicated editors.

### Adding and removing components

`Add Component` lists every non-abstract `Component` subclass in the current assembly. A type the object already has is not shown. Instances are created with `Activator.CreateInstance`, so a public parameterless constructor is required.

When a `PhysicsBody` is added, the editor also assigns the current scene's map and an initial `HitboxSize = (0.5, 0.5)`. Other components get no extra setup. `Start()` is called immediately. Removal happens without confirmation and without a `Dispose`/`OnRemoved` method.

### SpriteComponent

The panel shows the detected animator parameters, states, texture, `FrameWidth`, speed, looping, sound events and transitions. State names and target state names are case-sensitive. `FrameWidth` must divide the sprite sheet width as expected; the frame count is the integer `Texture.Width / FrameWidth`.

Transition rules are checked in the order of the current state's list, and the first match wins. The operators `>`, `<`, `>=`, `<=`, `==`, `!=` are supported; the special `ParameterName = ExitTime` fires after a non-looping animation finishes. A trigger is reset after an accepted transition.

A sound event links a frame number to a key in `AudioSourceComponent.SoundClips`, not directly to a file.

### AudioSourceComponent

You can pick the main track, test it with the Play/Stop buttons and build a dictionary of SFX keys. The UI lists WAV, OGG and MP3, but the current `AudioEngine` decodes only PCM WAV. Choose `.wav` to be safe.

`PlayOnStart` means the component's first `Update` in Play Mode. The flag is not reset automatically on Stop; restarting the same scene may not replay the sound until code explicitly calls `ResetPlayState`.

### Transition settings

If a component has a public `TransitionSettings` property, the Inspector shows a nested group with `Effect`, `Duration` and `Color`. `ButtonComponent` and `TeleportTriggerComponent` currently have it. The settings are saved to JSON; details are in [Scene Transitions](../05_Guides/Scene%20Transitions.md).

## Tile Palette

The `Select`, `Paint` and `Erase` modes are shared with the `Scene View`. The active layer is chosen from `Map.Layers`; `Layer Physics Enabled` lets the map use the layer for collisions.

The palette shows only PNG files directly inside `assets/Tiles`, without recursing into subfolders. In the `Asset Browser` you can double-click a PNG from any subfolder to make it the brush.

Settings are bound to the texture path and shared by every cell with that path in the current map:

- `Texture Scale` and `Texture Offset` change the size and position of the image;
- `Frame Count` and `Anim Speed` define a horizontal sprite-sheet animation;
- `Flip X/Y` mirror the UVs;
- `Tile Collision` chooses `None`, `Solid`, `SlopeLeft`, `SlopeRight`;
- `Hitbox Scale/Offset` are available only for `Solid`; slopes always match the grid.

## Asset Browser and import

The window shows the contents of `assets` recursively. A single click selects a file and makes its parent folder the current import target. Double-clicking a PNG turns it into the tile brush. PNG files get a preview, WAV/MP3 get Play/Stop buttons, JSON files are marked as scene files.

Dropping external files is handled by `Engine.FileDrop`: each file is copied into `CurrentAssetFolder` with `overwrite = true`. There is no confirmation dialog, the source folder structure is not preserved and no metadata is imported.

## Project Settings

The window changes `DefaultCameraZoom` and `BackgroundColor` and saves them to `project_config.json`. Lighting is configured per scene in the **Lighting** window (see [Lighting](../05_Guides/Lighting.md)). `InputManager.Bindings` is edited here too: you can wait for the next pressed key and add new actions.

New actions appear in the JSON but do nothing by themselves — a component has to query them through `InputManager.IsActionPressed`.

## Console

`ConsoleInterceptor` intercepts `Console.Write`/`WriteLine` and keeps the lines for the `Console` window. `Clear` empties the visible history, `Auto-scroll` follows new messages. Colour is chosen by the substrings `ERROR`, `Failed`, `WARNING`, `EVENT` and their Russian equivalents; this is a visual heuristic, not structured log levels.

## Recommended safe cycle

1. Load or create a valid scene.
2. Make one logical change.
3. Save the JSON.
4. Start Play Mode and check the Console and F1–F5.
5. Press Stop and reload the JSON if the runtime changed the scene.
6. Save the result again before closing the tab.
