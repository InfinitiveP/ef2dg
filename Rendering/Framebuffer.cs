using Silk.NET.OpenGL;
using System;

namespace ef2dg
{
    public class Framebuffer : IDisposable
    {
        private GL _gl;
        public uint FboId { get; private set; }
        public uint TextureId { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        // HDR-буфер хранит значения больше 1.0 (нужно для яркого света и bloom)
        private readonly bool _hdr;
        // Линейная фильтрация нужна для мягкого размытия bloom, игровые кадры остаются пиксельными
        private readonly bool _linear;

        public Framebuffer(GL gl, int width, int height, bool hdr = false, bool linear = false)
        {
            _gl = gl;
            Width = width;
            Height = height;
            _hdr = hdr;
            _linear = linear;
            Generate();
        }

        private void Generate()
        {
            // 1. Создаем сам буфер
            FboId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);

            // 2. Создаем текстуру, куда будут записываться пиксели игры
            TextureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, TextureId);
            
            unsafe
            {
                // Выделяем память под картинку
                if (_hdr)
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.Float, null);
                else
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)Width, (uint)Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            }

            if (_linear)
            {
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                // Размытие читает пиксели за краем кадра — повторяем крайний пиксель вместо противоположного края
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }
            else
            {
                // Фильтрация пиксельная, чтобы не мылило
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            }

            // 3. Прикрепляем текстуру к буферу
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TextureId, 0);

            if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                Console.WriteLine("ERROR: Framebuffer is not complete!");
            }

            // Отвязываем, чтобы случайно не рисовать туда лишнего
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // Если окно в ImGui растянули - пересоздаем холст нужного размера
        public void Resize(int width, int height)
        {
            if (Width == width && Height == height) return;
            Width = width;
            Height = height;
            Dispose();
            Generate();
        }

        // Включаем запись на холст
        public void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);
            _gl.Viewport(0, 0, (uint)Width, (uint)Height);
        }

        // Выключаем запись на холст (возвращаем на главный экран)
        public void Unbind(int windowWidth, int windowHeight)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.Viewport(0, 0, (uint)windowWidth, (uint)windowHeight);
        }

        public void CopyFrom(Framebuffer source)
        {
            if (Width != source.Width || Height != source.Height)
            {
                Resize(source.Width, source.Height);
            }

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, source.FboId);
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FboId);
            _gl.BlitFramebuffer(
                0, 0, source.Width, source.Height,
                0, 0, Width, Height,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            _gl.DeleteFramebuffer(FboId);
            _gl.DeleteTexture(TextureId);
        }
    }
}
