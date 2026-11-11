using System.Collections.Generic;

namespace ef2dg
{
    public abstract class Scene
    {
        public string InstanceName { get; set; } = "";
        // Файл, из которого сцена загружена или в который сохранена последней (для File → Save / Ctrl+S)
        public string FilePath { get; set; } = "";
        public Tilemap Map { get; set; } = null!;
        // Список всех объектов на сцене
        public List<GameObject> Entities { get; protected set; } = new List<GameObject>();
        public Camera? ActiveCamera { get; set; }
        public float SimulationPadding { get; set; } = 8.0f;
        // Освещение этой локации (окно Lighting), сохраняется вместе с уровнем
        public SceneLighting Lighting { get; set; } = new SceneLighting();

        private bool IsInSimulationZone(GameObject entity)
        {
            if (entity.AlwaysActive) return true;
            if (ActiveCamera == null) return true;

            float halfW = (ActiveCamera.ViewWidth / 2f) + SimulationPadding;
            float halfH = (ActiveCamera.ViewHeight / 2f) + SimulationPadding;

            float left = ActiveCamera.Position.X - halfW;
            float right = ActiveCamera.Position.X + halfW;
            float bottom = ActiveCamera.Position.Y - halfH;
            float top = ActiveCamera.Position.Y + halfH;

            return entity.Position.X >= left && entity.Position.X <= right &&
                entity.Position.Y >= bottom && entity.Position.Y <= top;
        }

        public virtual void FixedUpdate(float fixedDeltaTime)
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Engine.Instance.IsTransitionActive) break;
                // Компоненты могут удалить сущность при загрузке runtime-состояния
                // или при выполнении очереди мутаций. В таком случае исходный индекс
                // уже может выходить за новые границы списка.
                if (i >= Entities.Count) continue;
                var entity = Entities[i];
                if (IsInSimulationZone(entity))
                {
                    entity.FixedUpdate(fixedDeltaTime);
                }
            }
        }

        // Метод для загрузки ресурсов уровня
        public abstract void Load(ResourceManager resources, Silk.NET.Input.IInputContext input);

        public virtual void Update(float deltaTime)
        {
            Map?.Update(deltaTime);
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Engine.Instance.IsTransitionActive) break;
                // Список разрешено изменять из компонентов текущего кадра. Проверка
                // сохраняет обратный проход безопасным после удаления сущностей.
                if (i >= Entities.Count) continue;
                var entity = Entities[i];
                if (IsInSimulationZone(entity))
                {
                    entity.Update(deltaTime);
                }
            }
        }

        // Engine вызывает фазы по отдельности, чтобы наложить освещение между миром и HUD
        public virtual void Draw(SpriteRenderer renderer, Camera camera)
        {
            DrawWorld(renderer, camera);
            DrawEditorOverlay(renderer, camera);
            DrawUI(renderer, camera);
        }

        // Тайлы, игровые объекты и Foreground — освещение применяется только к этому слою
        public virtual void DrawWorld(SpriteRenderer renderer, Camera camera)
        {
            if (Map != null)
            {
                // 1. Рисуем все слои карты, КРОМЕ Foreground (т.е. Background и Ground)
                for (int i = 0; i < Map.Layers.Count; i++)
                {
                    if (Map.Layers[i].Name != "Foreground")
                        Map.DrawLayer(renderer, camera, i);
                }
            }

            // --- РАСЧЕТ ЗОНЫ ВИДИМОСТИ ДЛЯ ИГРОВЫХ ОБЪЕКТОВ ---
            float camHalfW = camera.ViewWidth / 2f;
            float camHalfH = camera.ViewHeight / 2f;
            float cullPadding = 2.0f; 
            
            float camLeft = camera.Position.X - camHalfW - cullPadding;
            float camRight = camera.Position.X + camHalfW + cullPadding;
            float camBottom = camera.Position.Y - camHalfH - cullPadding;
            float camTop = camera.Position.Y + camHalfH + cullPadding;

            // 2. РИСУЕМ МИР: только НЕ-интерфейсные объекты с отсечением невидимого
            foreach (var entity in Entities)
            {
                if (!entity.IsUI)
                {
                    // Проверяем, находится ли объект в пределах камеры
                    if (entity.Position.X >= camLeft && entity.Position.X <= camRight &&
                        entity.Position.Y >= camBottom && entity.Position.Y <= camTop)
                    {
                        entity.Draw(renderer, camera);
                    }
                }
            }

            if (Map != null)
            {
                // 3. Рисуем слой Foreground (перекрывает игровой мир)
                for (int i = 0; i < Map.Layers.Count; i++)
                {
                    if (Map.Layers[i].Name == "Foreground")
                        Map.DrawLayer(renderer, camera, i);
                }
            }
        }

        // Отладочные гизмо: поверх освещения, чтобы не терялись в темноте, но под HUD
        public virtual void DrawEditorOverlay(SpriteRenderer renderer, Camera camera) { }

        public virtual void DrawUI(SpriteRenderer renderer, Camera camera)
        {
            // 4. РИСУЕМ ИНТЕРФЕЙС (HUD) поверх всего остального

            var tempPos = camera.Position;
            var tempZoom = camera.Zoom;

            // Фиксируем камеру по центру экрана для UI
            camera.Position = new Silk.NET.Maths.Vector3D<float>(0, 0, 3.0f);
            camera.Zoom = 1.0f;

            foreach (var entity in Entities)
            {
                if (entity.IsUI)
                {
                    // Для UI не делаем проверку видимости (culling), так как он привязан к экрану
                    entity.Draw(renderer, camera);
                }
            }

            // Возвращаем игровую камеру на место
            camera.Position = tempPos;
            camera.Zoom = tempZoom;
        }
    }
}
