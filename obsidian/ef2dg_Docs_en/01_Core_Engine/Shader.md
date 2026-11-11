# Shader

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/Shader.md)

`Shader` loads GLSL sources from files, compiles the vertex and fragment shaders and links an OpenGL program. Compile or link errors are turned into an exception that carries the driver's message.

`Use`, `GetUniformLocation`, `SetUniform(Matrix4X4)` and `SetUniform(Vector2D)` are used by [SpriteRenderer](SpriteRenderer.md). The main files are `shader.vert` and `shader.frag`; debug outlines are drawn by the `debug.vert` and `debug.frag` pair.

The main vertex shader receives world coordinates, final UVs, RGBA and the texture slot already computed on the CPU; only `uView` and `uProjection` remain as uniforms. The fragment shader picks one of the batch's `16` textures and multiplies the sampled colour by the vertex colour. The choice is an explicit `switch`, so it does not depend on OpenGL 3.3 driver restrictions on dynamic indexing of `sampler2D` arrays.

`SpriteRenderer` additionally creates the `ui_batch` shader for solid-colour UI geometry and the debug shader for outlines. All instances are created once at startup and released together with the renderer.
