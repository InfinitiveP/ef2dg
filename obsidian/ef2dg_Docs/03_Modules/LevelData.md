# LevelData

`LevelData` — корневой DTO JSON-уровня. В нём хранятся `SceneType`, `InstanceName`, списки `Entities` и `TileLayers`, словарь `TileConfigs`, а также устаревшее совместимое поле `TextureScales`.

`SceneType` позволяет при загрузке создать `GameScene` или `UIScene` по имени типа. `TileConfigs` содержит параметры тайлов: UV, отражение, хитбокс, коллизию и анимацию.
