# DebugSettings

🌐 **English** · [Русский](../../ef2dg_Docs/01_Core_Engine/DebugSettings.md)

A static set of diagnostic flags. `Engine` toggles them with `F1`–`F5`, and `GameScene` uses them to draw hitboxes, attack zones and the map grid.

- `F1` / `ShowPlayerHitbox` — the player's hitbox.
- `F2` / `ShowEnemyHitbox` — hitboxes of all other physics entities.
- `F3` / `ShowMapHitbox` — tilemap collisions.
- `F4` / `ShowAttackZone` — attack zones. The built-in `PlayerController`/`EnemyAIComponent` use the `SpriteComponent.ActionHitbox*` parameters. Game components can read this flag and draw their own zones with `SpriteRenderer.DrawCollisionBox`.
- `F5` / `ShowPerformanceMonitor` — FPS, frame time, `Draws`, `Quads` and the scene stack depth in the window title.
- `PreviewLightingInEditor` — show scene lighting in Edit Mode; in Play Mode lighting is always on when the scene has it enabled.
- `IsEditorMode` is set by the engine as the opposite of `IsPlayMode`.

The flags are not serialized into levels and are not meant to be player-facing game settings.
