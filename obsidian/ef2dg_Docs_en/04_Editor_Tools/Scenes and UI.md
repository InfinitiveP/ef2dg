# Scenes and UI

🌐 **English** · [Русский](../../ef2dg_Docs/04_Editor_Tools/Сцены%20и%20интерфейс.md)

The editor keeps both a list of opened scene instances and the engine's stack of active scenes. They are related but different structures.

## OpenedScenes

`EditorState.OpenedScenes` backs the tabs. Closing a tab removes only the reference from this list; nothing is saved automatically. Reloading a level tries to reuse a tab by `InstanceName`, file name or type name.

## Engine stack

- `ChangeScene` clears the stack and makes the scene the only active one.
- `PushScene` adds a scene on top of the current one; rendering goes from bottom to top.
- `PopScene` removes the top scene, but only if the stack holds more than one.

The operations run at the end of `OnUpdate` so that a scene is never swapped in the middle of its own update. When switching from Play Mode, the engine temporarily leaves play mode, pauses audio and then resumes the game.

## ButtonComponent

In the `Scene View` a button can point to:

- a JSON file name, with or without the extension;
- the name of an already opened scene;
- the name of a class derived from `Scene` in the `ef2dg` namespace.

With `IsOverlay = false` it calls `ChangeScene`, with `true` it calls `PushScene`. If a JSON file is found, the editor reads `SceneType`, creates or reuses the scene and loads the data. Outside Play Mode buttons are visible but do not navigate.

## UI entities

`GameObject.IsUI` makes `Scene.DrawUI` draw the object after the world and the lighting, with the camera at `(0, 0, 3)` and `Zoom = 1`. The flag is part of `EntityData` and is saved in the level JSON.

`ButtonComponent` is positioned in viewport pixels in `SceneViewWindow` and drawn through ImGui separately from the regular `Component.Draw`. This is a different coordinate system from world sprites.

`UIScene.DrawMenuUI` can also create ImGui buttons from entities, but the main `Engine.OnRender` does not call this method directly. Actual button handling lives in `SceneViewWindow`.

## Known risks

- Switching a tab in Play Mode automatically returns to Play Mode after the change.
- `PushScene` always calls `Load` on the pushed scene, unlike the cached `ChangeScene`.
- Reusing a tab clears its entities before the load is confirmed to succeed; a corrupted file can leave the scene empty.
- With identical `InstanceName`s the editor may pick the wrong opened instance.
