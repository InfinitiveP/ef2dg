using Silk.NET.Maths;
using System;

namespace ef2dg
{
    public class ButtonComponent : Component
    {
        public string Label { get; set; } = "Button";
        public Vector2D<float> Size { get; set; } = new Vector2D<float>(150, 40);
        public string TargetSceneName { get; set; } = "";
        public bool IsOverlay { get; set; } = false;
        public bool IsHovered { get; set; } = false;
        public TransitionSettings Transition { get; set; } = new TransitionSettings();

        public override void Start() { }

        public override void Update(float deltaTime)
        {
            if (Sprite != null)
            {
                // Pass the state as a float (1.0f or 0.0f)
                // so the numeric Threshold field in the Inspector can be used
                Sprite.SetFloat("IsHovered", IsHovered ? 1.0f : 0.0f);
            }
        }

        public override void Draw(SpriteRenderer renderer, Camera camera) { }
    }
}
