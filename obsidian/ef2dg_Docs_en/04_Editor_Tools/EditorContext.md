# EditorContext

🌐 **English** · [Русский](../../ef2dg_Docs/04_Editor_Tools/EditorContext.md)

`EditorContext` is a structure that `Engine` creates every frame and passes to [EditorUI](EditorUI.md). It contains the current scene, the main framebuffer, the game camera, the window size, the `ResourceManager` and the `ChangeScene`, `PushScene` and `PopScene` delegates.

The context is not kept between frames. It gives editor windows access to the runtime without a global dependency lookup.
