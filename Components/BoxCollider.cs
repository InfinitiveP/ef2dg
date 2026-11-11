using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class BoxCollider : Component
    {
        public Vector2D<float> Size { get; set; } = new Vector2D<float>(1f, 1f);
        public Vector2D<float> Offset { get; set; } = new Vector2D<float>(0f, 0f);
        
        // IMPORTANT FOR THE FUTURE EVENT SYSTEM:
        // If true  -> the player is not blocked and passes through (quest zone, teleport)
        // If false -> intended as a solid static obstacle (tree, rock)
        public bool IsTrigger { get; set; } = false;

        public override void Start() 
        { 
        }

        public override void Update(float deltaTime) 
        { 
        }

        public override void FixedUpdate(float fixedDeltaTime) 
        { 
        }

        public override void Draw(SpriteRenderer renderer, Camera camera) 
        { 
            // Regular (in-game) drawing is not needed here; colliders are invisible
        }

        // Drawing method for debugging and the editor
        public void DrawDebug(SpriteRenderer renderer, Camera camera, Vector4D<float> color)
        {
            float posX = Owner.Position.X + Offset.X;
            float posY = Owner.Position.Y + Offset.Y;
            
            // SpriteRenderer provides DrawCollisionBox for this:
            renderer.DrawCollisionBox(new Vector2D<float>(posX, posY), Size, camera, color);
        }
    }
}