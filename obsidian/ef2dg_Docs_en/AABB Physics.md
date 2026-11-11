# AABB Physics

🌐 **English** · [Русский](../ef2dg_Docs/AABB%20Physics.md)

An AABB (axis-aligned bounding box) is a rectangle whose sides are parallel to the axes. In `ef2dg` the hitbox size and offset are set by `HitboxSize`/`HitboxOffset` on a [PhysicsBody](03_Modules/PhysicsBody.md) and by `Size`/`Offset` on a `BoxCollider`.

Two rectangles intersect when their intervals overlap on both X and Y at the same time. For a solid tile the map checks: `left < tileRight`, `right > tileLeft`, `bottom < tileTop`, `top > tileBottom`.

`PhysicsBody` resolves X and Y separately, so a collision resets the corresponding velocity component. Slope tiles are handled separately: the map compares the normalized hitbox corner with the diagonal and returns the floor height.
