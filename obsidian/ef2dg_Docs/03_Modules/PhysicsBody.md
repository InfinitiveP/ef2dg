# PhysicsBody

`PhysicsBody` — кинематический компонент физики. Он хранит `Velocity`, `HitboxSize`, `HitboxOffset`, `GravityScale`, `IsGrounded`, `EnableStepUp` и ссылку на [Tilemap](Tilemap.md).

В `FixedUpdate` скорость по Y получает гравитацию `15 × GravityScale`; затем компонент отдельно разрешает перемещение по X и Y. Для платформенного режима (`GravityScale > 0`) поддерживаются пол и склоны через `GetSlopeFloorY`; для вида сверху гравитацию можно выключить.

После движения компонент ищет `BoxCollider` с `IsTrigger = true` и вызывает события входа, пребывания и выхода. Обычные `BoxCollider` не участвуют в разрешении твёрдых столкновений: столкновения с миром обеспечиваются картой.
