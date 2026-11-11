# ⚙️ Техническая вики ef2dg

🌐 **Русский** · [English](../../ef2dg_Docs_en/00_Dashboard/00_Main.md)

`ef2dg` — самописный 2D-движок и визуальный редактор на C#, Silk.NET и OpenGL 3.3. Вики описывает фактическое состояние исходников; будущие возможности отделены от реализованных.

## Начните здесь

- [Начало работы](../05_Guides/Начало%20работы.md) — требования, запуск и структура проекта.
- [Полное руководство по редактору](../04_Editor_Tools/Полное%20руководство%20по%20редактору.md) — все окна, режимы, инструменты и ограничения.
- [Разработка компонентов](../05_Guides/Разработка%20компонентов.md) — создание компонента, жизненный цикл, сериализация и инспектор.
- [Практическое руководство](../05_Guides/Практическое%20руководство.md) — создание и сохранение уровня от начала до запуска.
- [Ограничения и диагностика](../05_Guides/Ограничения%20и%20диагностика.md) — проверка сборки и известные ограничения.

## Архитектура

- [Engine](../01_Core_Engine/Engine.md) — окно, цикл, framebuffer, освещение и стек сцен.
- [Scene](../02_Architecture/Scene.md) — уровень и порядок его обновления/отрисовки.
- [GameObject](../02_Architecture/GameObject.md) и [Component](../02_Architecture/Component.md) — композиция поведения сущностей.
- [SceneSerializer](../03_Modules/SceneSerializer.md), [LevelData](../03_Modules/LevelData.md), [EntityData](../03_Modules/EntityData.md), [ComponentData](../03_Modules/ComponentData.md) — формат JSON-уровня.

## Подсистемы

- [Camera](../01_Core_Engine/Camera.md), [SpriteRenderer](../01_Core_Engine/SpriteRenderer.md), [Shader](../01_Core_Engine/Shader.md), [Texture](../01_Core_Engine/Texture.md), [ResourceManager](../01_Core_Engine/ResourceManager.md).
- [Tilemap](../03_Modules/Tilemap.md), [PhysicsBody](../03_Modules/PhysicsBody.md), [AABB Physics](../AABB%20Physics.md).
- [SpriteComponent](../03_Modules/SpriteComponent.md), [InputManager](../03_Modules/InputManager.md), [Аудио](../03_Modules/Аудио.md), [Редактор](../04_Editor_Tools/EditorUI.md).
- [Каталог компонентов](../03_Modules/Каталог%20компонентов.md) — назначение и зависимости встроенных компонентов.
- [Формат уровня и сериализация](../03_Modules/Формат%20уровня%20и%20сериализация.md) — что сохраняется в JSON и что теряется.

## Переходы

- [Переходы между локациями](../05_Guides/Переходы%20между%20локациями.md) — эффекты `Instant`, `Fade`, `CrossFade` и `Slide`, настройка компонентов и API.

## Освещение

- [Освещение](../05_Guides/Освещение.md) — освещение по сценам, источники света, bloom, виньетка, пресеты и окно Lighting.

## Игра на движке

Игры на `ef2dg` документируются в собственных репозиториях и не входят в движок. Эта техническая вики остаётся источником истины о реализованных возможностях `ef2dg`: наличие системы в дизайн-документах игры не означает, что она существует в движке.
