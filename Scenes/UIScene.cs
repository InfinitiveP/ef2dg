using ImGuiNET;
using Silk.NET.Input;
using System;
using System.Numerics;

namespace ef2dg
{
    public class UIScene : Scene
    {
        protected ResourceManager _resources = null!;
        protected IInputContext _inputContext = null!;

        public override void Load(ResourceManager resources, IInputContext input)
        {
            _resources = resources;
            _inputContext = input;
            
            // Фиктивная карта для работы редактора
            Map = new Tilemap(0.5f);
            Map.Layers.Add(new TileLayer { Name = "UI_Layer", HasCollision = false });
        }

        public override void Update(float deltaTime) 
        {
            base.Update(deltaTime);
        }

        // Этот метод вызывается из Engine.cs внутри контекста ImGui
        public void DrawMenuUI(Vector2 containerSize, Action<Scene> changeScene, Action<Scene> pushScene)
        {
            // Создаем невидимый холст для UI элементов
            ImGui.BeginChild("##MenuCanvas", containerSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground);

            // Перебираем все сущности на сцене
            foreach (var entity in Entities)
            {
                var button = entity.GetComponent<ButtonComponent>();
                if (button != null)
                {
                    // Используем координаты сущности для позиционирования кнопки
                    // Переводим локальные координаты (например, от 0 до 1 или пиксели) в позицию курсора ImGui
                    ImGui.SetCursorPos(new Vector2(entity.Position.X, entity.Position.Y));

                    // Отрисовываем кнопку
                    if (ImGui.Button($"{button.Label}##{entity.GetHashCode()}", new Vector2(button.Size.X, button.Size.Y)))
                    {
                        // Обработка нажатия: пытаемся найти сцену по имени через Reflection
                        if (!string.IsNullOrEmpty(button.TargetSceneName))
                        {
                            Engine.Instance.RequestSceneTransition(button.TargetSceneName, button.IsOverlay, button.Transition);
                        }
                    }
                }
            }

            ImGui.EndChild();
        }
    }
}
