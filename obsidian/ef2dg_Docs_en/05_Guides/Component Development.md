# Component Development

🌐 **English** · [Русский](../../ef2dg_Docs/05_Guides/Разработка%20компонентов.md)

A component is the main way to add behaviour to an object. It inherits from `Component`, receives an `Owner` and is called through the [GameObject](../02_Architecture/GameObject.md) and the [Scene](../02_Architecture/Scene.md).

## Minimal component

```csharp
namespace ef2dg
{
    public class HealthComponent : Component
    {
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;

        public HealthComponent() { }

        public override void Update(float deltaTime)
        {
            if (CurrentHealth < 0) CurrentHealth = 0;
        }
    }
}
```

After a build the type automatically appears in `Inspector → Add Component` if it:

- is in a loaded assembly;
- inherits from `Component`;
- is not `abstract`;
- has a public parameterless constructor;
- is not already added to the selected object.

To load JSON, the current `SceneSerializer` looks the type up strictly as `ef2dg.<TypeName>`. The editor will see a component from another namespace through reflection, but the serializer will not restore it without code changes.

## Lifecycle

### Start

Called immediately from `AddComponent`/`AddComponentDynamic`, not on the first frame. At this point `Owner` is assigned, but `ParentScene`, sibling components and serialized settings may not be ready yet.

When loading JSON, `Start()` is called **before** the saved properties are assigned. So it is safe to use it for values that do not depend on configuration, or just to register initial state. If you need initialization after loading, use a lazy flag:

```csharp
private bool _initialized;

public override void Update(float deltaTime)
{
    if (!_initialized)
    {
        _initialized = true;
        // Public properties are already restored here.
    }
}
```

### FixedUpdate

Called only in Play Mode with a fixed step of `1/60` s. Physics and logic that depends on a stable step belong here. `Scene` skips entities outside the camera area unless `AlwaysActive = true`.

### Update

Called once per game frame in Play Mode. Use `deltaTime` for timers and non-physics movement. Do not modify the `Scene.Entities` collection inside a `foreach`; if you need removal, use an index loop from the end or a deferred queue.

### Draw

Called every frame in both Edit Mode and Play Mode. The method must only draw and must not change game state. The camera is passed as an argument. Some system components, such as `LightComponent`, intentionally draw nothing themselves: they are handled by a separate `Engine` pass. World entities are drawn before lighting and are affected by it; entities with `IsUI = true` are drawn after it.

### Trigger callbacks

`OnTriggerEnter`, `OnTriggerStay` and `OnTriggerExit` are called by `PhysicsBody` when its AABB overlaps another object with `BoxCollider.IsTrigger = true`. For a zone to work:

1. the moving object must have a `PhysicsBody`;
2. the zone must have a `BoxCollider` with `IsTrigger = true`;
3. both objects must be in the same `ParentScene`;
4. the game scene must be updating in Play Mode.

`BoxCollider.IsTrigger = false` does not create a solid obstacle for `PhysicsBody` yet; solid collisions go through the [Tilemap](../03_Modules/Tilemap.md).

## Accessing the owner and sibling components

`Owner` is assigned by the engine. Protected base-class properties shorten common lookups:

- `Position` reads/changes `Owner.Position`;
- `Sprite` returns the exact `SpriteComponent`;
- `Physics` returns the exact `PhysicsBody`.

For other types use `Owner.GetComponent<T>()`. It looks for the exact type only. An object holds at most one instance of each type, because components live in a `Dictionary<Type, Component>`.

Always allow for a missing dependency:

```csharp
public override void Update(float deltaTime)
{
    var audio = Owner.GetComponent<AudioSourceComponent>();
    if (Sprite == null || Physics == null) return;
    // Work with the dependencies.
}
```

## Properties, the Inspector and JSON

A public property with a setter automatically appears in the generic Inspector panel if its type is supported. But display and saving are different systems.

| Type | Inspector | JSON |
|---|---:|---:|
| `float` | yes | yes |
| `bool` | yes | yes |
| `int` | yes | yes |
| `string` | yes | yes |
| `Vector2D<float>` | yes | yes |
| `Vector4D<float>` | yes | yes |
| `TransitionSettings` | yes | yes |
| enum | no | no |
| list/dictionary/object | no | no |
| property without a setter | no | no |
| private field | no | no |

For a dictionary you can add a string adapter, like `SpriteComponent` does:

```csharp
public Dictionary<string, float> Values { get; set; } = new();

public string ValuesJson
{
    get => JsonSerializer.Serialize(Values);
    set => Values = JsonSerializer.Deserialize<Dictionary<string, float>>(value) ?? new();
}
```

`ValuesJson` will then be saved as a `string`. To hide the technical field from the generic Inspector panel, add its name to the `InspectorWindow` filter or write a custom editor.

Runtime references and caches that must not end up in the level should be marked with `[SceneTransient]`: such properties are ignored on both save and load.

For a configurable transition a public property is enough:

```csharp
public TransitionSettings Transition { get; set; } = new TransitionSettings();
```

The Inspector automatically shows the kind, duration and colour, and `SceneSerializer` saves the settings. Run it with `Engine.Instance.RunTransition` or `RequestSceneTransition`; see [Scene Transitions](Scene%20Transitions.md).

## Constructors

You can have extra constructors for manual creation in code, but the public parameterless constructor is required by the editor and the serializer:

```csharp
public PhysicsLikeComponent() { }

public PhysicsLikeComponent(float speed)
{
    Speed = speed;
}
```

Do not load OpenGL resources in a constructor: `Engine.Resources` and the context may not be ready yet. Use `ResourceManager` at the right moment or store the path and load lazily.

## Working with the scene

Once an entity is placed in a scene, `Owner.ParentScene` must point to it. The editor and the serializer do this automatically. For entities created by hand, assign the reference before adding components whose `Start()` uses the scene:

```csharp
var entity = new GameObject
{
    Name = "Chest",
    ParentScene = this,
    Position = new Vector2D<float>(2f, 1f)
};
entity.AddComponent(new SpriteComponent());
Entities.Add(entity);
```

## Working with PhysicsBody

`PhysicsBody.Map` must reference the current scene's map. The Inspector assigns it when the component is added, and the serializer after all components are restored. When creating it by hand, pass the map through the constructor or the property.

`GravityScale > 0` enables the platformer model with gravity and slopes; `GravityScale = 0` suits a top-down view. `EnableStepUp` only makes sense with positive gravity.

## Animation and input

A component can control a `SpriteComponent` through `SetFloat`, `SetBool`, `SetTrigger` and `Play`. Parameter and state names are strings and are case-sensitive. Calling `Play` with a missing state or an empty texture silently does nothing.

For input, use a logical action:

```csharp
if (InputManager.IsActionPressed("Interact"))
{
    // The component's action.
}
```

Adding an action in `Project Settings` only stores the binding; the component still has to query it.

## Logs and diagnostics

Write messages with `Console.WriteLine`; they appear in the `Console` window. Use a stable component prefix:

```csharp
Console.WriteLine("[HealthComponent] Entity died.");
```

Do not log every `Update` — it quickly floods the console and hurts performance.

## Common mistakes

- No parameterless constructor — `Add Component` or loading throws an exception.
- The component is not in the `ef2dg` namespace — it is saved by name but not restored by the current serializer.
- Initializing from settings in `Start()` — default values are used on load.
- Using an unsupported type — the property is visible or works in code, but disappears after loading.
- Expecting two components of the same type — the second replaces the first in the dictionary.
- `PhysicsBody.Map == null` — physics exits `FixedUpdate` immediately.
- Changing the entity list inside a plain `foreach` — a collection-modified error is possible.
- Game logic in `Draw()` — it will also run in Edit Mode.

## New component checklist

- [ ] The class is public, non-abstract and in `namespace ef2dg`.
- [ ] There is a public parameterless constructor.
- [ ] Public settings use types the serializer supports.
- [ ] `Start()` does not depend on values that are not restored yet.
- [ ] Every sibling component is checked for `null`.
- [ ] Physics is in `FixedUpdate`, other logic in `Update`.
- [ ] `Draw` does not change state.
- [ ] Saving, reloading and behaviour after Stop and another Play have been checked.
