using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class SmartStoneComponent : Component
    {
        public float TriggerRadius { get; set; } = 3.0f;
        public float MoveSpeed { get; set; } = 2.0f;
        
        private GameObject? _targetPlayer;

        public override void Start()
        {
            // Try to register the parameter at start (if the sprite is already added)
            var sprite = Owner.GetComponent<SpriteComponent>();
            if (sprite != null) sprite.SetFloat("Speed", 0f);
        }

        public override void Update(float deltaTime)
        {
            var sprite = Owner.GetComponent<SpriteComponent>();
            var pb = Owner.GetComponent<PhysicsBody>();

            // 1. LAZY PLAYER LOOKUP: if there is no player yet, search every frame
            if (_targetPlayer == null && Owner.ParentScene != null)
            {
                foreach (var entity in Owner.ParentScene.Entities)
                {
                    if (entity.GetComponent<PlayerController>() != null)
                    {
                        _targetPlayer = entity;
                        break;
                    }
                }
            }

            // 2. GUARD: if the player is still not in the scene, stay in place,
            // but always send parameter 0 to the animator so it shows up in the UI!
            if (_targetPlayer == null)
            {
                if (pb != null) pb.Velocity = new Silk.NET.Maths.Vector2D<float>(0, pb.Velocity.Y);
                if (sprite != null) sprite.SetFloat("Speed", 0f);
                return;
            }

            // 3. MAIN MOVEMENT LOGIC
            float dx = _targetPlayer.Position.X - Owner.Position.X;
            float distance = MathF.Abs(dx);

            if (distance < TriggerRadius)
            {
                float dirX = MathF.Sign(dx);
                
                if (pb != null) pb.Velocity = new Silk.NET.Maths.Vector2D<float>(-dirX * MoveSpeed, pb.Velocity.Y);

                if (sprite != null) 
                {
                    sprite.SetFloat("Speed", MoveSpeed);
                    sprite.FlipX = (-dirX < 0);
                }
            }
            else
            {
                if (pb != null) pb.Velocity = new Silk.NET.Maths.Vector2D<float>(0, pb.Velocity.Y);
                if (sprite != null) sprite.SetFloat("Speed", 0f);
            }
        }
    }
}