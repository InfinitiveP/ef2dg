# SpriteComponent

`SpriteComponent` хранит анимации, текущий кадр, направление `FlipX`, параметры аниматора и зону удара. `AnimationState` задаёт путь текстуры, ширину кадра, скорость, зацикливание, правила перехода и звуки кадров.

`Play` подгружает текстуру через `Engine.Resources`, сбрасывает кадр и запускает состояние. В `Update` компонент проверяет правила `AnimationTransition`, проигрывает `SoundEvents` через `AudioSourceComponent` и продвигает кадры. `Draw` вычисляет UV прямоугольника текущего кадра и передаёт его [SpriteRenderer](../01_Core_Engine/SpriteRenderer.md).

`AnimationsJson` и `ParametersJson` нужны для сохранения словарей сложных данных через строковые свойства сериализатора.
