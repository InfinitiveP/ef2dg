# InputManager

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/InputManager.md)

`InputManager` maps logical actions to `Silk.NET.Input.Key` values. The `bindings.json` file stores the dictionary; on load the missing default actions are added automatically: `Left`, `Right`, `Up`, `Down`, `Jump`, `Attack`.

Game code calls `IsActionPressed("Attack")` instead of checking a specific key. `IsKeyPressed` is meant for technical checks, `GetAnyPressedKey` for rebinding controls in the editor.

For runtime UI, `SceneViewWindow` stores the on-screen bounds of the framebuffer image. At the start of the next frame — after ImGui input is updated and before `Scene.Update` — the current pointer is converted into the screen UI camera's coordinates by `UpdateRuntimePointer` and passed to `SetUiPointerState`. `UiPointerPosition`, `IsUiPointerInside`, `IsUiPointerLeftDown`, `IsUiPointerRightDown` and `UiPointerWheel` are available. These are not raw window coordinates: they already account for the position of the docked `Scene View` and the viewport size. `WorldPointerPosition` gives the cursor position in world coordinates.

The order "ImGui input → pointer transform → game `Update`" matters: if the pointer were updated only during `Scene View.Draw`, the UI would get the state one frame late and could miss a short click between frames.

UI coordinates are computed for a fixed view height of `2` (`Zoom = 1`) and `Camera.AspectRatio`. Do not use `Camera.ViewWidth/ViewHeight` directly: they include the game camera's zoom, whereas `Scene.DrawUI` temporarily sets the interface zoom to `1` before drawing `IsUI` entities.
