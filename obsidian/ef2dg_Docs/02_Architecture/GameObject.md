# GameObject

`GameObject` — сущность сцены с `Name`, `Position`, `Scale`, `Rotation`, `ParentScene`, `AlwaysActive` и `IsUI`. Поведение хранится в словаре компонентов по типу.

`AddComponent<T>` добавляет компонент и запускает его; `GetComponent<T>` возвращает компонент того же точного типа; `RemoveComponent` удаляет его. `GetAllComponents` используется [SceneSerializer](../03_Modules/SceneSerializer.md) и инспектором.

`AlwaysActive` отключает отсечение обновлений за камерой. `IsUI` отделяет экранный интерфейс от игрового мира: в [Scene](Scene.md) UI рисуется после мира с временно зафиксированной камерой.
