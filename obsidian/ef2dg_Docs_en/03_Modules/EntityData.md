# EntityData

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/EntityData.md)

`EntityData` is the serialized form of a [GameObject](../02_Architecture/GameObject.md). The `Name`, `X`, `Y`, `ScaleX`, `ScaleY` and `Rotation` fields make up the transform, `AlwaysActive` and `IsUI` keep the entity flags, and `Components` holds a list of [ComponentData](ComponentData.md).

On load, `SceneSerializer` creates a new `GameObject`, assigns `ParentScene`, restores the transform, adds the components and then puts the entity into `Scene.Entities`.
