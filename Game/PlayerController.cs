using Silk.NET.Maths;
using System;

namespace ef2dg
{
    public class PlayerController : Component
    {
        public float MoveSpeed { get; set; } = 4.0f;
        public float JumpForce { get; set; } = 6.0f;

        public bool IsAttacking => Sprite != null && Sprite.CurrentAnimationName == "Attack";

        private bool _hasDealtDamageThisAttack = false; // Защита от мульти-урона за один взмах

        public override void Update(float deltaTime)
        {
            if (Sprite == null || Physics == null) return;

            // 1. АТАКА
            if (IsAttacking)
            {
                if (Sprite.CurrentFrame == Sprite.ActionTriggerFrame && !_hasDealtDamageThisAttack)
                {
                    PerformAttack();
                    _hasDealtDamageThisAttack = true;
                }
                
                Physics.Velocity = new Vector2D<float>(0, Physics.GravityScale == 0 ? 0 : Physics.Velocity.Y);
                return; 
            }
            else
            {
                _hasDealtDamageThisAttack = false; 
            }

            if (InputManager.IsActionPressed("Attack"))
            {
                Sprite.SetTrigger("Attack"); 
                return; 
            }

            // 2. ДВИЖЕНИЕ
            float moveX = 0;
            float moveY = 0;

            if (InputManager.IsActionPressed("Left")) { moveX -= MoveSpeed; Sprite.FlipX = true; }
            if (InputManager.IsActionPressed("Right")) { moveX += MoveSpeed; Sprite.FlipX = false; }

            // ЛОГИКА ЗАВИСИТ ОТ ГРАВИТАЦИИ!
            if (Physics.GravityScale > 0)
            {
                // Режим ПЛАТФОРМЕРА (прыжок активен, Y управляется гравитацией)
                if (InputManager.IsActionPressed("Jump") && Physics.IsGrounded)
                {
                    Physics.Velocity = new Vector2D<float>(Physics.Velocity.X, JumpForce);
                    Physics.IsGrounded = false;
                }
                Physics.Velocity = new Vector2D<float>(moveX, Physics.Velocity.Y);
            }
            else
            {
                // Режим ВИД СВЕРХУ (гравитации нет, свободно ходим по Y)
                if (InputManager.IsActionPressed("Up")) { moveY += MoveSpeed; }
                if (InputManager.IsActionPressed("Down")) { moveY -= MoveSpeed; }
                
                Physics.Velocity = new Vector2D<float>(moveX, moveY);
            }

            // 3. ПЕРЕДАЕМ ДАННЫЕ В АНИМАТОР
            // Если играем с видом сверху, скорость учитывает обе оси (выбираем максимальную для анимации)
            float currentSpeed = Physics.GravityScale > 0 ? MathF.Abs(moveX) : MathF.Max(MathF.Abs(moveX), MathF.Abs(moveY));
            
            Sprite.SetFloat("Speed", currentSpeed);
            Sprite.SetBool("IsGrounded", Physics.IsGrounded);

            // 4. ПЕРЕКЛЮЧЕНИЕ СТЕЙТОВ
            if (!IsAttacking)
            {
                if (Physics.GravityScale > 0)
                {
                    // Анимации платформера
                    if (!Physics.IsGrounded)
                        Sprite.Play(Physics.Velocity.Y > 0 ? "Jump" : "Fall");
                    else
                        Sprite.Play(currentSpeed > 0.1f ? "Run" : "Idle");
                }
                else
                {
                    // Анимации вида сверху (здесь нет прыжков и падений)
                    Sprite.Play(currentSpeed > 0.1f ? "Run" : "Idle");
                }
            }
        }

        // ==========================================
        // ЛОГИКА УДАРА (Перенесена из Сцены в Контроллер!)
        // ==========================================
        private void PerformAttack()
        {
            if (Owner.ParentScene == null || Sprite == null) return;

            float directionOffset = Sprite.FlipX ? -Sprite.ActionHitboxOffset.X : Sprite.ActionHitboxOffset.X;
            float aLeft = Position.X + directionOffset - Sprite.ActionHitboxSize.X / 2f;
            float aRight = Position.X + directionOffset + Sprite.ActionHitboxSize.X / 2f;
            float aTop = Position.Y + Sprite.ActionHitboxOffset.Y + Sprite.ActionHitboxSize.Y / 2f;
            float aBottom = Position.Y + Sprite.ActionHitboxOffset.Y - Sprite.ActionHitboxSize.Y / 2f;

            for (int i = Owner.ParentScene.Entities.Count - 1; i >= 0; i--)
            {
                var entity = Owner.ParentScene.Entities[i];
                if (entity == Owner) continue; 

                var ePhys = entity.GetComponent<PhysicsBody>();
                if (ePhys == null) continue;

                float eLeft = entity.Position.X + ePhys.HitboxOffset.X - ePhys.HitboxSize.X / 2f;
                float eRight = entity.Position.X + ePhys.HitboxOffset.X + ePhys.HitboxSize.X / 2f;
                float eTop = entity.Position.Y + ePhys.HitboxOffset.Y + ePhys.HitboxSize.Y / 2f;
                float eBottom = entity.Position.Y + ePhys.HitboxOffset.Y - ePhys.HitboxSize.Y / 2f;

                if (aRight > eLeft && aLeft < eRight && aTop > eBottom && aBottom < eTop)
                {
                    Owner.ParentScene.Entities.RemoveAt(i); 
                    Console.WriteLine("Enemy killed by PlayerController!");
                }
            }
        }
    }
}