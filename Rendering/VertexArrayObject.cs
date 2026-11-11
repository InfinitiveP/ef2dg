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
                // Рассчитываем размер одного элемента (float)
                uint sizeType = (uint)sizeof(TDataType);
                
                // Шаг (Stride) = 5 элементов (3 позиция + 2 текстура) * размер типа
                // Если массив float, то 5 * 4 байта = 20 байт шаг
                uint stride = 5 * sizeType; 

                // --- Атрибут 0: Позиция (X, Y, Z) ---
                // 3 компонента, смещение (offset) = 0
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);

                // --- Атрибут 1: Текстура (U, V) ---
                // 2 компонента, смещение = 3 элемента * размер типа (пропускаем X, Y, Z)
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