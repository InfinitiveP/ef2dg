using Silk.NET.Maths;

namespace ef2dg
{
    public enum TransitionKind
    {
        Instant,
        Fade,
        CrossFade,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown
    }

    public class TransitionSettings
    {
        public TransitionKind Kind { get; set; } = TransitionKind.Instant;
        public float Duration { get; set; } = 0.5f;
        public Vector4D<float> Color { get; set; } = new Vector4D<float>(0f, 0f, 0f, 1f);

        public TransitionSettings Clone()
        {
            return new TransitionSettings
            {
                Kind = Kind,
                Duration = Duration,
                Color = Color
            };
        }
    }
}
