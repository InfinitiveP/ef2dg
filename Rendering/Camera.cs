using System;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ef2dg
{
    public class Camera
    {
        public Vector3D<float> Position { get; set; }
        public Vector3D<float> Front { get; set; } = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        public Vector3D<float> Up { get; set; } = Vector3D<float>.UnitY;

        // Zoom property (1.0 by default)
        public float Zoom { get; set; } = 1.0f;

        private float _width;
        private float _height;
        
        // View width and height are multiplied by Zoom
        public float AspectRatio => _height == 0f ? 1f : _width / _height;
        public float ViewHeight => 2.0f * Zoom;
        public float ViewWidth => ViewHeight * AspectRatio;

        public Camera(float width, float height)
        {
            _width = width;
            _height = height;
            Position = new Vector3D<float>(0.0f, 0.0f, 3.0f);
        }

        public void UpdateSize(float width, float height)
        {
            _width = width;
            _height = height;
        }

        public Matrix4X4<float> GetViewMatrix()
        {
            return Matrix4X4.CreateLookAt(Position, Position + Front, Up);
        }

        public Matrix4X4<float> GetProjectionMatrix()
        {
            float aspectRatio = _width / _height;
            
            // Base size (2.0) multiplied by Zoom
            float orthographicSize = 2.0f * Zoom; 
            
            return Matrix4X4.CreateOrthographic(orthographicSize * aspectRatio, orthographicSize, 0.1f, 100.0f);
        }
    }
}
