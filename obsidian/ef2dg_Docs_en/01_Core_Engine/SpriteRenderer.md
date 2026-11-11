# SpriteRenderer

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/SpriteRenderer.md)

`SpriteRenderer` keeps the familiar `Draw`, `DrawColored` and `DrawCollisionBox` calls, but regular sprites are no longer sent to OpenGL one by one. The CPU computes the final world coordinates, UVs, RGBA and texture slot of all four vertices and accumulates them in a dynamic batch.

## Main sprite batch

- one batch holds up to `2048` sprites;
- one sprite takes four vertices of ten `float`s each: `XYZ`, `UV`, `RGBA`, texture slot;
- a static EBO is pre-filled with indices for all `2048` quads;
- the VBO is created once with `DynamicDraw`, and `Flush()` uploads only the used part with `BufferSubData`;
- one draw call can use up to `16` different `Texture.Handle`s without a texture atlas;
- quad order in the index buffer matches call order, so alpha blending and layer order are preserved.

`Draw` passes white and delegates to `DrawColored`. `DrawColored` keeps its signature and appends a quad to the current batch. Position, size, `rotationDeg`, `flipX`, `texOffset`, `texScale` and colour are computed on the CPU.

## Auto-flush

The accumulated geometry is sent to the GPU when any of these happens:

- the `2048` sprite limit is reached;
- all `16` texture slots are used and a new texture is needed;
- the view/projection matrices change, for example when switching from the game camera to `IsUI`;
- `DrawCollisionBox` or the solid UI batch starts;
- `Engine` changes the framebuffer or blend state;
- the render frame ends.

`RenderSceneToFramebuffer` explicitly finishes the batch before lighting is applied. `DrawFramebuffer` and `DrawColorOverlay` flush immediately because they are separate full-screen passes.

## Solid-colour UI batch

The existing `BeginSolidBatch`/`DrawSolid`/`EndSolidBatch` path is kept. It uses its own `ui_batch.vert` and `ui_batch.frag` and merges frames, fills and pixel text without texture sampling. When solid geometry and textured icons alternate, the renderer flushes only the previous run, preserving the exact transparency order.

## Debug geometry

`DrawCollisionBox` first calls `FlushAll`, then uses a separate static VAO/VBO/EBO and `debug.vert`/`debug.frag`. After `PolygonMode.Line`, `PolygonMode.Fill` is always restored.

## Diagnostics

`BeginFrame` and `EndFrame` collect `LastFrameDrawCalls` and `LastFrameQuads`. With the `F5` monitor enabled, `Draws` and `Quads` are shown in the window title next to FPS and frame time, so batching efficiency can be compared on the same scene.

Keep in mind that batching reduces CPU/driver overhead from draw calls, but it does not remove expensive game logic, resource loading, heavy overdraw or overly frequent mandatory flushes between render passes.
