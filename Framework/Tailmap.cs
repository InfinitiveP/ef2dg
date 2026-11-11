using System;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace ef2dg
{
    public enum TileCollisionType { None, Solid, SlopeLeft, SlopeRight }

    public class TileSettings
    {
        public Vector2D<float> TextureScale { get; set; } = new Vector2D<float>(1f, 1f);
        public Vector2D<float> TextureOffset { get; set; } = new Vector2D<float>(0f, 0f);
        
        public bool FlipX { get; set; } = false;
        public bool FlipY { get; set; } = false;
        
        public Vector2D<float> HitboxScale { get; set; } = new Vector2D<float>(1f, 1f);
        public Vector2D<float> HitboxOffset { get; set; } = new Vector2D<float>(0f, 0f);
        public TileCollisionType CollisionType { get; set; } = TileCollisionType.Solid;

        public int FrameCount { get; set; } = 1;       // Сколько кадров в картинке (по умолчанию 1)
        public float AnimationSpeed { get; set; } = 5f; // Кадров в секунду
    }

    public class TileLayer
    {
        public string Name { get; set; } = "Layer";
        public bool HasCollision { get; set; } = false; 
        public Dictionary<Vector2D<int>, string> Grid { get; set; } = new Dictionary<Vector2D<int>, string>();
    }

    public class Tilemap
    {
        public float TileSizeInWorld { get; set; } 
        public float BasePixelSize { get; set; } = 64f; 
        
        public List<TileLayer> Layers { get; set; } = new List<TileLayer>();
        public Dictionary<string, TileSettings> TileConfigs { get; set; } = new Dictionary<string, TileSettings>();

        private float _animationTimer = 0f;

        public Tilemap(float tileSizeInWorld)
        {
            TileSizeInWorld = tileSizeInWorld;
            Layers.Add(new TileLayer { Name = "Background", HasCollision = false });
            Layers.Add(new TileLayer { Name = "Ground", HasCollision = true });
            Layers.Add(new TileLayer { Name = "Foreground", HasCollision = false });
        }

        public void Update(float deltaTime)
        {
            _animationTimer += deltaTime;
        }
        public void DrawLayer(SpriteRenderer renderer, Camera camera, int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= Layers.Count) return;
            var layer = Layers[layerIndex];

            // 1. Вычисляем половину ширины и высоты обзора камеры
            float halfW = camera.ViewWidth / 2f;
            float halfH = camera.ViewHeight / 2f;

            // 2. Переводим мировые координаты экрана в координаты клеток (Grid)
            // Добавляем буфер (-2 и +2 клетки по краям), чтобы тайлы не исчезали резко на границе экрана
            int minX = (int)MathF.Floor((camera.Position.X - halfW) / TileSizeInWorld) - 2;
            int maxX = (int)MathF.Ceiling((camera.Position.X + halfW) / TileSizeInWorld) + 2;
            int minY = (int)MathF.Floor((camera.Position.Y - halfH) / TileSizeInWorld) - 2;
            int maxY = (int)MathF.Ceiling((camera.Position.Y + halfH) / TileSizeInWorld) + 2;

            // 3. ВМЕСТО FOREACH ПО ВСЕЙ КАРТЕ, ИДЕМ ТОЛЬКО ПО ВИДИМОМУ КВАДРАТУ
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var gridPos = new Vector2D<int>(x, y);

                    // Поиск в словаре работает за O(1). 
                    // Если в этой видимой клетке нет тайла - мгновенно пропускаем
                    if (!layer.Grid.TryGetValue(gridPos, out string? path) || string.IsNullOrEmpty(path)) 
                        continue;

                    var tex = Engine.Resources.GetTexture(path);
                    
                    if (!TileConfigs.TryGetValue(path, out var config))
                    {
                        config = new TileSettings { TextureScale = new Vector2D<float>(tex.Width / BasePixelSize, tex.Height / BasePixelSize) };
                        TileConfigs[path] = config;
                    }

                    Vector2D<float> sizeInWorld;
                    if (tex.Width == 2 && tex.Height == 2) sizeInWorld = new Vector2D<float>(TileSizeInWorld, TileSizeInWorld);
                    else sizeInWorld = new Vector2D<float>(TileSizeInWorld * config.TextureScale.X, TileSizeInWorld * config.TextureScale.Y);
                    
                    float posX = x * TileSizeInWorld + config.TextureOffset.X;
                    float posY = y * TileSizeInWorld + (sizeInWorld.Y - TileSizeInWorld) / 2f + config.TextureOffset.Y; 
                    Vector2D<float> position = new Vector2D<float>(posX, posY);

                    // МАТЕМАТИКА КАДРОВ АНИМАЦИИ (как мы делали в прошлый раз)
                    float uvWidth = 1.0f / config.FrameCount; 
                    float startUvX = 0f;

                    if (config.FrameCount > 1 && config.AnimationSpeed > 0)
                    {
                        int currentFrame = (int)(_animationTimer * config.AnimationSpeed) % config.FrameCount;
                        startUvX = currentFrame * uvWidth;
                    }

                    Vector2D<float> uvOffset = new Vector2D<float>(config.FlipX ? startUvX + uvWidth : startUvX, config.FlipY ? 1f : 0f);
                    Vector2D<float> uvScale = new Vector2D<float>(config.FlipX ? -uvWidth : uvWidth, config.FlipY ? -1f : 1f);

                    renderer.Draw(tex, position, sizeInWorld, 0f, camera, uvOffset, uvScale, false);
                }
            }
        }
        
        // НОВОЕ: Идеальная математика столкновения AABB (квадрата) и Треугольника!
        public bool HasCollision(float left, float right, float top, float bottom)
        {
            float halfSize = TileSizeInWorld / 2f;
            int startX = (int)MathF.Floor((left + halfSize) / TileSizeInWorld);
            int endX = (int)MathF.Floor((right + halfSize) / TileSizeInWorld);
            int startY = (int)MathF.Floor((bottom + halfSize) / TileSizeInWorld);
            int endY = (int)MathF.Floor((top + halfSize) / TileSizeInWorld);

            foreach (var layer in Layers)
            {
                if (!layer.HasCollision) continue;

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        var gridPos = new Vector2D<int>(x, y);
                        // Фикс CS8600: используем string?
                        if (layer.Grid.TryGetValue(gridPos, out string? path) && !string.IsNullOrEmpty(path) && TileConfigs.TryGetValue(path, out var config))
                        {
                            if (config.CollisionType == TileCollisionType.Solid)
                            {
                                float tilePosX = x * TileSizeInWorld + config.HitboxOffset.X;
                                float tilePosY = y * TileSizeInWorld + config.HitboxOffset.Y;
                                float tileWidth = TileSizeInWorld * config.HitboxScale.X;
                                float tileHeight = TileSizeInWorld * config.HitboxScale.Y;

                                if (left < tilePosX + tileWidth / 2f && right > tilePosX - tileWidth / 2f && 
                                    bottom < tilePosY + tileHeight / 2f && top > tilePosY - tileHeight / 2f)
                                    return true;
                            }
                            // Математика диагональных стен (идеально для Top-Down)
                            else if (config.CollisionType == TileCollisionType.SlopeLeft || config.CollisionType == TileCollisionType.SlopeRight)
                            {
                                bool isLeft = config.CollisionType == TileCollisionType.SlopeLeft;
                                bool fX = config.FlipX ^ isLeft; // Логический XOR
                                bool fY = config.FlipY;

                                float tileLeftEdge = (x * TileSizeInWorld) - TileSizeInWorld / 2f;
                                float tileBottomEdge = (y * TileSizeInWorld) - TileSizeInWorld / 2f;

                                // Нормализуем координаты хитбокса персонажа в пространство тайла (от 0 до 1)
                                float localLeft = (left - tileLeftEdge) / TileSizeInWorld;
                                float localRight = (right - tileLeftEdge) / TileSizeInWorld;
                                float localBottom = (bottom - tileBottomEdge) / TileSizeInWorld;
                                float localTop = (top - tileBottomEdge) / TileSizeInWorld;

                                // Высчитываем самый "глубокий" угол хитбокса относительно склона
                                float effX_max = fX ? (1.0f - localLeft) : localRight;
                                float effY_min = fY ? (1.0f - localTop) : localBottom;

                                // Если самый глубокий угол пробил диагональ - это столкновение!
                                if (effY_min < effX_max) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // Добавлен аргумент includeSolid = false
        // Теперь метод возвращает кортеж: саму высоту (Y) и флажок, является ли этот пол склоном (IsSlope)
        public (float Y, bool IsSlope)? GetSlopeFloorY(float leftX, float rightX, float worldY, bool includeSolid = false)
        {
            (float Y, bool IsSlope)? bestFloor = null;
            float[] checkPoints = { leftX, (leftX + rightX) / 2f, rightX };

            foreach (float checkX in checkPoints)
            {
                int gridX = (int)MathF.Floor((checkX + TileSizeInWorld / 2f) / TileSizeInWorld);
                int baseGridY = (int)MathF.Floor((worldY + TileSizeInWorld / 2f) / TileSizeInWorld);

                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    int gridY = baseGridY + yOffset;

                    foreach (var layer in Layers)
                    {
                        if (!layer.HasCollision) continue;
                        
                        var gridPos = new Vector2D<int>(gridX, gridY);
                        if (layer.Grid.TryGetValue(gridPos, out string? path) && !string.IsNullOrEmpty(path) && TileConfigs.TryGetValue(path, out var config))
                        {
                            float currentFloorY = float.MinValue;
                            bool foundFloor = false;
                            bool isSlope = false; // НОВОЕ: Трекаем тип поверхности

                            if (config.CollisionType == TileCollisionType.SlopeRight || config.CollisionType == TileCollisionType.SlopeLeft)
                            {
                                if (config.FlipY) continue;

                                bool isLeft = config.CollisionType == TileCollisionType.SlopeLeft;
                                bool fX = config.FlipX ^ isLeft;
                                
                                float tileLeftEdge = (gridX * TileSizeInWorld) - (TileSizeInWorld / 2f);
                                float localX = (checkX - tileLeftEdge) / TileSizeInWorld;
                                if (localX < 0) localX = 0; if (localX > 1) localX = 1;

                                float heightOffset = fX ? (1.0f - localX) : localX;
                                float tileBottomEdge = (gridY * TileSizeInWorld) - (TileSizeInWorld / 2f);
                                
                                currentFloorY = tileBottomEdge + (heightOffset * TileSizeInWorld);
                                foundFloor = true;
                                isSlope = true; // Это математический наклон!
                            }
                            else if (includeSolid && config.CollisionType == TileCollisionType.Solid)
                            {
                                float topY = (gridY * TileSizeInWorld) + config.HitboxOffset.Y + (TileSizeInWorld * config.HitboxScale.Y / 2f);
                                
                                if (worldY >= topY - (TileSizeInWorld * 0.4f)) 
                                {
                                    currentFloorY = topY;
                                    foundFloor = true;
                                    isSlope = false; // Это плоский квадратный блок!
                                }
                            }

                            if (foundFloor)
                            {
                                if (worldY >= currentFloorY - (TileSizeInWorld * 1.5f) && worldY <= currentFloorY + (TileSizeInWorld * 1.5f))
                                {
                                    // Если нашли пол выше, запоминаем его вместе с его типом (склон/не склон)
                                    if (bestFloor == null || currentFloorY > bestFloor.Value.Y) 
                                    {
                                        bestFloor = (currentFloorY, isSlope);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return bestFloor;
        }

        public void DrawDebug(SpriteRenderer renderer, Camera camera, Vector4D<float> color)
        {
            // 1. Вычисляем границы обзора камеры
            float halfW = camera.ViewWidth / 2f;
            float halfH = camera.ViewHeight / 2f;

            // Считаем клетки, захватывая парочку запасных по краям
            int minX = (int)MathF.Floor((camera.Position.X - halfW) / TileSizeInWorld) - 2;
            int maxX = (int)MathF.Ceiling((camera.Position.X + halfW) / TileSizeInWorld) + 2;
            int minY = (int)MathF.Floor((camera.Position.Y - halfH) / TileSizeInWorld) - 2;
            int maxY = (int)MathF.Ceiling((camera.Position.Y + halfH) / TileSizeInWorld) + 2;

            foreach (var layer in Layers)
            {
                if (!layer.HasCollision) continue;

                // 2. Итерируемся ТОЛЬКО по видимой зоне экрана
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var gridPos = new Vector2D<int>(x, y);

                        // Если тайл есть и у него настроена коллизия - рисуем хитбокс
                        if (layer.Grid.TryGetValue(gridPos, out string? path) && !string.IsNullOrEmpty(path))
                        {
                            if (TileConfigs.TryGetValue(path, out var config) && config.CollisionType != TileCollisionType.None)
                            {
                                float posX = x * TileSizeInWorld + (config.CollisionType == TileCollisionType.Solid ? config.HitboxOffset.X : 0);
                                float posY = y * TileSizeInWorld + (config.CollisionType == TileCollisionType.Solid ? config.HitboxOffset.Y : 0);
                                
                                var size = new Vector2D<float>(
                                    TileSizeInWorld * (config.CollisionType == TileCollisionType.Solid ? config.HitboxScale.X : 1f), 
                                    TileSizeInWorld * (config.CollisionType == TileCollisionType.Solid ? config.HitboxScale.Y : 1f)
                                );
                                
                                DrawShape(renderer, camera, posX, posY, size, config.CollisionType, config.FlipX, config.FlipY, color);
                            }
                        }
                    }
                }
            }
        }

        public void DrawBrushHighlight(SpriteRenderer renderer, Camera camera, Vector2D<int> gridCell, string selectedPath)
        {
            float posX = gridCell.X * TileSizeInWorld;
            float posY = gridCell.Y * TileSizeInWorld;
            var size = new Vector2D<float>(TileSizeInWorld, TileSizeInWorld);
            var color = new Vector4D<float>(1, 1, 1, 0.5f);
            TileCollisionType type = TileCollisionType.Solid;
            bool flipX = false; bool flipY = false;

            if (!string.IsNullOrEmpty(selectedPath) && TileConfigs.TryGetValue(selectedPath, out var config))
            {
                if (config.CollisionType == TileCollisionType.Solid)
                {
                    posX += config.HitboxOffset.X; posY += config.HitboxOffset.Y;
                    size = new Vector2D<float>(TileSizeInWorld * config.HitboxScale.X, TileSizeInWorld * config.HitboxScale.Y);
                }
                type = config.CollisionType; flipX = config.FlipX; flipY = config.FlipY;
            }

            DrawShape(renderer, camera, posX, posY, size, type, flipX, flipY, color);
        }

        // НОВОЕ: Отрисовка плотного треугольника лесенкой!
        private void DrawShape(SpriteRenderer renderer, Camera camera, float posX, float posY, Vector2D<float> size, TileCollisionType type, bool flipX, bool flipY, Vector4D<float> color)
        {
            if (type == TileCollisionType.Solid)
            {
                renderer.DrawCollisionBox(new Vector2D<float>(posX, posY), size, camera, color);
            }
            else if (type == TileCollisionType.SlopeLeft || type == TileCollisionType.SlopeRight)
            {
                bool isLeft = type == TileCollisionType.SlopeLeft;
                bool fX = flipX ^ isLeft;
                bool fY = flipY;

                int steps = 10; 
                float stepW = size.X / steps;
                
                for(int i = 0; i < steps; i++)
                {
                    // Находим локальный X (от 0 до 1)
                    float localX = (i + 0.5f) / steps;
                    float effX = fX ? 1.0f - localX : localX;
                    
                    // Высота конкретного столбика
                    float h = effX * size.Y;
                    
                    float cx = posX - size.X/2f + i * stepW + stepW/2f;
                    
                    // Если потолок (FlipY), столбик "свисает" сверху вниз
                    float cy = fY ? (posY + size.Y/2f - h/2f) : (posY - size.Y/2f + h/2f);
                    
                    renderer.DrawCollisionBox(new Vector2D<float>(cx, cy), new Vector2D<float>(stepW, h), camera, color);
                }
            }
        }
    }
}