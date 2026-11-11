using Silk.NET.Maths;

namespace ef2dg
{
    public class CameraFollowComponent : Component
    {
        public float SmoothSpeed { get; set; } = 5.0f;
        public Vector2D<float> Offset { get; set; } = new Vector2D<float>(0, 0);

        // Перехватываем камеру из рендера и запоминаем её для всей игры
        private static Camera? _globalCamera;

        public override void FixedUpdate(float deltaTime)
        {
            if (_globalCamera == null) return;

            float targetX = Position.X + Offset.X;
            float targetY = Position.Y + Offset.Y;
            
            float lerpFactor = SmoothSpeed * deltaTime;
            
            _globalCamera.Position = new Vector3D<float>(
                _globalCamera.Position.X + (targetX - _globalCamera.Position.X) * lerpFactor,
                _globalCamera.Position.Y + (targetY - _globalCamera.Position.Y) * lerpFactor,
                _globalCamera.Position.Z
            );
        }

        public override void Draw(SpriteRenderer renderer, Camera camera)
        {
            // Запоминаем камеру один раз при первой отрисовке
            if (_globalCamera == null) _globalCamera = camera;
        }
    }
}