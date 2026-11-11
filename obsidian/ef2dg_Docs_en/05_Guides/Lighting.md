# Lighting

🌐 **English** · [Русский](../../ef2dg_Docs/05_Guides/Освещение.md)

Every scene (location) has its own `SceneLighting` settings. They are stored in the level JSON in the `Lighting` field and saved with `File → Save` (`Ctrl+S`). Light sources are objects with a [LightComponent](../03_Modules/Component%20Catalog.md#lightcomponent).

> [!NOTE]
> The Lighting window and its presets currently have Russian labels. The original label is given first, with its English meaning in parentheses.

## Quick location setup

1. Open the **Lighting** window.
2. Pick a location preset in the drop-down: «Бездна (фиолетовая)» (Abyss, purple), «Подземелье с факелами» (torch-lit dungeon), «Пещера» (cave), «Лунная ночь» (moonlit night), «Закат» (sunset), «Ядовитое болото» (toxic swamp), «Преисподняя» (underworld), «День» (day). Lighting is enabled automatically.
3. Press **+ Добавить источник света** (add light source) and choose a type: torch, candle, magic orb, crystal, toxic glow, lava, moonlight, player aura. The object appears in the centre of the camera and can be dragged with the mouse.
4. Fine-tune the source in the **Inspector**, and the overall look with the sliders in the Lighting window.
5. Save the level (`Ctrl+S`). To reuse the same look on other levels of the location, enter a name and press **Сохранить пресет** (save preset): it goes into `lighting_presets.json` and appears in the preset list. To delete a preset, right-click the list.

The **В редакторе** (in editor) checkbox hides lighting outside Play Mode when darkness makes object placement inconvenient. In Play Mode lighting is always on if the scene has it enabled.

## Scene settings

- **Темнота** (darkness) — `AmbientColor` and `AmbientIntensity`: the scene colour where no light source reaches. White means no darkening.
- **Яркость всех** (overall brightness) — `LightMultiplier`, a global multiplier for the scene's sources.
- **Свечение (Bloom)** — `BloomStrength`, `BloomThreshold`, `BloomRadius`: a soft halo around bright pixels.
- **Цветокоррекция** (colour grading) — `Exposure`, `Contrast`, `Saturation`, `Tint`.
- **Виньетка** (vignette) — `VignetteStrength`, `VignetteSize`, `VignetteColor`.

## Light source settings

- `Color`, `Intensity` — colour and brightness. Brightness above 1 overexposes the surface and triggers bloom.
- `Radius` — range in world units (one tile = 0.5); `Offset` — shift from the object's position.
- `Falloff` — fade towards the edge: below 1 gives an even spot, above 1 concentrates light in the centre.
- `Glow`, `GlowSize` — a visible glowing core (orb, flame) drawn on top of the lit scene.
- `FlickerAmount`, `FlickerSpeed` — fire flicker; `PulseAmount`, `PulseSpeed` — smooth pulsing.
- `Enabled` — quickly turn the source off.

In the editor a source is marked with a diamond of its colour and a circle showing its radius; the circle is brighter for the selected object.

## How it is drawn

`Engine` draws every scene of the stack in phases: `Scene.DrawWorld` → lighting → `Scene.DrawEditorOverlay` → `Scene.DrawUI`. That is why the HUD (`IsUI`) and debug gizmos are never darkened.

`LightingRenderer` performs:

1. A light map in an HDR buffer (RGBA16F): filled with the darkness colour, then additive circles for every source (`Shaders/light.*`).
2. Composition: world pixels are multiplied by the light map (`light_composite.frag`).
3. An additive `Glow` core for the sources.
4. Bloom: bright pixels are extracted into a quarter-size buffer and blurred with two Gaussian passes (`bloom_extract.frag`, `bloom_blur.frag`).
5. Colour grading and vignette, written back into the scene framebuffer (`post_final.frag`).

## Limitations

- No shadows are computed: light passes through walls.
- Sprites cannot glow by themselves (no emissive); a glowing object is made a light source with `Glow`.
- Levels without a `Lighting` field get the old global `EnableLighting` and `AmbientLightColor` from `project_config.json` (lighting is disabled for `UIScene`). After saving, the level gets its own settings.
- Lighting is applied to the shared scene framebuffer after each scene of the stack, so an overlay scene with lighting enabled also darkens the HUD of the scene below it.
