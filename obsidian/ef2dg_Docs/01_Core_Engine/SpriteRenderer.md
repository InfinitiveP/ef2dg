# SpriteRenderer

🌐 **Русский** · [English](../../ef2dg_Docs_en/01_Core_Engine/SpriteRenderer.md)

`SpriteRenderer` принимает прежние вызовы `Draw`, `DrawColored` и `DrawCollisionBox`, но обычные спрайты больше не отправляются в OpenGL по одному. CPU рассчитывает итоговые мировые координаты, UV, RGBA и texture slot четырёх вершин, после чего накапливает их в динамическом batch.

## Основной sprite batch

- один batch содержит до `2048` спрайтов;
- один спрайт занимает четыре вершины по десять `float`: `XYZ`, `UV`, `RGBA`, texture slot;
- статический EBO заранее содержит индексы для всех `2048` quad;
- VBO создаётся один раз с `DynamicDraw`, а `Flush()` обновляет только занятую часть через `BufferSubData`;
- один draw call может использовать до `16` разных `Texture.Handle` без texture atlas;
- порядок quad в index buffer совпадает с порядком вызовов, поэтому alpha blending и порядок слоёв не меняются.

`Draw` передаёт белый цвет и делегирует работу `DrawColored`. `DrawColored` сохраняет сигнатуру и добавляет quad в текущий batch. Позиция, размер, `rotationDeg`, `flipX`, `texOffset`, `texScale` и цвет рассчитываются на CPU.

## Auto-flush

Накопленная геометрия отправляется в GPU при одном из условий:

- достигнут лимит `2048` спрайтов;
- использованы все `16` texture slots и потребовалась новая текстура;
- изменились view/projection matrices, например при переходе от игровой камеры к `IsUI`;
- начинается `DrawCollisionBox` или solid UI batch;
- `Engine` меняет framebuffer либо blend state;
- завершён render frame.

`RenderSceneToFramebuffer` явно завершает batch перед применением освещения. `DrawFramebuffer` и `DrawColorOverlay` немедленно вызывают `Flush`, поскольку являются отдельными полноэкранными проходами.

## Одноцветный UI batch

Существующий `BeginSolidBatch`/`DrawSolid`/`EndSolidBatch` сохранён. Он использует отдельные `ui_batch.vert` и `ui_batch.frag` и объединяет рамки, заливки и пиксельный текст без texture sampling. При чередовании solid-геометрии и текстурных иконок renderer сбрасывает только предыдущую последовательность, сохраняя точный порядок прозрачности.

## Отладочная геометрия

`DrawCollisionBox` сначала вызывает `FlushAll`, затем использует отдельные статический VAO/VBO/EBO и `debug.vert`/`debug.frag`. После `PolygonMode.Line` обязательно восстанавливается `PolygonMode.Fill`.

## Диагностика

`BeginFrame` и `EndFrame` собирают `LastFrameDrawCalls` и `LastFrameQuads`. При включённом мониторинге `F5` значения `Draws` и `Quads` выводятся в заголовок окна рядом с FPS и временем кадра. Это позволяет сравнивать эффективность batch на одинаковой сцене.

Следует учитывать: batch снижает CPU/driver overhead от draw calls, но не устраняет дорогую игровую логику, загрузку ресурсов, чрезмерный overdraw или слишком частые обязательные flush между разными render passes.
