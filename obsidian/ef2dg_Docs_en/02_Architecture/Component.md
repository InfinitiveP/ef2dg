# Component

🌐 **English** · [Русский](../../ef2dg_Docs/02_Architecture/Component.md)

`Component` is the abstract base class for behaviour. Once added to a [GameObject](GameObject.md), its `Owner` property points to that object. For convenience, the protected `Position`, `Sprite` and `Physics` shortcuts are available.

## Lifecycle

`AddComponent` and `AddComponentDynamic` assign `Owner` and call `Start` immediately. After that the scene calls `FixedUpdate`, `Update` and `Draw` through the owner. Trigger physics calls `OnTriggerEnter`, `OnTriggerStay` and `OnTriggerExit`.

Public settable properties of type `float`, `bool`, `int`, `string`, `Vector2D<float>` and `Vector4D<float>` are serialized by [SceneSerializer](../03_Modules/SceneSerializer.md). Complex or computed-only members need dedicated serialization support.

## Important details

- A `GameObject` can hold only one component of each exact type: the dictionary is keyed by `Type`.
- `GetComponent<T>()` looks for an exact type match, not for any derived class.
- `Start()` is called as soon as the component is added. When a JSON level is loaded, this happens **before** the saved properties are assigned.
- A component created by the editor or the loader must have a public parameterless constructor.
- Do not rely on the execution order of different components: it depends on the internal dictionary.
- Removing a component does not call any teardown method — there is no such lifecycle hook yet.

The full template and rules are in [Component Development](../05_Guides/Component%20Development.md).
