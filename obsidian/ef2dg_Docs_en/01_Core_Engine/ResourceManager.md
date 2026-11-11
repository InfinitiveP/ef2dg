# ResourceManager

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/ResourceManager.md)

The manager owns OpenGL textures and caches them by string path. `GetTexture(path)` returns an already loaded texture or creates a new one. If the file is missing, a magenta-black `MissingTexture` placeholder is returned and cached.

A one-pixel `WhiteTexture` is also available as a fallback resource for lighting. All loaded and internal textures are released in `Dispose()` when the engine shuts down.

Paths are relative to the process working directory, so run the project from its own folder.
