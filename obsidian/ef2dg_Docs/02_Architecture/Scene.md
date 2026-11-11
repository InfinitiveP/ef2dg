# Scene

🌐 **Русский** · [English](../../ef2dg_Docs_en/02_Architecture/Scene.md)

`Scene` — абстрактный контейнер уровня. Он содержит `Entities`, `Map`, `InstanceName`, `FilePath`, `ActiveCamera`, `SimulationPadding` и настройки освещения `Lighting` ([Освещение](../05_Guides/Освещение.md)). Наследник обязан реализовать `Load(ResourceManager, IInputContext)`.

`FixedUpdate` и `Update` вызывают одноимённые методы сущностей только в зоне камеры с запасом `SimulationPadding`, если у сущности не включён `AlwaysActive`.

Отрисовка разделена на фазы, которые `Engine` вызывает по отдельности:

1. `DrawWorld` — слои `Background` и `Ground`, видимые не-UI сущности, затем `Foreground`;
2. освещение сцены (`LightingRenderer`), если оно включено;
3. `DrawEditorOverlay` — отладочные гизмо и хитбоксы поверх света;
4. `DrawUI` — сущности с `IsUI = true` экранной камерой; HUD не затемняется.

`Draw` сохранён и вызывает те же три фазы подряд без освещения.

`GameScene` создаёт стартового игрока при новой сцене. `UIScene` создаёт служебный тайлмап и использует элементы с `ButtonComponent`.
