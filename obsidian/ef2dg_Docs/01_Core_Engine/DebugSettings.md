# DebugSettings

Статический набор флагов диагностики. `Engine` меняет их клавишами `F1`–`F5`, а `GameScene` использует для вывода хитбоксов, зон атаки и сетки карты.

- `F1` / `ShowPlayerHitbox` — хитбокс игрока.
- `F2` / `ShowEnemyHitbox` — хитбоксы остальных физических сущностей.
- `F3` / `ShowMapHitbox` — коллизии тайлмапа.
- `F4` / `ShowAttackZone` — зоны атак. Встроенные `PlayerController`/`EnemyAIComponent` используют параметры `SpriteComponent.ActionHitbox*`. Компоненты игры могут читать этот флаг и рисовать собственные зоны через `SpriteRenderer.DrawCollisionBox`.
- `F5` / `ShowPerformanceMonitor` — FPS, время кадра, `Draws`, `Quads` и глубина стека сцен в заголовке окна.
- `PreviewLightingInEditor` — показывать освещение сцены в Edit Mode; в Play Mode оно включено всегда, если включено у сцены.
- `IsEditorMode` выставляется движком как противоположность `IsPlayMode`.

Флаги не сериализуются в уровень и не предназначены для пользовательских настроек игры.
