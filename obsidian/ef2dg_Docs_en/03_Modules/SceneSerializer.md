# SceneSerializer

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/SceneSerializer.md)

`SceneSerializer.Save(scene, filePath)` builds [LevelData](LevelData.md) and writes formatted JSON. `Load(scene, filePath, resources)` reads JSON and returns `false` if the file is missing, empty or cannot be parsed.

On load, entities and map data are cleared, layers and tile configuration are restored, and then components are created. `PhysicsBody` gets the current map; `SpriteComponent` starts `Idle` or its first animation; the `GameScene` player is detected by `PlayerController`.

The serializer does not save OpenGL resources, delegates, private state, editor UI state or unsupported property types.

## Supported component properties

Only public properties with a public setter of type `float`, `bool`, `int`, `string`, `Vector2D<float>` and `Vector4D<float>` are saved automatically (vectors go into `VectorValues`, `Vector4D` as an array of four numbers). Scene lighting settings are saved in the level's `Lighting` field; older levels without it take them from `project_config.json` (see [Lighting](../05_Guides/Lighting.md)).

Animation and audio dictionaries are saved through a workaround — the string properties `AnimationsJson`, `ParametersJson` and `SoundClipsJson`. A new complex type needs the same kind of serializable adapter or an extension of `ComponentData`.

## Load order

1. The component is created with `Activator.CreateInstance`.
2. `AddComponentDynamic` assigns `Owner` and calls `Start()`.
3. Only after that does the serializer assign properties from JSON.

So `Start()` must not compute final state from saved settings. Initialization that depends on loaded values should be done lazily in the first `Update`, in a separate post-load method, or after a future lifecycle extension.

More: [Level Format and Serialization](Level%20Format%20and%20Serialization.md).
