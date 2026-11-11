using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class LightComponent : Component
    {
        public bool Enabled { get; set; } = true;

        // Light colour (for example, yellow-orange for a torch)
        public Vector4D<float> Color { get; set; } = new Vector4D<float>(1.0f, 0.8f, 0.5f, 1.0f);

        // Brightness. Values above 1 overexpose the surface and trigger bloom
        public float Intensity { get; set; } = 1.0f;

        // Glow radius in game coordinates
        public float Radius { get; set; } = 4.0f;

        // Offset of the source from the object position (for example, a flame above a torch)
        public Vector2D<float> Offset { get; set; } = new Vector2D<float>(0f, 0f);

        // Edge falloff sharpness: < 1 gives an even spot, > 1 concentrates light in the centre
        public float Falloff { get; set; } = 1.0f;

        // Visible source core (orb, flame), drawn on top of the lit scene
        public float Glow { get; set; } = 0f;
        // Core size as a fraction of the radius
        public float GlowSize { get; set; } = 0.2f;

        // Uneven fire flicker
        public float FlickerAmount { get; set; } = 0f;
        public float FlickerSpeed { get; set; } = 8f;

        // Smooth pulsing of magic and crystals
        public float PulseAmount { get; set; } = 0f;
        public float PulseSpeed { get; set; } = 1f;

        // Random phase so neighbouring torches do not flicker in sync
        private readonly float _seed = Random.Shared.NextSingle() * 1000f;

        public Vector2D<float> WorldPosition => Position + Offset;

        // Current parameters with flicker and pulsing applied
        public void Evaluate(float time, out float intensity, out float radius, out float glow)
        {
            float factor = 1f;
            if (FlickerAmount > 0f)
            {
                // A sum of incommensurable sines gives a non-repeating "living" flicker
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
            // The radius "breathes" less than the brightness, otherwise the light edge visibly jumps
            radius = Radius * (0.85f + 0.15f * factor);
            glow = Glow * factor;
        }

        public override void Draw(SpriteRenderer renderer, Camera camera)
        {
            // Left empty! Light is not drawn together with sprites,
            // but in a separate LightingRenderer pass
        }
    }
}
