# Engine

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/Engine.md)

`Engine` is the runtime entry point. `Program.Main` creates an instance and calls `Run()`.

## Initialization

`Run()` creates a `1280×720` window with OpenGL 3.3 and subscribes to the `Load`, `Update`, `Render`, framebuffer-resize and close events. `OnLoad` creates the `Camera`, `SpriteRenderer`, `ResourceManager`, `AudioEngine`, the ImGui controller, the game `GameScene`, and the final, scene, lighting and transition-snapshot framebuffers.

## Main loop

`OnUpdate` handles Play/Edit mode, the editor camera and FPS. The engine does not intercept `Escape`: in Play Mode the key is free for the game, and the editor is closed with `File → Exit`. In Play Mode the active scene receives `FixedUpdate` with a step of `1/60` s, then `Update`; the accumulator is capped at `0.25` s. In Edit Mode physics and game logic are not updated.

Before `ImGuiController.Update` the engine itself submits the modifier state (`ImGuiMod_Ctrl`, `Shift`, `Alt`, `Super`) to ImGui through `io.AddKeyEvent`: the Silk.NET 2.23 controller forwards only the keys themselves, while ImGui 1.90 derives `io.KeyCtrl` from these events alone. Without it the editor shortcuts `Ctrl+O`, `Ctrl+S`, `Ctrl+Shift+S`, `Ctrl+P` and `Ctrl+A` selection in text fields did not work. `File → Exit` only sets a flag; the window closes at the end of `OnRender`, after `ImGuiController.Render()`. Closing it while the menu was being drawn disposed ImGui inside `BeginMenu`/`EndMenu` and crashed the process.

`OnRender` opens a [SpriteRenderer](SpriteRenderer.md) statistics frame and draws the scene stack into a separate scene framebuffer. Each scene is drawn in phases `DrawWorld` → `LightingRenderer` lighting (if the scene has it enabled) → `DrawEditorOverlay` → `DrawUI`, so the HUD is never darkened (see [Lighting](../05_Guides/Lighting.md)). The accumulated sprite batch is always flushed before any framebuffer or blend state change. The frame is then composed into the final framebuffer together with the active [transition](../05_Guides/Scene%20Transitions.md) and shown in the [editor](../04_Editor_Tools/EditorUI.md) through ImGui. A separate snapshot framebuffer keeps the previous frame for `CrossFade` and `Slide`.

## Scenes

`ChangeScene`, `PushScene` and `PopScene` defer the operation to the end of the frame. On a change, a scene is loaded only if that particular instance has not been initialized yet. Switching away while in Play Mode pauses the game temporarily and then restores the previous mode.

The overloads of these methods that take `TransitionSettings`, as well as `RunTransition` and `RequestSceneTransition`, perform the action in the middle of the visual effect. While a transition is active, new requests are ignored.

## Debugging

`F1`–`F4` toggle drawing of hitboxes and attack zones, `F5` toggles the performance monitor. The monitor shows FPS, frame time, the number of `SpriteRenderer` draw calls, the number of submitted quads and the scene stack depth in the window title. The flags live in [DebugSettings](DebugSettings.md).
