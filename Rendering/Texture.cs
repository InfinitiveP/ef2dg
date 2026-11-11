using System;
using Silk.NET.OpenGL;
using StbImageSharp;
using System.IO;

namespace ef2dg
{
    public class Texture : IDisposable
    {
        private uint _handle;
        private GL _gl;
        
        // Flag that prevents deleting a texture owned by someone else
        private bool _ownsHandle = true;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public uint Handle => _handle;

        // 1. Constructor for loading from a file
        public unsafe Texture(GL gl, string path)
        {
            _gl = gl;
            _handle = _gl.GenTexture();
            Bind();

            using (FileStream stream = File.OpenRead(path))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                Width = image.Width;
                Height = image.Height;

                fixed (byte* ptr = image.Data)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            }
            SetParameters();
        }

        // 2. Constructor for creating a placeholder texture from bytes in memory
        public unsafe Texture(GL gl, int width, int height, byte[] data)
        {
            _gl = gl;
            Width = width;
            Height = height;
            _handle = _gl.GenTexture();
            Bind();

            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
            SetParameters();
        }

        // 3. Wrapper for textures already created by a Framebuffer
        public Texture(GL gl, uint handle, int width, int height)
        {
            _gl = gl;
            _handle = handle; // Take the ready ID from the Framebuffer
            Width = width;
            Height = height;
            
            // IMPORTANT: this texture is not ours; the Framebuffer deletes it when the time comes
            _ownsHandle = false; 
        }

        private void SetParameters()
        {
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            // Delete the texture only if we created it ourselves
            if (_ownsHandle) 
            {
                _gl.DeleteTexture(_handle);
            }
        }
    }
}