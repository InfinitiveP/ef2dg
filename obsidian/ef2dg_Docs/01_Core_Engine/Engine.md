# Engine

`Engine` — точка входа рантайма. `Program.Main` создаёт экземпляр и вызывает `Run()`.

## Инициализация

`Run()` создаёт окно `1280×720` с OpenGL 3.3 и подписывает обработчики `Load`, `Update`, `Render`, изменения framebuffer и закрытия. В `OnLoad` создаются `Camera`, `SpriteRenderer`, `ResourceManager`, `AudioEngine`, ImGui-контроллер, игровая `GameScene`, итоговый, сценовый, световой и transition snapshot framebuffer.

## Игровой цикл

`OnUpdate` обрабатывает `Esc`, режим Play/Edit, камеру редактора и FPS. В Play Mode активная сцена получает `FixedUpdate` с шагом `1/60` секунды, затем `Update`; накопитель ограничен `0,25` секунды. В Edit Mode физика и игровая логика не обновляются.

`OnRender` открывает frame статистики [SpriteRenderer](SpriteRenderer.md), рисует стек сцен в отдельный scene framebuffer. Каждая сцена рисуется по фазам `DrawWorld` → освещение `LightingRenderer` (если оно включено у сцены) → `DrawEditorOverlay` → `DrawUI`, поэтому HUD не затемняется (см. [Освещение](../05_Guides/Освещение.md)). Перед каждой сменой framebuffer или blend state накопленный sprite batch обязательно сбрасывается. После этого кадр компонуется в итоговый framebuffer с учётом активного [перехода](../05_Guides/Переходы%20между%20локациями.md) и показывается в [редакторе](../04_Editor_Tools/EditorUI.md) через ImGui. Отдельный snapshot framebuffer хранит старый кадр для `CrossFade` и `Slide`.

## Сцены

`ChangeScene`, `PushScene` и `PopScene` откладывают операцию до конца кадра. При смене сцену загружают только если конкретный экземпляр ещё не был инициализирован. Переключение из Play Mode временно ставит игру на паузу и затем возвращает прежний режим.

Перегрузки этих методов с `TransitionSettings`, а также `RunTransition` и `RequestSceneTransition`, выполняют действие в середине визуального эффекта. Пока переход активен, новые запросы игнорируются.

## Отладка

`F1`–`F4` включают отрисовку хитбоксов и зон атаки, `F5` — монитор производительности. В заголовке монитора отображаются FPS, frame time, число draw calls `SpriteRenderer`, число отправленных quad и глубина стека сцен. Флаги находятся в [DebugSettings](DebugSettings.md).
