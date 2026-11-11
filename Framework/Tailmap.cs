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

        public int FrameCount { get; set; } = 1;       // Number of frames in the image (1 by default)
        public float AnimationSpeed { get; set; } = 5f; // Frames per second
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

            // 1. Compute half of the camera view width and height
            float halfW = camera.ViewWidth / 2f;
            float halfH = camera.ViewHeight / 2f;

            // 2. Convert the screen's world coordinates into grid cell coordinates
            // Add a margin (-2 and +2 cells at the edges) so tiles do not pop out abruptly at the screen border
            int minX = (int)MathF.Floor((camera.Position.X - halfW) / TileSizeInWorld) - 2;
            int maxX = (int)MathF.Ceiling((camera.Position.X + halfW) / TileSizeInWorld) + 2;
            int minY = (int)MathF.Floor((camera.Position.Y - halfH) / TileSizeInWorld) - 2;
            int maxY = (int)MathF.Ceiling((camera.Position.Y + halfH) / TileSizeInWorld) + 2;

            // 3. INSTEAD OF A FOREACH OVER THE WHOLE MAP, ITERATE ONLY OVER THE VISIBLE SQUARE
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var gridPos = new Vector2D<int>(x, y);

                    // Dictionary lookup is O(1).
                    // If this visible cell has no tile, skip it immediately
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

                    // ANIMATION FRAME MATH
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
        
        // Exact collision math between an AABB (box) and a triangle
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
                        // CS8600 fix: use string?
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
                            // Diagonal wall math (ideal for top-down)
                            else if (config.CollisionType == TileCollisionType.SlopeLeft || config.CollisionType == TileCollisionType.SlopeRight)
                            {
                                bool isLeft = config.CollisionType == TileCollisionType.SlopeLeft;
                                bool fX = config.FlipX ^ isLeft; // Logical XOR
                                bool fY = config.FlipY;

                                float tileLeftEdge = (x * TileSizeInWorld) - TileSizeInWorld / 2f;
                                float tileBottomEdge = (y * TileSizeInWorld) - TileSizeInWorld / 2f;

                                // Normalize the character hitbox coordinates into tile space (0 to 1)
                                float localLeft = (left - tileLeftEdge) / TileSizeInWorld;
                                float localRight = (right - tileLeftEdge) / TileSizeInWorld;
                                float localBottom = (bottom - tileBottomEdge) / TileSizeInWorld;
                                float localTop = (top - tileBottomEdge) / TileSizeInWorld;

                                // Find the "deepest" hitbox corner relative to the slope
                                float effX_max = fX ? (1.0f - localLeft) : localRight;
                                float effY_min = fY ? (1.0f - localTop) : localBottom;

                                // If the deepest corner crossed the diagonal, it is a collision!
                                if (effY_min < effX_max) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // Added argument includeSolid = false
        // The method returns a tuple: the height (Y) and a flag telling whether this floor is a slope (IsSlope)
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
                            bool isSlope = false; // Track the surface type

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
                                isSlope = true; // This is a mathematical slope!
                            }
                            else if (includeSolid && config.CollisionType == TileCollisionType.Solid)
                            {
                                float topY = (gridY * TileSizeInWorld) + config.HitboxOffset.Y + (TileSizeInWorld * config.HitboxScale.Y / 2f);
                                
                                if (worldY >= topY - (TileSizeInWorld * 0.4f)) 
                                {
                                    currentFloorY = topY;
                                    foundFloor = true;
                                    isSlope = false; // This is a flat square block!
                                }
                            }

                            if (foundFloor)
                            {
                                if (worldY >= currentFloorY - (TileSizeInWorld * 1.5f) && worldY <= currentFloorY + (TileSizeInWorld * 1.5f))
                                {
                                    // If a higher floor is found, remember it together with its type (slope or not)
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
            // 1. Compute the camera view bounds
            float halfW = camera.ViewWidth / 2f;
            float halfH = camera.ViewHeight / 2f;

            // Count cells, grabbing a couple of spare ones at the edges
            int minX = (int)MathF.Floor((camera.Position.X - halfW) / TileSizeInWorld) - 2;
            int maxX = (int)MathF.Ceiling((camera.Position.X + halfW) / TileSizeInWorld) + 2;
            int minY = (int)MathF.Floor((camera.Position.Y - halfH) / TileSizeInWorld) - 2;
            int maxY = (int)MathF.Ceiling((camera.Position.Y + halfH) / TileSizeInWorld) + 2;

            foreach (var layer in Layers)
            {
                if (!layer.HasCollision) continue;

                // 2. Iterate ONLY over the visible screen area
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var gridPos = new Vector2D<int>(x, y);

                        // If there is a tile with collision configured, draw its hitbox
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

        // Draw a filled triangle as a staircase
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
                    // Find the local X (0 to 1)
                    float localX = (i + 0.5f) / steps;
                    float effX = fX ? 1.0f - localX : localX;
                    
                    // Height of this column
                    float h = effX * size.Y;
                    
                    float cx = posX - size.X/2f + i * stepW + stepW/2f;
                    
                    // For a ceiling (FlipY) the column hangs from top to bottom
                    float cy = fY ? (posY + size.Y/2f - h/2f) : (posY - size.Y/2f + h/2f);
                    
                    renderer.DrawCollisionBox(new Vector2D<float>(cx, cy), new Vector2D<float>(stepW, h), camera, color);
                }
            }
        }
    }
}