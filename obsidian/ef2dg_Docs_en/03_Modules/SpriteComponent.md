# SpriteComponent

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/SpriteComponent.md)

`SpriteComponent` stores animations, the current frame, the `FlipX` direction, animator parameters and the action hitbox. An `AnimationState` defines the texture path, frame width, speed, looping, transition rules and per-frame sounds.

`Play` loads the texture through `Engine.Resources`, resets the frame and starts the state. In `Update` the component checks `AnimationTransition` rules, plays `SoundEvents` through `AudioSourceComponent` and advances frames. `Draw` computes the UV rectangle of the current frame and passes it to [SpriteRenderer](../01_Core_Engine/SpriteRenderer.md).

`AnimationsJson` and `ParametersJson` are needed to save the complex dictionaries through the serializer's string properties.
