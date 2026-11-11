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
            // Пытаемся зарегистрировать параметр при старте (если спрайт уже добавлен)
            var sprite = Owner.GetComponent<SpriteComponent>();
            if (sprite != null) sprite.SetFloat("Speed", 0f);
        }

        public override void Update(float deltaTime)
        {
            var sprite = Owner.GetComponent<SpriteComponent>();
            var pb = Owner.GetComponent<PhysicsBody>();

            // 1. ЛЕНИВЫЙ ПОИСК ИГРОКА: Если его еще нет, ищем его каждый кадр
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

            // 2. ЗАЩИТА: Если игрока все еще нет на сцене, мы стоим на месте, 
            // но обязательно отправляем параметр 0 в Аниматор, чтобы он появился в UI!
            if (_targetPlayer == null)
            {
                if (pb != null) pb.Velocity = new Silk.NET.Maths.Vector2D<float>(0, pb.Velocity.Y);
                if (sprite != null) sprite.SetFloat("Speed", 0f);
                return;
            }

            // 3. ОСНОВНАЯ ЛОГИКА ДВИЖЕНИЯ
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