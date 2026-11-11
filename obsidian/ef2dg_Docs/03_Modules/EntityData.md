# EntityData

`EntityData` — сериализованное представление [GameObject](../02_Architecture/GameObject.md). Поля `Name`, `X`, `Y`, `ScaleX`, `ScaleY` и `Rotation` составляют transform, а `Components` хранит список [ComponentData](ComponentData.md).

При загрузке `SceneSerializer` создаёт новый `GameObject`, назначает `ParentScene`, восстанавливает transform, добавляет компоненты и затем помещает сущность в `Scene.Entities`.
