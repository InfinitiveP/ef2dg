using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class EnemyAIComponent : Component
    {
        public float SightRadius { get; set; } = 5.0f;   
        public float AttackRadius { get; set; } = 0.8f;  
        public float MoveSpeed { get; set; } = 2.5f;     
        public float AttackCooldown { get; set; } = 1.5f;
        
        public bool IsAttacking => Sprite != null && Sprite.CurrentAnimationName == "Attack";

        private GameObject? _targetPlayer;
        private float _cooldownTimer = 0f;

        public override void Start()
        {
            if (Sprite != null) Sprite.SetFloat("Speed", 0f);
        }

        public override void Update(float deltaTime)
        {
            if (Sprite == null || Physics == null) return;

            // Find the player
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

            if (_targetPlayer == null) return;

            if (_cooldownTimer > 0) _cooldownTimer -= deltaTime;

            // Block movement while attacking
            if (IsAttacking)
            {
                // For the future: a check for the bot's hit frame can be added here!
                // if (Sprite.CurrentFrame == Sprite.ActionTriggerFrame) { /* deal damage to the player */ }
                
                Physics.Velocity = new Vector2D<float>(0, Physics.Velocity.Y);
                return; 
            }

            float dx = _targetPlayer.Position.X - Position.X;
            float distance = MathF.Abs(dx);

            if (distance <= AttackRadius)
            {
                Physics.Velocity = new Vector2D<float>(0, Physics.Velocity.Y);
                Sprite.SetFloat("Speed", 0f);

                if (_cooldownTimer <= 0)
                {
                    Sprite.SetTrigger("Attack"); 
                    _cooldownTimer = AttackCooldown; 
                }
            }
            else if (distance <= SightRadius)
            {
                float dirX = MathF.Sign(dx);
                Physics.Velocity = new Vector2D<float>(dirX * MoveSpeed, Physics.Velocity.Y);
                Sprite.SetFloat("Speed", MoveSpeed);
                Sprite.FlipX = (dirX < 0);
            }
            else
            {
                Physics.Velocity = new Vector2D<float>(0, Physics.Velocity.Y);
                Sprite.SetFloat("Speed", 0f);
            }
        }
    }
}