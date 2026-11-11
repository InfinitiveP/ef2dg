using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class BoxCollider : Component
    {
        public Vector2D<float> Size { get; set; } = new Vector2D<float>(1f, 1f);
        public Vector2D<float> Offset { get; set; } = new Vector2D<float>(0f, 0f);
        
        // ВАЖНО ДЛЯ БУДУЩЕЙ СИСТЕМЫ ИВЕНТОВ:
        // Если true  -> Игрок не упирается, проходит насквозь (зона квеста, телепорт)
        // Если false -> Работает как твердое статическое препятствие (дерево, камень)
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
            // Обычная отрисовка (игровая) здесь не нужна, коллайдеры невидимы
        }

        // Метод отрисовки для дебага и редактора
        public void DrawDebug(SpriteRenderer renderer, Camera camera, Vector4D<float> color)
        {
            float posX = Owner.Position.X + Offset.X;
            float posY = Owner.Position.Y + Offset.Y;
            
            // Если у тебя в SpriteRenderer метод называется DrawCollisionBox, используем его:
            renderer.DrawCollisionBox(new Vector2D<float>(posX, posY), Size, camera, color);
        }
    }
}