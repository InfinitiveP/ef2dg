using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.OpenGL;

namespace ef2dg
{
    public class ResourceManager : IDisposable
    {
        private GL _gl;
        private Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        
        private Texture _missingTexture; 
        
        // НОВОЕ: Открываем доступ для Tilemap, чтобы он знал, что это заглушка
        public Texture MissingTexture => _missingTexture;
        public Texture WhiteTexture { get; private set; }

        public ResourceManager(GL gl)
        {
            _gl = gl;
            byte[] missingData = new byte[] {
                255, 0, 255, 255,   0, 0, 0, 255,
                0, 0, 0, 255,       255, 0, 255, 255
            };
            _missingTexture = new Texture(_gl, 2, 2, missingData);

            byte[] whiteData = new byte[] { 255, 255, 255, 255 }; 
            WhiteTexture = new Texture(_gl, 1, 1, whiteData);
        }

        public Texture GetTexture(string path)
        {
            if (_textures.ContainsKey(path)) return _textures[path]; // Flyweight: O(1) кеш

            if (!File.Exists(path)) 
            {
                Console.WriteLine($"[ResourceManager] ERROR: File not found {path}. Using fallback texture.");
                _textures.Add(path, _missingTexture); // кешуємо заглушку за цим ключем
                return _missingTexture;
            }

            Console.WriteLine($"[ResourceManager] Loading new texture into memory: {path}");
            Texture newTexture = new Texture(_gl, path);
            _textures.Add(path, newTexture);
            return newTexture;
        }

        public void Dispose()
        {
            foreach (var texture in _textures.Values) 
            {
                // Не удаляем заглушку в цикле, так как на нее могут ссылаться много путей
                if (texture != _missingTexture) texture.Dispose();
            }
            _textures.Clear();
            _missingTexture.Dispose();
            WhiteTexture.Dispose();
        }
    }
}