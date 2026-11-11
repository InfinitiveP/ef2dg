# Limitations and Diagnostics

🌐 **English** · [Русский](../../ef2dg_Docs/05_Guides/Ограничения%20и%20диагностика.md)

`dotnet build` succeeds with .NET SDK 10.0.101: there are no errors. The compiler emits warning `CS0169`: the `_texture` field in `ParticleEmitterComponent` is declared but never used.

A graphical launch depends on the OpenGL driver, the window system and an audio device. If something goes wrong, make sure you run from the project folder, that the shader files and the paths under `assets/` exist, and check the messages in the `Console` window.

`bin/` and `obj/` are build outputs. Do not treat them as sources, edit them or commit them.

## Known functional limitations

- Play Mode does not create a copy of the scene and does not roll back changes after stopping.
- Undo/Redo are implemented only for tile editing.
- `Vector4D<float>` is saved by the serializer as an array of four numbers in `VectorValues`. The colour inside `TransitionSettings` is still stored in its own `TransitionValues`.
- `File → Save` (`Ctrl+S`) saves the live scene in Play Mode too: spawned enemies, damage and picked-up items end up in the JSON. Save a level only in Edit Mode after reloading the file.
- The audio file pickers show WAV, OGG and MP3, but `AudioEngine` decodes only PCM WAV with a RIFF/WAVE header.
- A `BoxCollider` with `IsTrigger = false` is drawn but does not take part in `PhysicsBody` collision resolution; solid physics goes through the [Tilemap](../03_Modules/Tilemap.md).
- A new empty `GameScene` created with the `+` button may get no map, because the editor sets `IsLoadedFromFile = true` and `GameScene.Load` returns before creating the `Tilemap`. Load a prepared JSON level for reliable results.
- The `File → Open...` list shows every JSON file in the working directory except service files (`bindings.json`, `level_backup.json`, `project_config.json`, `lighting_presets.json`, `editor_layout.json`).
