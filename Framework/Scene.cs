using System.Collections.Generic;

namespace ef2dg
{
    public abstract class Scene
    {
        public string InstanceName { get; set; } = "";
        // File the scene was loaded from or last saved to (for File → Save / Ctrl+S)
        public string FilePath { get; set; } = "";
        public Tilemap Map { get; set; } = null!;
        // List of every object in the scene
        public List<GameObject> Entities { get; protected set; } = new List<GameObject>();
        public Camera? ActiveCamera { get; set; }
        public float SimulationPadding { get; set; } = 8.0f;
        // Lighting of this location (Lighting window), saved with the level
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
                // Components may remove an entity while loading runtime state
                // or while running a mutation queue. In that case the original index
                // may already be past the new end of the list.
                if (i >= Entities.Count) continue;
                var entity = Entities[i];
                if (IsInSimulationZone(entity))
                {
                    entity.FixedUpdate(fixedDeltaTime);
                }
            }
        }

        // Method for loading level resources
        public abstract void Load(ResourceManager resources, Silk.NET.Input.IInputContext input);

        public virtual void Update(float deltaTime)
        {
            Map?.Update(deltaTime);
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Engine.Instance.IsTransitionActive) break;
                // Components may modify the list during the current frame. The check
                // keeps the backward pass safe after entities are removed.
                if (i >= Entities.Count) continue;
                var entity = Entities[i];
                if (IsInSimulationZone(entity))
                {
                    entity.Update(deltaTime);
                }
            }
        }

        // Engine calls the phases separately to apply lighting between the world and the HUD
        public virtual void Draw(SpriteRenderer renderer, Camera camera)
        {
            DrawWorld(renderer, camera);
            DrawEditorOverlay(renderer, camera);
            DrawUI(renderer, camera);
        }

        // Tiles, game objects and Foreground — lighting is applied only to this layer
        public virtual void DrawWorld(SpriteRenderer renderer, Camera camera)
        {
            if (Map != null)
            {
                // 1. Draw every map layer EXCEPT Foreground (i.e. Background and Ground)
                for (int i = 0; i < Map.Layers.Count; i++)
                {
                    if (Map.Layers[i].Name != "Foreground")
                        Map.DrawLayer(renderer, camera, i);
                }
            }

            // --- VISIBILITY AREA FOR GAME OBJECTS ---
            float camHalfW = camera.ViewWidth / 2f;
            float camHalfH = camera.ViewHeight / 2f;
            float cullPadding = 2.0f; 
            
            float camLeft = camera.Position.X - camHalfW - cullPadding;
            float camRight = camera.Position.X + camHalfW + cullPadding;
            float camBottom = camera.Position.Y - camHalfH - cullPadding;
            float camTop = camera.Position.Y + camHalfH + cullPadding;

            // 2. DRAW THE WORLD: only NON-UI objects, culling invisible ones
            foreach (var entity in Entities)
            {
                if (!entity.IsUI)
                {
                    // Check whether the object is inside the camera bounds
                    if (entity.Position.X >= camLeft && entity.Position.X <= camRight &&
                        entity.Position.Y >= camBottom && entity.Position.Y <= camTop)
                    {
                        entity.Draw(renderer, camera);
                    }
                }
            }

            if (Map != null)
            {
                // 3. Draw the Foreground layer (covers the game world)
                for (int i = 0; i < Map.Layers.Count; i++)
                {
                    if (Map.Layers[i].Name == "Foreground")
                        Map.DrawLayer(renderer, camera, i);
                }
            }
        }

        // Debug gizmos: above lighting so they do not get lost in the dark, but below the HUD
        public virtual void DrawEditorOverlay(SpriteRenderer renderer, Camera camera) { }

        public virtual void DrawUI(SpriteRenderer renderer, Camera camera)
        {
            // 4. DRAW THE INTERFACE (HUD) on top of everything else

            var tempPos = camera.Position;
            var tempZoom = camera.Zoom;

            // Pin the camera to the screen centre for UI
            camera.Position = new Silk.NET.Maths.Vector3D<float>(0, 0, 3.0f);
            camera.Zoom = 1.0f;

            foreach (var entity in Entities)
            {
                if (entity.IsUI)
                {
                    // UI is not culled because it is attached to the screen
                    entity.Draw(renderer, camera);
                }
            }

            // Restore the game camera
            camera.Position = tempPos;
            camera.Zoom = tempZoom;
        }
    }
}
