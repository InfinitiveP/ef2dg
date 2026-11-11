# Tilemap

🌐 **Русский** · [English](../../ef2dg_Docs_en/03_Modules/Tilemap.md)

`Tilemap` содержит `Layers`, `TileConfigs`, `TileSizeInWorld` и `BasePixelSize`. Новая карта получает слои `Background`, `Ground` и `Foreground`; коллизии по умолчанию включены только у `Ground`.

`DrawLayer` перебирает только клетки в области камеры с запасом, получает текстуру из [ResourceManager](../01_Core_Engine/ResourceManager.md) и применяет настройки масштаба, UV, отражения и анимации. `HasCollision` проверяет тайлы с `Solid`, `SlopeLeft` и `SlopeRight`; `GetSlopeFloorY` возвращает высоту пола для физики.

В редакторе тайл хранится как путь к изображению по координатам сетки. Настройки `TileSettings` сохраняются вместе с уровнем.
