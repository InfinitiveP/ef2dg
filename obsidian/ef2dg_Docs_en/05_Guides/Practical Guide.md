# Practical Guide

🌐 **English** · [Русский](../../ef2dg_Docs/05_Guides/Практическое%20руководство.md)

## Creating a level

1. Start the editor and create or open a JSON level with `File → Open...` (`Ctrl+O`).
2. Add an entity in `Hierarchy` and the components you need in `Inspector`.
3. For the map, pick a PNG in the `Asset Browser`, double-click it and paint the layer in the `Scene View`.
4. In the `Tilemap Editor`, set up tile scale, offset, collision and animation.
5. For a character, add `SpriteComponent`, `PhysicsBody`, `PlayerController` and, if needed, `CameraFollowComponent`.
6. Save with `File → Save As...` (`Ctrl+Shift+S`); the saved JSON must live in the project's working directory.

> [!WARNING]
> **Save before Play Mode**
> The game runs on the live scene instance. Deleted enemies, moved objects and other runtime changes remain after Stop. There is no automatic scene copy and no rollback.

## Testing

Switch to Play. `W/A/S/D` map to the movement actions, `Space` to jump and `F` to attack; the actual bindings are stored in `bindings.json`. The `F1`–`F5` debug flags help check physics and performance.

## A new component

Inherit from `Component`, add a public parameterless constructor, override the lifecycle methods you need and expose saved settings as public properties of supported types. Thanks to reflection the component appears in the `Add Component` menu automatically. Full rules and examples: [Component Development](Component%20Development.md).
