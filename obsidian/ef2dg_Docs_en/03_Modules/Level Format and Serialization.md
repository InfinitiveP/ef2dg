# Level Format and Serialization

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/Формат%20уровня%20и%20сериализация.md)

A level is the JSON representation of a [Scene](../02_Architecture/Scene.md) produced by [SceneSerializer](SceneSerializer.md). The file is read relative to the working directory.

## Top level

```json
{
  "SceneType": "GameScene",
  "InstanceName": "level_1",
  "Entities": [],
  "TileLayers": [],
  "TileConfigs": {},
  "Lighting": {}
}
```

- `SceneType` — the scene class name without a namespace.
- `InstanceName` — the instance name and the editor tab caption.
- `Entities` — entities and their components.
- `TileLayers` — map layers and cells.
- `TileConfigs` — settings shared by every cell with the same texture path.
- `Lighting` — the scene's lighting settings, see [Lighting](../05_Guides/Lighting.md).
- `TextureScales` — an obsolete compatibility field, used only if `TileConfigs` is empty.

## Entity

`EntityData` stores the name, transform, `AlwaysActive` and `IsUI`. That is why UI entities and system objects keep their screen-layer mode and their updates outside the simulation zone after reloading.

## Component

Each `ComponentData` stores a `TypeName` and per-type value dictionaries. The component name must resolve through `Type.GetType("ef2dg.<TypeName>")`. Renaming a class breaks loading of old levels: an unknown component is skipped without data migration.

## Support matrix

| Data | Saved | Note |
|---|---:|---|
| `float`, `bool`, `int`, `string` | yes | public property with a setter |
| `Vector2D<float>` | yes | array of two numbers |
| `Vector4D<float>` | yes | array of four numbers in `VectorValues` |
| `TransitionSettings` | yes | separate `TransitionValues` with kind, duration and RGBA |
| `Scene.Lighting` | yes | the level's `Lighting` field; if missing, the legacy `project_config.json` setting is used, see [Lighting](../05_Guides/Lighting.md) |
| enum | no | needs an int/string adapter |
| array, list, dictionary | no | needs a JSON-string adapter |
| private field | no | runtime state is lost |
| `GameObject.IsUI` | yes | the `EntityData.IsUI` field |
| `GameObject.AlwaysActive` | yes | the `EntityData.AlwaysActive` field, editable in the Inspector |
| `ParentScene`, `Owner`, `Map` | restored by code | not serialized directly |
| property marked `[SceneTransient]` | no | ignored on both save and load; meant for runtime references and caches |

## Restore order

The scene clears its old entities. The map recreates layers and configuration. Then components are created for each entity, and `Start()` is called before properties are filled in. After that `PhysicsBody.Map` is bound to the map, `SpriteComponent` starts its animation and the player is assigned to `GameScene.Player`.

Consequences:

- `Start()` sees default values;
- references between entities are not saved;
- private timers and caches are reset;
- runtime resources have to be restored lazily;
- runtime references, such as an ID generated during play, must be marked `[SceneTransient]`, otherwise saving in Play Mode will pollute the level data;
- entity order is saved as a list; component order should not be treated as a contract.

## File compatibility

Adding a new public property is safe: an old JSON file leaves the default value. Removing or renaming a property leaves the old field in JSON, but it is no longer applied. Renaming a component class or `SceneType` requires a migration or a compatible class with the old name.

## Checking a new component

1. Add the component through the Inspector.
2. Change every saved property to a distinguishable value.
3. Save the level.
4. Close or clear the current scene.
5. Load the JSON.
6. Check the properties, dependencies and first-frame behaviour.
7. Open the JSON by hand and make sure complex data really was written.
