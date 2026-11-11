# Scene

🌐 **English** · [Русский](../../ef2dg_Docs/02_Architecture/Scene.md)

`Scene` is the abstract level container. It holds `Entities`, `Map`, `InstanceName`, `FilePath`, `ActiveCamera`, `SimulationPadding` and the `Lighting` settings ([Lighting](../05_Guides/Lighting.md)). A derived class must implement `Load(ResourceManager, IInputContext)`.

`FixedUpdate` and `Update` call the matching entity methods only inside the camera area extended by `SimulationPadding`, unless the entity has `AlwaysActive` enabled.

Drawing is split into phases that `Engine` calls separately:

1. `DrawWorld` — the `Background` and `Ground` layers, visible non-UI entities, then `Foreground`;
2. scene lighting (`LightingRenderer`), if enabled;
3. `DrawEditorOverlay` — debug gizmos and hitboxes on top of the lighting;
4. `DrawUI` — entities with `IsUI = true`, drawn with the screen camera; the HUD is never darkened.

`Draw` is kept and simply calls the three drawing phases in a row, without lighting.

`GameScene` creates a starter player for a new scene. `UIScene` creates a service tilemap and works with `ButtonComponent` elements.
