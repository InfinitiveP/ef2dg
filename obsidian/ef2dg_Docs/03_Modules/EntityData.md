# EntityData

🌐 **Русский** · [English](../../ef2dg_Docs_en/03_Modules/EntityData.md)

`EntityData` — сериализованное представление [GameObject](../02_Architecture/GameObject.md). Поля `Name`, `X`, `Y`, `ScaleX`, `ScaleY` и `Rotation` составляют transform, `AlwaysActive` и `IsUI` хранят флаги сущности, а `Components` — список [ComponentData](ComponentData.md).

При загрузке `SceneSerializer` создаёт новый `GameObject`, назначает `ParentScene`, восстанавливает transform, добавляет компоненты и затем помещает сущность в `Scene.Entities`.
