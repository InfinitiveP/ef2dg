using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ef2dg
{
    // 2D-освещение сцены: карта света (HDR), видимое свечение источников, bloom и цветокоррекция.
    // Берёт уже нарисованный мир из target и записывает освещённый результат обратно в него.
    public sealed class LightingRenderer : IDisposable
    {
        private const int MaxQuads = 1024;
        private const int FloatsPerVertex = 9; // XY, локальные координаты (-1..1), RGBA, falloff
        private const int BloomDownscale = 4;
        private const int BlurIterations = 2;
        private const float GlowFalloff = 2f;

        private readonly GL _gl;
        private readonly Shader _lightShader;
        private readonly Shader _compositeShader;
        private readonly Shader _bloomExtractShader;
        private readonly Shader _blurShader;
        private readonly Shader _finalShader;

        private readonly uint _quadVao;
        private readonly uint _quadVbo;
        private readonly uint _quadEbo;
        private readonly float[] _quadVertices = new float[MaxQuads * 4 * FloatsPerVertex];
        private int _quadVertexFloatCount;
        private int _quadCount;

        // Пустой VAO: полноэкранный треугольник строится в fullscreen.vert из gl_VertexID
        private readonly uint _fullscreenVao;

        private readonly Framebuffer _lightMap;
        private readonly Framebuffer _litScene;
        private readonly Framebuffer _bloomA;
        private readonly Framebuffer _bloomB;

        private readonly record struct GlowQuad(Vector2D<float> Center, float Radius, Vector3 Color);
        private readonly List<GlowQuad> _glowQuads = new();

        public LightingRenderer(GL gl)
        {
            _gl = gl;

            _lightShader = new Shader(_gl, "Shaders/light.vert", "Shaders/light.frag");
            _compositeShader = new Shader(_gl, "Shaders/fullscreen.vert", "Shaders/light_composite.frag");
            _bloomExtractShader = new Shader(_gl, "Shaders/fullscreen.vert", "Shaders/bloom_extract.frag");
            _blurShader = new Shader(_gl, "Shaders/fullscreen.vert", "Shaders/bloom_blur.frag");
            _finalShader = new Shader(_gl, "Shaders/fullscreen.vert", "Shaders/post_final.frag");

            _compositeShader.Use();
            _compositeShader.SetUniform("uScene", 0);
            _compositeShader.SetUniform("uLightMap", 1);
            _bloomExtractShader.Use();
            _bloomExtractShader.SetUniform("uTexture", 0);
            _blurShader.Use();
            _blurShader.SetUniform("uTexture", 0);
            _finalShader.Use();
            _finalShader.SetUniform("uScene", 0);
            _finalShader.SetUniform("uBloom", 1);

            _quadVao = _gl.GenVertexArray();
            _quadVbo = _gl.GenBuffer();
            _quadEbo = _gl.GenBuffer();

            var indices = new uint[MaxQuads * 6];
            for (int quad = 0; quad < MaxQuads; quad++)
            {
                uint vertex = (uint)(quad * 4);
                int index = quad * 6;
                indices[index] = vertex;
                indices[index + 1] = vertex + 1;
                indices[index + 2] = vertex + 2;
                indices[index + 3] = vertex + 2;
                indices[index + 4] = vertex + 3;
                indices[index + 5] = vertex;
            }

            _gl.BindVertexArray(_quadVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _quadEbo);
            unsafe
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(_quadVertices.Length * sizeof(float)), null, BufferUsageARB.DynamicDraw);
                fixed (uint* indexData = indices)
                    _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                        (nuint)(indices.Length * sizeof(uint)), indexData, BufferUsageARB.StaticDraw);

                uint stride = FloatsPerVertex * sizeof(float);
                _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);
                _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
                _gl.EnableVertexAttribArray(1);
                _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(4 * sizeof(float)));
                _gl.EnableVertexAttribArray(2);
                _gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, (void*)(8 * sizeof(float)));
                _gl.EnableVertexAttribArray(3);
            }

            _fullscreenVao = _gl.GenVertexArray();
            _gl.BindVertexArray(0);

            _lightMap = new Framebuffer(_gl, 800, 600, hdr: true);
            _litScene = new Framebuffer(_gl, 800, 600, hdr: true, linear: true);
            _bloomA = new Framebuffer(_gl, 200, 150, hdr: true, linear: true);
            _bloomB = new Framebuffer(_gl, 200, 150, hdr: true, linear: true);
        }

        // Перед вызовом sprite batch должен быть сброшен: проход меняет render target и blend state
        public void Apply(Scene scene, Framebuffer target, Camera camera, float time)
        {
            var settings = scene.Lighting;
            EnsureSize(target.Width, target.Height);

            // 1. Карта освещения: фон = ambient, источники складываются аддитивно и могут быть ярче 1.0
            _lightMap.Bind();
            Vector3 ambient = settings.AmbientColor * settings.AmbientIntensity;
            _gl.ClearColor(ambient.X, ambient.Y, ambient.Z, 1f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);

            _glowQuads.Clear();
            BeginQuads(camera);
            CollectLights(scene, camera, time, settings.LightMultiplier);
            FlushQuads();

            // 2. Освещённая сцена = исходные пиксели * карта освещения
            _litScene.Bind();
            _gl.Disable(EnableCap.Blend);
            _compositeShader.Use();
            BindTexture(0, target.TextureId);
            BindTexture(1, _lightMap.TextureId);
            DrawFullscreen();

            // 3. Видимое ядро источников поверх освещённой сцены
            if (_glowQuads.Count > 0)
            {
                _gl.Enable(EnableCap.Blend);
                _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                BeginQuads(camera);
                foreach (var glow in _glowQuads) AddQuad(glow.Center, glow.Radius, glow.Color, GlowFalloff);
                FlushQuads();
                _gl.Disable(EnableCap.Blend);
            }

            // 4. Bloom: выделяем яркие пиксели в уменьшенный буфер и размываем
            if (settings.BloomStrength > 0f) RenderBloom(settings);

            // 5. Цветокоррекция и виньетка, результат возвращается в исходный буфер
            target.Bind();
            _finalShader.Use();
            _finalShader.SetUniform("uBloomStrength", MathF.Max(0f, settings.BloomStrength));
            _finalShader.SetUniform("uExposure", settings.Exposure);
            _finalShader.SetUniform("uContrast", settings.Contrast);
            _finalShader.SetUniform("uSaturation", settings.Saturation);
            _finalShader.SetUniform("uTint", settings.Tint);
            _finalShader.SetUniform("uVignetteStrength", settings.VignetteStrength);
            _finalShader.SetUniform("uVignetteSize", settings.VignetteSize);
            _finalShader.SetUniform("uVignetteColor", settings.VignetteColor);
            _finalShader.SetUniform("uAspect", target.Height > 0 ? (float)target.Width / target.Height : 1f);
            BindTexture(0, _litScene.TextureId);
            BindTexture(1, _bloomA.TextureId);
            DrawFullscreen();

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindVertexArray(0);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        private void CollectLights(Scene scene, Camera camera, float time, float multiplier)
        {
            float halfWidth = camera.ViewWidth * 0.5f;
            float halfHeight = camera.ViewHeight * 0.5f;

            foreach (var entity in scene.Entities)
            {
                if (entity.IsUI) continue;
                var light = entity.GetComponent<LightComponent>();
                if (light == null || !light.Enabled) continue;

                light.Evaluate(time, out float intensity, out float radius, out float glow);
                if (radius <= 0f) continue;

                // Источники за пределами кадра не влияют на видимые пиксели
                Vector2D<float> center = light.WorldPosition;
                if (MathF.Abs(center.X - camera.Position.X) > halfWidth + radius ||
                    MathF.Abs(center.Y - camera.Position.Y) > halfHeight + radius) continue;

                var color = new Vector3(light.Color.X, light.Color.Y, light.Color.Z);
                if (intensity > 0f)
                    AddQuad(center, radius, color * (intensity * multiplier), MathF.Max(0.1f, light.Falloff));
                if (glow > 0f && light.GlowSize > 0f)
                    _glowQuads.Add(new GlowQuad(center, radius * light.GlowSize, color * (glow * multiplier)));
            }
        }

        private void RenderBloom(SceneLighting settings)
        {
            _bloomA.Bind();
            _bloomExtractShader.Use();
            _bloomExtractShader.SetUniform("uTexelSize", new Vector2D<float>(1f / _litScene.Width, 1f / _litScene.Height));
            _bloomExtractShader.SetUniform("uThreshold", MathF.Max(0f, settings.BloomThreshold));
            BindTexture(0, _litScene.TextureId);
            DrawFullscreen();

            _blurShader.Use();
            float radius = MathF.Max(0.1f, settings.BloomRadius);
            for (int i = 0; i < BlurIterations; i++)
            {
                _bloomB.Bind();
                _blurShader.SetUniform("uDirection", new Vector2D<float>(radius / _bloomA.Width, 0f));
                BindTexture(0, _bloomA.TextureId);
                DrawFullscreen();

                _bloomA.Bind();
                _blurShader.SetUniform("uDirection", new Vector2D<float>(0f, radius / _bloomB.Height));
                BindTexture(0, _bloomB.TextureId);
                DrawFullscreen();
            }
        }

        // Гизмо редактора: ромб в цвете источника и окружность радиуса (ярче у выбранного объекта)
        public static void DrawGizmos(Scene scene, Camera camera, SpriteRenderer renderer, Texture white,
            GameObject? selected, int viewportHeight)
        {
            float pixel = camera.ViewHeight / Math.Max(1, viewportHeight);

            foreach (var entity in scene.Entities)
            {
                if (entity.IsUI) continue;
                var light = entity.GetComponent<LightComponent>();
                if (light == null) continue;

                bool isSelected = entity == selected;
                var center = light.WorldPosition;
                var ringColor = new Vector4D<float>(light.Color.X, light.Color.Y, light.Color.Z, isSelected ? 0.9f : 0.3f);
                DrawRing(renderer, camera, white, center, light.Radius, pixel * (isSelected ? 2f : 1f), ringColor);
                if (isSelected && light.Glow > 0f && light.GlowSize > 0f)
                    DrawRing(renderer, camera, white, center, light.Radius * light.GlowSize, pixel, ringColor);

                float markerSize = pixel * (isSelected ? 12f : 9f);
                renderer.DrawColored(white, center, new Vector2D<float>(markerSize, markerSize), 45f, camera,
                    new Vector2D<float>(0f, 0f), new Vector2D<float>(1f, 1f), false, new Vector4D<float>(0f, 0f, 0f, 0.85f));
                renderer.DrawColored(white, center, new Vector2D<float>(markerSize - pixel * 3f, markerSize - pixel * 3f), 45f, camera,
                    new Vector2D<float>(0f, 0f), new Vector2D<float>(1f, 1f), false,
                    new Vector4D<float>(light.Color.X, light.Color.Y, light.Color.Z, light.Enabled ? 1f : 0.35f));
            }
        }

        private static void DrawRing(SpriteRenderer renderer, Camera camera, Texture white,
            Vector2D<float> center, float radius, float thickness, Vector4D<float> color)
        {
            const int Segments = 48;
            float step = MathF.Tau / Segments;
            float segmentLength = 2f * radius * MathF.Sin(step * 0.5f) + thickness;

            for (int i = 0; i < Segments; i++)
            {
                float angle = (i + 0.5f) * step;
                var position = new Vector2D<float>(center.X + MathF.Cos(angle) * radius, center.Y + MathF.Sin(angle) * radius);
                renderer.DrawColored(white, position, new Vector2D<float>(segmentLength, thickness), angle * 180f / MathF.PI + 90f,
                    camera, new Vector2D<float>(0f, 0f), new Vector2D<float>(1f, 1f), false, color);
            }
        }

        private void EnsureSize(int width, int height)
        {
            _lightMap.Resize(width, height);
            _litScene.Resize(width, height);
            int bloomWidth = Math.Max(1, width / BloomDownscale);
            int bloomHeight = Math.Max(1, height / BloomDownscale);
            _bloomA.Resize(bloomWidth, bloomHeight);
            _bloomB.Resize(bloomWidth, bloomHeight);
        }

        private void BeginQuads(Camera camera)
        {
            _lightShader.Use();
            _lightShader.SetUniform("uView", camera.GetViewMatrix());
            _lightShader.SetUniform("uProjection", camera.GetProjectionMatrix());
            _quadCount = 0;
            _quadVertexFloatCount = 0;
        }

        private void AddQuad(Vector2D<float> center, float radius, Vector3 color, float falloff)
        {
            if (_quadCount >= MaxQuads) FlushQuads();

            AddVertex(-1f, -1f);
            AddVertex(1f, -1f);
            AddVertex(1f, 1f);
            AddVertex(-1f, 1f);
            _quadCount++;

            void AddVertex(float localX, float localY)
            {
                _quadVertices[_quadVertexFloatCount++] = center.X + localX * radius;
                _quadVertices[_quadVertexFloatCount++] = center.Y + localY * radius;
                _quadVertices[_quadVertexFloatCount++] = localX;
                _quadVertices[_quadVertexFloatCount++] = localY;
                _quadVertices[_quadVertexFloatCount++] = color.X;
                _quadVertices[_quadVertexFloatCount++] = color.Y;
                _quadVertices[_quadVertexFloatCount++] = color.Z;
                _quadVertices[_quadVertexFloatCount++] = 1f;
                _quadVertices[_quadVertexFloatCount++] = falloff;
            }
        }

        private void FlushQuads()
        {
            if (_quadCount == 0) return;

            _lightShader.Use();
            _gl.BindVertexArray(_quadVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
            unsafe
            {
                fixed (float* vertexData = _quadVertices)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0,
                        (nuint)(_quadVertexFloatCount * sizeof(float)), vertexData);
                }
                _gl.DrawElements(PrimitiveType.Triangles, (uint)(_quadCount * 6), DrawElementsType.UnsignedInt, null);
            }
            _quadCount = 0;
            _quadVertexFloatCount = 0;
        }

        private void DrawFullscreen()
        {
            _gl.BindVertexArray(_fullscreenVao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        private void BindTexture(int slot, uint handle)
        {
            _gl.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + slot));
            _gl.BindTexture(TextureTarget.Texture2D, handle);
        }

        public void Dispose()
        {
            _lightShader.Dispose();
            _compositeShader.Dispose();
            _bloomExtractShader.Dispose();
            _blurShader.Dispose();
            _finalShader.Dispose();
            _gl.DeleteBuffer(_quadVbo);
            _gl.DeleteBuffer(_quadEbo);
            _gl.DeleteVertexArray(_quadVao);
            _gl.DeleteVertexArray(_fullscreenVao);
            _lightMap.Dispose();
            _litScene.Dispose();
            _bloomA.Dispose();
            _bloomB.Dispose();
        }
    }
}
