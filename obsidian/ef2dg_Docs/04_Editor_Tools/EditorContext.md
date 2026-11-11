# EditorContext

🌐 **Русский** · [English](../../ef2dg_Docs_en/04_Editor_Tools/EditorContext.md)

`EditorContext` — структура, которую `Engine` создаёт каждый кадр и передаёт [EditorUI](EditorUI.md). Она содержит текущую сцену, основной framebuffer, игровую камеру, размер окна, `ResourceManager` и делегаты `ChangeScene`, `PushScene`, `PopScene`.

Контекст не хранится между кадрами. Он даёт окнам редактора доступ к рантайму без глобального поиска зависимостей.
