using System;
using Silk.NET.OpenGL;

namespace ef2dg
{
    // <TDataType> означает, что буфер может хранить что угодно: float, int, byte...
    // where TDataType : unmanaged — ограничение: только простые типы данных (не классы)
    public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged
    {
        private uint _handle; // номер буфера (ID)
        private BufferTargetARB _bufferType; // Тип буфера (обычный или индексный)
        private GL _gl; // Ссылка на OpenGL

        // Конструктор, создает буфер и сразу заливает туда данные
        public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
        {
            _gl = gl;
            _bufferType = bufferType;

            // 1. Генерируем пустой буфер и получаем его ID (Handle)
            _handle = _gl.GenBuffer();

            // 2. Биндим его (указываем OpenGL с каким буфером работаем)
            Bind();

            // 3. Заливаем данные
            // (nuint) — приведение типа для размера
            // sizeof(TDataType) — размер одного элемента в байтах
            fixed (void* d = data)
            {
                _gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        // Метод, чтобы активировать этот буфер перед рисованием
        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle);
        }

        // Метод для удаления буфера из памяти видеокарты
        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}