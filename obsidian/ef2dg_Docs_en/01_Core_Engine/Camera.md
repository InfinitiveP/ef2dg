# Camera

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/Camera.md)

`Camera` stores `Position`, `Front`, `Up`, `Zoom` and the window size. It builds the view matrix with `CreateLookAt` and an orthographic projection matrix.

- `Zoom = 1` makes the visible world two units tall; a larger `Zoom` shows a larger area.
- `ViewWidth` and `ViewHeight` are used to cull invisible entities and tiles.
- When the window is resized, `Engine.OnResize` calls `UpdateSize`.

In Edit Mode the camera is moved with `W`, `A`, `S`, `D`; the mouse wheel zooms the `Scene View`. In Play Mode the camera can be driven by `CameraFollowComponent`.
