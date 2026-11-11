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

        // An HDR buffer stores values above 1.0 (needed for bright light and bloom)
        private readonly bool _hdr;
        // Linear filtering is needed for soft bloom blur; game frames stay pixelated
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
            // 1. Create the buffer itself
            FboId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);

            // 2. Create the texture the game pixels are written to
            TextureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, TextureId);
            
            unsafe
            {
                // Allocate memory for the image
                if (_hdr)
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.Float, null);
                else
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)Width, (uint)Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            }

            if (_linear)
            {
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                // The blur reads pixels beyond the frame edge — repeat the edge pixel instead of wrapping to the opposite edge
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }
            else
            {
                // Nearest filtering so the image is not blurred
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            }

            // 3. Attach the texture to the buffer
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TextureId, 0);

            if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                Console.WriteLine("ERROR: Framebuffer is not complete!");
            }

            // Unbind so nothing is drawn there by accident
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // If the ImGui window was resized, recreate the canvas at the new size
        public void Resize(int width, int height)
        {
            if (Width == width && Height == height) return;
            Width = width;
            Height = height;
            Dispose();
            Generate();
        }

        // Enable rendering into the canvas
        public void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);
            _gl.Viewport(0, 0, (uint)Width, (uint)Height);
        }

        // Disable rendering into the canvas (back to the main screen)
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
