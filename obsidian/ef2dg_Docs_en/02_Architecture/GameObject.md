# GameObject

🌐 **English** · [Русский](../../ef2dg_Docs/02_Architecture/GameObject.md)

`GameObject` is a scene entity with `Name`, `Position`, `Scale`, `Rotation`, `ParentScene`, `AlwaysActive` and `IsUI`. Its behaviour is stored in a dictionary of components keyed by type.

`AddComponent<T>` adds a component and starts it; `GetComponent<T>` returns the component of exactly that type; `RemoveComponent` removes it. `GetAllComponents` is used by [SceneSerializer](../03_Modules/SceneSerializer.md) and the Inspector.

`AlwaysActive` disables update culling outside the camera. `IsUI` separates the screen interface from the game world: in a [Scene](Scene.md), UI is drawn after the world with a temporarily fixed camera.
