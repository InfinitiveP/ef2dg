# NDC Coordinates and Mouse Picking

🌐 **English** · [Русский](../ef2dg_Docs/Математика%20NDC%20координат%20и%20Mouse%20Picking.md)

Rendering uses the orthographic [Camera](01_Core_Engine/Camera.md). The world width of the visible area equals `ViewWidth` and the height equals `ViewHeight`; both depend on the window size and `Zoom`.

In `SceneViewWindow` the mouse position is first converted from ImGui screen coordinates into framebuffer image coordinates. The relative position is then scaled into the camera's world area: the image centre corresponds to `Camera.Position`, and the edge to half of `ViewWidth` or `ViewHeight`.

UI elements use the entity's screen position inside the ImGui window. World objects use the computed world coordinates; a tile is picked by dividing by `TileSizeInWorld` and rounding to the grid index.
