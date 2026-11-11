# Component Catalog

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/Каталог%20компонентов.md)

The components found in the current source code, their dependencies and setup details.

## SpriteComponent

Draws a sprite sheet and drives the animation state machine. Requires valid texture paths. Frame sounds need an `AudioSourceComponent` on the same object. Dictionaries are saved through `AnimationsJson` and `ParametersJson`.

## PhysicsBody

Handles velocity, gravity, tile collisions, slopes and triggers. Needs `Owner.ParentScene` and `Map`. Use `GravityScale > 0` for a platformer and `0` for top-down. Solid `BoxCollider`s do not stop the body.

## BoxCollider

Stores an AABB area. With `IsTrigger = true` it is detected by other `PhysicsBody` components and raises callbacks. With `false` it currently serves mostly for visualization and future physics extensions.

## CameraFollowComponent

Remembers the camera passed to `Draw` once in a static field, then smoothly moves it toward `Owner.Position + Offset` in `FixedUpdate`. The component does not move the camera before its first `Draw`. The static reference is shared by all instances.

## LightComponent

A light source: `Color`, `Intensity`, `Radius`, `Offset`, `Falloff`, the `Glow`/`GlowSize` core, `FlickerAmount`/`FlickerSpeed` flicker, `PulseAmount`/`PulseSpeed` pulsing and the `Enabled` flag. Its own `Draw` is empty; sources are collected by `LightingRenderer`. The light is visible only if lighting is enabled for the scene in the Lighting window. The Inspector shows a dedicated block with presets for this component. More in [Lighting](../05_Guides/Lighting.md).

## AudioSourceComponent

Starts music or positional SFX and stores a dictionary of clip keys. `PlayOnStart` fires once per component instance; `ResetPlayState` is needed to play it again. PCM WAV is guaranteed to work.

## ParticleEmitterComponent

Creates a particle pool with velocity, spawn area, lifetime, size, colour, gravity and rotation parameters. The `StartColor`/`EndColor` colours are `Vector4D` and are saved in `VectorValues`. The source contains an unused `_texture` field; the actual texture is resolved from `TexturePath` through resources.

## ButtonComponent

Describes a button: `Label`, on-screen `Size`, `TargetSceneName`, `IsOverlay`, `IsHovered`. `SceneViewWindow` draws it as an ImGui button or as a `SpriteComponent` texture. Navigation to a JSON file or scene class happens only on click in Play Mode. `IsHovered` is updated by the editor and saved like any public property, although it is transient UI state.

## PlayerController

Requires `SpriteComponent` and `PhysicsBody`. Uses the `Left`, `Right`, `Up`, `Down`, `Jump`, `Attack` actions. Works as a platformer with `GravityScale > 0` and as top-down with `0`. The attack removes any other entity with a `PhysicsBody` that falls into the strike AABB; there is no health or team system yet.

## EnemyAIComponent

Requires `SpriteComponent` and `PhysicsBody` and looks for the first entity with a `PlayerController`. Moves only along X, chases within `SightRadius` and starts the attack animation within `AttackRadius`. Actual damage to the player is not implemented.

## SmartStoneComponent

Finds the player and moves away from them along X inside `TriggerRadius`. Works without required components, but movement needs a `PhysicsBody` and animation needs a `SpriteComponent`.

## TeleportTriggerComponent

On `OnTriggerEnter` it checks whether the entering object has a `PlayerController`. If `TargetSceneName` is empty, it moves the player to `TargetLocation`; otherwise it loads the given JSON scene or scene class. Both modes use `Transition`. It needs a zone object with `BoxCollider.IsTrigger = true` and a player with a `PhysicsBody`. More in [Scene Transitions](../05_Guides/Scene%20Transitions.md).
