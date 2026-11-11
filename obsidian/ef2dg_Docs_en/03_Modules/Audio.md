# Audio

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/Аудио.md)

`AudioEngine` implements `IAudioService` on top of OpenAL. At startup it creates a pool of 16 effect sources and a separate looping music source. `AudioLocator` provides the service to components and has a safe `NullAudioService` fallback.

`PlaySFX` queues a command; `ProcessFrame` limits identical sounds to two per frame, picks a free source and updates the listener position from the camera. WAV decoding is supported. `PlayMusic`, `PauseAll`, `ResumeAll` and `StopAll` control music and effects.

`AudioSourceComponent` stores the volume, default path, the `PlayOnStart`/`IsMusic` flags and the `SoundClips` dictionary. `PlayOneShot(key)` is used by [SpriteComponent](SpriteComponent.md) frame events.
