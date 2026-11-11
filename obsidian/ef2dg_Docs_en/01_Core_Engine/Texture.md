# Texture

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/Texture.md)

`Texture` is an RAII wrapper around an OpenGL texture handle. It loads a file through StbImageSharp or creates a texture from a byte array, stores the width and height and can bind itself to a `TextureUnit` slot.

Game textures should be obtained through [ResourceManager](ResourceManager.md) rather than created by hand in components. The exception is the temporary framebuffer texture wrapper that `Engine` uses for compositing.
