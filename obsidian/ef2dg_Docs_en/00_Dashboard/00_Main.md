# ⚙️ ef2dg Technical Wiki

🌐 **English** · [Русский](../../ef2dg_Docs/00_Dashboard/00_Main.md)

`ef2dg` is a custom 2D engine and visual editor written in C# on top of Silk.NET and OpenGL 3.3. This wiki describes the actual state of the source code; planned features are kept separate from implemented ones.

## Start here

- [Getting Started](../05_Guides/Getting%20Started.md) — requirements, running the engine and project layout.
- [Complete Editor Guide](../04_Editor_Tools/Complete%20Editor%20Guide.md) — every window, mode, tool and limitation.
- [Component Development](../05_Guides/Component%20Development.md) — writing a component: lifecycle, serialization and the Inspector.
- [Practical Guide](../05_Guides/Practical%20Guide.md) — building and saving a level from scratch to Play.
- [Limitations and Diagnostics](../05_Guides/Limitations%20and%20Diagnostics.md) — build checks and known limitations.

## Architecture

- [Engine](../01_Core_Engine/Engine.md) — window, main loop, framebuffers, lighting and the scene stack.
- [Scene](../02_Architecture/Scene.md) — a level and the order in which it is updated and drawn.
- [GameObject](../02_Architecture/GameObject.md) and [Component](../02_Architecture/Component.md) — composing entity behaviour.
- [SceneSerializer](../03_Modules/SceneSerializer.md), [LevelData](../03_Modules/LevelData.md), [EntityData](../03_Modules/EntityData.md), [ComponentData](../03_Modules/ComponentData.md) — the JSON level format.

## Subsystems

- [Camera](../01_Core_Engine/Camera.md), [SpriteRenderer](../01_Core_Engine/SpriteRenderer.md), [Shader](../01_Core_Engine/Shader.md), [Texture](../01_Core_Engine/Texture.md), [ResourceManager](../01_Core_Engine/ResourceManager.md).
- [Tilemap](../03_Modules/Tilemap.md), [PhysicsBody](../03_Modules/PhysicsBody.md), [AABB Physics](../AABB%20Physics.md).
- [SpriteComponent](../03_Modules/SpriteComponent.md), [InputManager](../03_Modules/InputManager.md), [Audio](../03_Modules/Audio.md), [Editor](../04_Editor_Tools/EditorUI.md).
- [Component Catalog](../03_Modules/Component%20Catalog.md) — purpose and dependencies of the built-in components.
- [Level Format and Serialization](../03_Modules/Level%20Format%20and%20Serialization.md) — what is saved to JSON and what is lost.

## Transitions

- [Scene Transitions](../05_Guides/Scene%20Transitions.md) — `Instant`, `Fade`, `CrossFade` and `Slide` effects, component setup and API.

## Lighting

- [Lighting](../05_Guides/Lighting.md) — per-scene lighting, light sources, bloom, vignette, presets and the Lighting window.

## Games built on the engine

Games made with `ef2dg` are documented in their own repositories and are not part of the engine. This technical wiki remains the source of truth for what `ef2dg` actually implements: a system described in a game's design documents does not exist in the engine until it appears here.
