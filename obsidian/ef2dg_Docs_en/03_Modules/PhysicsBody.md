# PhysicsBody

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/PhysicsBody.md)

`PhysicsBody` is a kinematic physics component. It stores `Velocity`, `HitboxSize`, `HitboxOffset`, `GravityScale`, `IsGrounded`, `EnableStepUp` and a reference to the [Tilemap](Tilemap.md).

In `FixedUpdate`, the Y velocity receives gravity of `15 × GravityScale`; the component then resolves movement along X and Y separately. In platformer mode (`GravityScale > 0`) floors and slopes are supported through `GetSlopeFloorY`; for a top-down view, gravity can be turned off.

After moving, the component looks for `BoxCollider`s with `IsTrigger = true` and raises enter, stay and exit events. Regular `BoxCollider`s take no part in solid collision resolution: collisions with the world are provided by the map.
