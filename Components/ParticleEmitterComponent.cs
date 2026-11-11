using System;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace ef2dg
{
    public struct Particle
    {
        public Vector2D<float> Position;
        public Vector2D<float> Velocity;
        public float Lifetime;
        public float MaxLifetime;
        public float StartSize;
        public float EndSize;
        public Vector4D<float> StartColor;
        public Vector4D<float> EndColor;
        public float Rotation;
        public float RotationalSpeed;
        public bool IsAlive => Lifetime > 0;
    }

    public class ParticleEmitterComponent : Component
    {
        // Текстура частицы (если пусто, берется заглушка)
        public string TexturePath { get; set; } = "";

        // Настройки эмиттера
        public int MaxParticles { get; set; } = 100;
        public float EmitRate { get; set; } = 20.0f; // Частиц в секунду
        public bool IsLooping { get; set; } = true;
        
        // Физика частицы
        public Vector2D<float> VelocityBase { get; set; } = new Vector2D<float>(0.0f, 1.0f);
        public Vector2D<float> VelocityRandomRange { get; set; } = new Vector2D<float>(0.5f, 0.3f);
        public Vector2D<float> SpawnOffsetRange { get; set; } = new Vector2D<float>(0.2f, 0.0f);
        public float Gravity { get; set; } = 0.0f;

        // Жизнь и размер
        public float ParticleLifetime { get; set; } = 1.5f;
        public float StartSize { get; set; } = 0.1f;
        public float EndSize { get; set; } = 0.02f;

        // Цвет (R, G, B, A) в начале и в конце
        public Vector4D<float> StartColor { get; set; } = new Vector4D<float>(1.0f, 0.6f, 0.1f, 1.0f); // Оранжевый
        public Vector4D<float> EndColor { get; set; } = new Vector4D<float>(0.2f, 0.0f, 0.0f, 0.0f);   // Прозрачный красный

        private Particle[] _particles = Array.Empty<Particle>();
        private float _emitTimer = 0.0f;
        private Random _random = new Random();
        private Texture? _texture;

        public override void Start()
        {
            _particles = new Particle[MaxParticles];
        }

        public override void Update(float deltaTime)
        {
            if (_particles.Length != MaxParticles)
            {
                _particles = new Particle[MaxParticles];
            }

            // Спавн новых частиц
            if (IsLooping && EmitRate > 0)
            {
                _emitTimer += deltaTime;
                float timeBetweenSpawns = 1.0f / EmitRate;

                while (_emitTimer >= timeBetweenSpawns)
                {
                    EmitSingleParticle();
                    _emitTimer -= timeBetweenSpawns;
                }
            }

            // Обновление живых частиц
            for (int i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].IsAlive) continue;

                _particles[i].Lifetime -= deltaTime;
                _particles[i].Position = new Vector2D<float>(
                    _particles[i].Position.X + _particles[i].Velocity.X * deltaTime,
                    _particles[i].Position.Y + (_particles[i].Velocity.Y - Gravity * deltaTime) * deltaTime
                );
                _particles[i].Rotation += _particles[i].RotationalSpeed * deltaTime;
            }
        }

        private void EmitSingleParticle()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].IsAlive)
                {
                    float offsetX = ((float)_random.NextDouble() * 2f - 1f) * SpawnOffsetRange.X;
                    float offsetY = ((float)_random.NextDouble() * 2f - 1f) * SpawnOffsetRange.Y;

                    float vx = VelocityBase.X + ((float)_random.NextDouble() * 2f - 1f) * VelocityRandomRange.X;
                    float vy = VelocityBase.Y + ((float)_random.NextDouble() * 2f - 1f) * VelocityRandomRange.Y;

                    _particles[i] = new Particle
                    {
                        Position = new Vector2D<float>(Position.X + offsetX, Position.Y + offsetY),
                        Velocity = new Vector2D<float>(vx, vy),
                        Lifetime = ParticleLifetime,
                        MaxLifetime = ParticleLifetime,
                        StartSize = StartSize,
                        EndSize = EndSize,
                        StartColor = StartColor,
                        EndColor = EndColor,
                        Rotation = (float)_random.NextDouble() * 360f,
                        RotationalSpeed = ((float)_random.NextDouble() * 2f - 1f) * 90f
                    };
                    break;
                }
            }
        }

        public override void Draw(SpriteRenderer renderer, Camera camera)
        {
            // Динамически получаем нужную текстуру каждый кадр
            // ResourceManager сам кэширует картинки, так что это безопасно
            Texture currentTexture;
            
            if (string.IsNullOrEmpty(TexturePath))
            {
                currentTexture = Engine.Resources.WhiteTexture; // Используем белый квадрат!
            }
            else
            {
                currentTexture = Engine.Resources.GetTexture(TexturePath);
            }

            Vector2D<float> uvOffset = new Vector2D<float>(0, 0);
            Vector2D<float> uvScale = new Vector2D<float>(1, 1);

            for (int i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].IsAlive) continue;

                float t = 1.0f - (_particles[i].Lifetime / _particles[i].MaxLifetime); 

                float currentSize = _particles[i].StartSize + (_particles[i].EndSize - _particles[i].StartSize) * t;

                Vector4D<float> currentColor = new Vector4D<float>(
                    _particles[i].StartColor.X + (_particles[i].EndColor.X - _particles[i].StartColor.X) * t,
                    _particles[i].StartColor.Y + (_particles[i].EndColor.Y - _particles[i].StartColor.Y) * t,
                    _particles[i].StartColor.Z + (_particles[i].EndColor.Z - _particles[i].StartColor.Z) * t,
                    _particles[i].StartColor.W + (_particles[i].EndColor.W - _particles[i].StartColor.W) * t
                );

                // ИСПОЛЬЗУЕМ НОВОЕ ИМЯ МЕТОДА
                renderer.DrawColored(
                    currentTexture,
                    _particles[i].Position,
                    new Vector2D<float>(currentSize, currentSize),
                    _particles[i].Rotation,
                    camera,
                    uvOffset,
                    uvScale,
                    false,
                    currentColor
                );
            }
        }
    }
}