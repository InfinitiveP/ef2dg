# Tilemap

🌐 **English** · [Русский](../../ef2dg_Docs/03_Modules/Tilemap.md)

`Tilemap` contains `Layers`, `TileConfigs`, `TileSizeInWorld` and `BasePixelSize`. A new map gets the `Background`, `Ground` and `Foreground` layers; by default only `Ground` has collisions enabled.

`DrawLayer` iterates only over cells inside the camera area plus a margin, gets the texture from [ResourceManager](../01_Core_Engine/ResourceManager.md) and applies scale, UV, flipping and animation settings. `HasCollision` checks `Solid`, `SlopeLeft` and `SlopeRight` tiles; `GetSlopeFloorY` returns the floor height for physics.

In the editor a tile is stored as an image path at grid coordinates. `TileSettings` are saved together with the level.
