# InputManager

`InputManager` связывает логические действия со значениями `Silk.NET.Input.Key`. Файл `bindings.json` хранит словарь; при загрузке автоматически добавляются отсутствующие стандартные действия: `Left`, `Right`, `Up`, `Down`, `Jump`, `Attack`.

Игровой код вызывает `IsActionPressed("Attack")`, а не проверяет конкретную клавишу. `IsKeyPressed` предназначен для технических проверок, `GetAnyPressedKey` — для переназначения управления в редакторе.

Для runtime UI `SceneViewWindow` сохраняет экранные границы изображения framebuffer. В начале следующего кадра, после обновления ImGui input и до `Scene.Update`, текущий указатель переводится в координаты экранной UI-камеры через `UpdateRuntimePointer` и передаётся в `SetUiPointerState`. Доступны `UiPointerPosition`, `IsUiPointerInside`, `IsUiPointerLeftDown`, `IsUiPointerRightDown` и `UiPointerWheel`. Это не raw-координаты окна: они уже учитывают положение docked `Scene View` и размер viewport.

Порядок «ImGui input → pointer transform → игровое `Update`» принципиален: если обновлять pointer только во время `Scene View.Draw`, UI получит состояние с задержкой и может потерять короткий клик между кадрами.

UI-координаты вычисляются для фиксированной высоты обзора `2` (`Zoom = 1`) и `Camera.AspectRatio`. Использовать `Camera.ViewWidth/ViewHeight` напрямую нельзя: эти свойства включают zoom игровой камеры, тогда как `Scene.Draw` перед отрисовкой `IsUI` временно устанавливает zoom интерфейса в `1`.

Перед расширением мыши сохранена резервная копия `Core/InputManager.cs.backup-2026-10-12`; файл не имеет расширения `.cs` и не участвует в компиляции.
