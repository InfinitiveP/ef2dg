# Shader

`Shader` загружает исходники GLSL из файлов, компилирует vertex- и fragment-шейдеры и линкует программу OpenGL. Ошибки компиляции или линковки превращаются в исключение с текстом драйвера.

Методы `Use`, `GetUniformLocation`, `SetUniform(Matrix4X4)` и `SetUniform(Vector2D)` применяются [SpriteRenderer](SpriteRenderer.md). Основные файлы — `shader.vert` и `shader.frag`; контуры отладки рисует пара `debug.vert` и `debug.frag`.

Основной vertex shader получает уже преобразованные на CPU мировые координаты, итоговые UV, RGBA и texture slot. В нём остаются только `uView` и `uProjection`. Fragment shader выбирает одну из `16` текстур batch и умножает sampled color на vertex color. Выбор оформлен явным `switch`, чтобы не зависеть от ограничений динамической индексации массива `sampler2D` на драйверах OpenGL 3.3.

Дополнительно `SpriteRenderer` создаёт `ui_batch` shader для одноцветной UI-геометрии и debug shader для контуров. Экземпляры создаются один раз при старте и освобождаются вместе с renderer.
