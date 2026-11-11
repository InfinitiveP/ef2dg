using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class LightComponent : Component
    {
        public bool Enabled { get; set; } = true;

        // Цвет света (например, желто-оранжевый для факела)
        public Vector4D<float> Color { get; set; } = new Vector4D<float>(1.0f, 0.8f, 0.5f, 1.0f);

        // Яркость. Значения больше 1 пересвечивают поверхность и включают bloom
        public float Intensity { get; set; } = 1.0f;

        // Радиус свечения в игровых координатах
        public float Radius { get; set; } = 4.0f;

        // Смещение источника от позиции объекта (например, пламя над факелом)
        public Vector2D<float> Offset { get; set; } = new Vector2D<float>(0f, 0f);

        // Резкость спада к краю: < 1 — ровное пятно, > 1 — свет собран у центра
        public float Falloff { get; set; } = 1.0f;

        // Видимое ядро источника (сфера, пламя), рисуется поверх освещённой сцены
        public float Glow { get; set; } = 0f;
        // Размер ядра как доля радиуса
        public float GlowSize { get; set; } = 0.2f;

        // Неровное мерцание огня
        public float FlickerAmount { get; set; } = 0f;
        public float FlickerSpeed { get; set; } = 8f;

        // Плавная пульсация магии и кристаллов
        public float PulseAmount { get; set; } = 0f;
        public float PulseSpeed { get; set; } = 1f;

        // Случайная фаза, чтобы соседние факелы мерцали не синхронно
        private readonly float _seed = Random.Shared.NextSingle() * 1000f;

        public Vector2D<float> WorldPosition => Position + Offset;

        // Текущие параметры с учётом мерцания и пульсации
        public void Evaluate(float time, out float intensity, out float radius, out float glow)
        {
            float factor = 1f;
            if (FlickerAmount > 0f)
            {
                // Сумма несоизмеримых синусоид даёт неповторяющееся «живое» мерцание
                float t = time * FlickerSpeed + _seed;
                float noise = MathF.Sin(t) * 0.5f + MathF.Sin(t * 2.31f + 1.7f) * 0.3f + MathF.Sin(t * 5.13f + 4.2f) * 0.2f;
                factor *= 1f + noise * FlickerAmount;
            }
            if (PulseAmount > 0f)
            {
                factor *= 1f + MathF.Sin(time * PulseSpeed * MathF.Tau + _seed) * PulseAmount;
            }
            factor = MathF.Max(0f, factor);

            intensity = Intensity * factor;
            // Радиус «дышит» слабее яркости, иначе граница света заметно прыгает
            radius = Radius * (0.85f + 0.15f * factor);
            glow = Glow * factor;
        }

        public override void Draw(SpriteRenderer renderer, Camera camera)
        {
            // Оставляем пустым! Свет рисуется не вместе со спрайтами,
            // а отдельным проходом LightingRenderer
        }
    }
}
