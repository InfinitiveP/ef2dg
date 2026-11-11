# LevelData

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/LevelData.md)

`LevelData` is the root DTO of a JSON level. It stores `SceneType`, `InstanceName`, the `Entities` and `TileLayers` lists, the `TileConfigs` dictionary, the scene `Lighting` settings and the obsolete compatibility field `TextureScales`.

`SceneType` lets the loader create a `GameScene` or `UIScene` by type name. `TileConfigs` holds tile parameters: UVs, flipping, hitbox, collision and animation.
