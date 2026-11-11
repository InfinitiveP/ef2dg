using System;
using Silk.NET.OpenGL;

namespace ef2dg
{
    public class VertexArrayObject<TDataType> : IDisposable where TDataType : unmanaged
    {
        private uint _handle;
        private GL _gl;

        public VertexArrayObject(GL gl, BufferObject<TDataType> vbo)
        {
            _gl = gl;
            _handle = _gl.GenVertexArray();
            Bind();
            vbo.Bind();

            unsafe
            {
                // Compute the size of one element (float)
                uint sizeType = (uint)sizeof(TDataType);
                
                // Stride = 5 elements (3 position + 2 texture) * type size
                // For a float array, 5 * 4 bytes = 20-byte stride
                uint stride = 5 * sizeType; 

                // --- Attribute 0: position (X, Y, Z) ---
                // 3 components, offset = 0
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);

                // --- Attribute 1: texture (U, V) ---
                // 2 components, offset = 3 elements * type size (skip X, Y, Z)
                _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeType));
                _gl.EnableVertexAttribArray(1);
            }
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }
    }
}