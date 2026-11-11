# Scene Transitions

🌐 **English** · [Русский](../../ef2dg_Docs/05_Guides/Переходы%20между%20локациями.md)

A single transition system handles scene changes, stacked overlays and arbitrary actions. While the visual effect runs, scene updates, physics, the editor camera and game audio are paused. The action itself runs in the middle of the effect.

## TransitionSettings

`TransitionSettings` contains:

- `Kind` — `Instant`, `Fade`, `CrossFade`, `SlideLeft`, `SlideRight`, `SlideUp`, `SlideDown`;
- `Duration` — total effect duration in seconds, `0.5` by default;
- `Color` — the colour of the intermediate background and fade, black by default.

`Instant` keeps the engine's original behaviour. The settings are serialized into `ComponentData.TransitionValues`, including the RGBA colour.

## Phases

1. The engine finishes the current frame and copies it into a separate framebuffer.
2. The first half of the effect hides the old frame.
3. At the midpoint the action runs: teleport, loading, `ChangeScene`, `PushScene` or `PopScene`.
4. The second half reveals the new frame.

A repeated request during an active transition returns `false` and is ignored. If loading a scene fails, the engine logs `[Transition ERROR]` and shows the original scene again.

## Setting up ButtonComponent

Expand the component's `Transition` group in the `Inspector` and choose `Effect`, `Duration` and `Color`. `TargetSceneName` can be a JSON file name with or without the extension, the name of an opened scene or the name of a class derived from `Scene`. `IsOverlay` chooses between replacing the stack and `PushScene`.

Loading the JSON is deferred to the middle of the effect, so the read and deserialization pause is hidden by the transition.

## Setting up TeleportTriggerComponent

- Empty `TargetSceneName` — move the player to `TargetLocation` inside the current scene.
- Non-empty `TargetSceneName` — load a JSON file or a scene by name.
- `Transition.Kind = Instant` — the action without a visual effect.

The zone needs `BoxCollider.IsTrigger = true`; the entering player needs a `PhysicsBody` and a `PlayerController`.

## Using it from code

Switching to a ready scene:

```csharp
Engine.Instance.ChangeScene(targetScene, new TransitionSettings
{
    Kind = TransitionKind.Fade,
    Duration = 0.6f,
    Color = new Vector4D<float>(0f, 0f, 0f, 1f)
});
```

Loading by name or JSON:

```csharp
Engine.Instance.RequestSceneTransition(
    "level_2.json",
    isOverlay: false,
    settings: Transition);
```

An arbitrary action under the effect:

```csharp
Engine.Instance.RunTransition(
    () => Owner.Position = TargetLocation,
    Transition);
```

The `PushScene(scene, settings)` and `PopScene(settings)` overloads are also available. The old methods without settings stay instant.

## Visual details

- `Fade` covers the old frame with the colour and then reveals the new one.
- `CrossFade` first dims the old frame towards the colour, then blends the saved frame with the new one.
- `Slide*` pushes the old frame out in the given direction and brings the new one in from the opposite edge.
- The effect is composed after game rendering and lighting, but before the framebuffer is shown in the `Scene View`.
