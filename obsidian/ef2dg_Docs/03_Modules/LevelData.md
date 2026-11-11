# LevelData

🌐 **Русский** · [English](../../ef2dg_Docs_en/03_Modules/LevelData.md)

`LevelData` — корневой DTO JSON-уровня. В нём хранятся `SceneType`, `InstanceName`, списки `Entities` и `TileLayers`, словарь `TileConfigs`, настройки освещения сцены `Lighting`, а также устаревшее совместимое поле `TextureScales`.

`SceneType` позволяет при загрузке создать `GameScene` или `UIScene` по имени типа. `TileConfigs` содержит параметры тайлов: UV, отражение, хитбокс, коллизию и анимацию.
