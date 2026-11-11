using System;
using Silk.NET.OpenGL;

namespace ef2dg
{
    // <TDataType> means the buffer can hold anything: float, int, byte...
    // where TDataType : unmanaged — constraint: only simple data types (not classes)
    public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged
    {
        private uint _handle; // buffer number (ID)
        private BufferTargetARB _bufferType; // Buffer type (regular or index)
        private GL _gl; // Reference to OpenGL

        // Constructor: creates the buffer and uploads the data right away
        public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
        {
            _gl = gl;
            _bufferType = bufferType;

            // 1. Generate an empty buffer and get its ID (Handle)
            _handle = _gl.GenBuffer();

            // 2. Bind it (tell OpenGL which buffer we are working with)
            Bind();

            // 3. Upload the data
            // (nuint) — cast for the size
            // sizeof(TDataType) — size of one element in bytes
            fixed (void* d = data)
            {
                _gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        // Activates this buffer before drawing
        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle);
        }

        // Deletes the buffer from GPU memory
        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}