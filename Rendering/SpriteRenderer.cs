using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ef2dg
{
    public sealed class SpriteRenderer : IDisposable
    {
        private const int MaxSprites = 2048;
        private const int MaxTextureSlots = 16;
        private const int FloatsPerVertex = 10; // XYZ, UV, RGBA, texture slot
        private const int VerticesPerSprite = 4;
        private const int IndicesPerSprite = 6;

        private readonly GL _gl;
        private readonly Shader _shader;
        private readonly Shader _debugShader;

        // Статический quad остаётся отдельным только для debug.vert/debug.frag.
        private readonly VertexArrayObject<float> _debugVao;
        private readonly BufferObject<float> _debugVbo;
        private readonly BufferObject<uint> _debugEbo;

        // Основной динамический multi-texture sprite batch.
        private readonly uint _batchVao;
        private readonly uint _batchVbo;
        private readonly uint _batchEbo;
        private readonly float[] _batchVertices = new float[MaxSprites * VerticesPerSprite * FloatsPerVertex];
        private readonly uint[] _batchTextureHandles = new uint[MaxTextureSlots];
        private int _batchVertexFloatCount;
        private int _batchSpriteCount;
        private int _batchTextureCount;
        private Matrix4X4<float> _batchView;
        private Matrix4X4<float> _batchProjection;
        private bool _batchHasMatrices;

        // Специализированный batch одноцветной UI-геометрии сохранён: он не требует texture sampling.
        private readonly Shader _solidBatchShader;
        private readonly uint _solidBatchVao;
        private readonly uint _solidBatchVbo;
        private readonly uint _solidBatchEbo;
        private readonly List<float> _solidBatchVertices = new(7 * 4 * 512);
        private readonly List<uint> _solidBatchIndices = new(6 * 512);
        private Camera? _solidBatchCamera;
        private bool _solidBatchActive;

        private int _drawCallsThisFrame;
        private int _quadsThisFrame;
        public int LastFrameDrawCalls { get; private set; }
        public int LastFrameQuads { get; private set; }

        public SpriteRenderer(GL gl)
        {
            _gl = gl;

            float[] debugVertices =
            {
                 0.5f,  0.5f, 0.0f, 1.0f, 0.0f,
                 0.5f, -0.5f, 0.0f, 1.0f, 1.0f,
                -0.5f, -0.5f, 0.0f, 0.0f, 1.0f,
                -0.5f,  0.5f, 0.0f, 0.0f, 0.0f
            };
            uint[] debugIndices = { 0, 1, 3, 1, 2, 3 };
            _debugVbo = new BufferObject<float>(_gl, debugVertices, BufferTargetARB.ArrayBuffer);
            _debugEbo = new BufferObject<uint>(_gl, debugIndices, BufferTargetARB.ElementArrayBuffer);
            _debugVao = new VertexArrayObject<float>(_gl, _debugVbo);
            _debugEbo.Bind();

            _shader = new Shader(_gl, "shader.vert", "shader.frag");
            _debugShader = new Shader(_gl, "debug.vert", "debug.frag");

            _batchVao = _gl.GenVertexArray();
            _batchVbo = _gl.GenBuffer();
            _batchEbo = _gl.GenBuffer();
            uint[] batchIndices = BuildBatchIndices();

            _gl.BindVertexArray(_batchVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _batchVbo);
            unsafe
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(_batchVertices.Length * sizeof(float)), null, BufferUsageARB.DynamicDraw);
            }
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _batchEbo);
            unsafe
            {
                fixed (uint* indexData = batchIndices)
                    _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                        (nuint)(batchIndices.Length * sizeof(uint)), indexData, BufferUsageARB.StaticDraw);

                uint stride = FloatsPerVertex * sizeof(float);
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);
                _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
                _gl.EnableVertexAttribArray(1);
                _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));
                _gl.EnableVertexAttribArray(2);
                _gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, (void*)(9 * sizeof(float)));
                _gl.EnableVertexAttribArray(3);
            }

            _shader.Use();
            for (int slot = 0; slot < MaxTextureSlots; slot++)
            {
                int location = _shader.GetUniformLocation($"uTextures[{slot}]");
                if (location >= 0) _gl.Uniform1(location, slot);
            }

            _solidBatchShader = new Shader(_gl, "ui_batch.vert", "ui_batch.frag");
            _solidBatchVao = _gl.GenVertexArray();
            _solidBatchVbo = _gl.GenBuffer();
            _solidBatchEbo = _gl.GenBuffer();
            _gl.BindVertexArray(_solidBatchVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _solidBatchVbo);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _solidBatchEbo);
            unsafe
            {
                uint stride = 7u * sizeof(float);
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);
                _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
                _gl.EnableVertexAttribArray(1);
            }

            _gl.BindVertexArray(0);
        }

        public void BeginFrame()
        {
            FlushAll();
            _drawCallsThisFrame = 0;
            _quadsThisFrame = 0;
        }

        public void EndFrame()
        {
            FlushAll();
            LastFrameDrawCalls = _drawCallsThisFrame;
            LastFrameQuads = _quadsThisFrame;
        }

        public void BeginSolidBatch(Camera camera)
        {
            if (_solidBatchActive) EndSolidBatch();
            Flush();
            _solidBatchCamera = camera;
            _solidBatchActive = true;
        }

        public void DrawSolid(
            Texture fallbackTexture,
            Vector2D<float> position,
            Vector2D<float> size,
            float rotationDeg,
            Camera camera,
            Vector4D<float> color)
        {
            if (!_solidBatchActive || !ReferenceEquals(_solidBatchCamera, camera))
            {
                DrawColored(fallbackTexture, position, size, rotationDeg, camera,
                    new(0f, 0f), new(1f, 1f), false, color);
                return;
            }

            // Нельзя держать одновременно sprite и solid batch: иначе нарушится порядок alpha blending.
            Flush();

            float halfWidth = size.X * 0.5f;
            float halfHeight = size.Y * 0.5f;
            float radians = rotationDeg * MathF.PI / 180f;
            float cosine = MathF.Cos(radians);
            float sine = MathF.Sin(radians);
            uint firstVertex = (uint)(_solidBatchVertices.Count / 7);

            AddSolidVertex(-halfWidth, halfHeight);
            AddSolidVertex(halfWidth, halfHeight);
            AddSolidVertex(halfWidth, -halfHeight);
            AddSolidVertex(-halfWidth, -halfHeight);

            _solidBatchIndices.Add(firstVertex);
            _solidBatchIndices.Add(firstVertex + 1);
            _solidBatchIndices.Add(firstVertex + 3);
            _solidBatchIndices.Add(firstVertex + 1);
            _solidBatchIndices.Add(firstVertex + 2);
            _solidBatchIndices.Add(firstVertex + 3);
            _quadsThisFrame++;

            void AddSolidVertex(float localX, float localY)
            {
                float x = position.X + localX * cosine - localY * sine;
                float y = position.Y + localX * sine + localY * cosine;
                _solidBatchVertices.Add(x);
                _solidBatchVertices.Add(y);
                _solidBatchVertices.Add(0f);
                _solidBatchVertices.Add(color.X);
                _solidBatchVertices.Add(color.Y);
                _solidBatchVertices.Add(color.Z);
                _solidBatchVertices.Add(color.W);
            }
        }

        public void FlushSolidBatch()
        {
            if (!_solidBatchActive || _solidBatchCamera == null || _solidBatchIndices.Count == 0) return;

            Span<float> vertices = CollectionsMarshal.AsSpan(_solidBatchVertices);
            Span<uint> indices = CollectionsMarshal.AsSpan(_solidBatchIndices);
            _solidBatchShader.Use();
            _solidBatchShader.SetUniform("uView", _solidBatchCamera.GetViewMatrix());
            _solidBatchShader.SetUniform("uProjection", _solidBatchCamera.GetProjectionMatrix());
            _gl.BindVertexArray(_solidBatchVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _solidBatchVbo);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _solidBatchEbo);
            unsafe
            {
                fixed (float* vertexData = vertices)
                    _gl.BufferData(BufferTargetARB.ArrayBuffer,
                        (nuint)(vertices.Length * sizeof(float)), vertexData, BufferUsageARB.StreamDraw);
                fixed (uint* indexData = indices)
                    _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                        (nuint)(indices.Length * sizeof(uint)), indexData, BufferUsageARB.StreamDraw);
                _gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
            }
            _drawCallsThisFrame++;
            _solidBatchVertices.Clear();
            _solidBatchIndices.Clear();
        }

        public void EndSolidBatch()
        {
            FlushSolidBatch();
            _solidBatchActive = false;
            _solidBatchCamera = null;
        }

        public void Draw(
            Texture texture,
            Vector2D<float> position,
            Vector2D<float> size,
            float rotationDeg,
            Camera camera,
            Vector2D<float> texOffset,
            Vector2D<float> texScale,
            bool flipX)
        {
            DrawColored(texture, position, size, rotationDeg, camera, texOffset, texScale, flipX,
                new Vector4D<float>(1f, 1f, 1f, 1f));
        }

        public void DrawColored(
            Texture texture,
            Vector2D<float> position,
            Vector2D<float> size,
            float rotationDeg,
            Camera camera,
            Vector2D<float> texOffset,
            Vector2D<float> texScale,
            bool flipX,
            Vector4D<float> color)
        {
            // Текстурный quad должен оказаться после уже накопленной solid-геометрии.
            FlushSolidBatch();

            Matrix4X4<float> view = camera.GetViewMatrix();
            Matrix4X4<float> projection = camera.GetProjectionMatrix();
            if (!_batchHasMatrices)
            {
                SetBatchMatrices(view, projection);
            }
            else if (!_batchView.Equals(view) || !_batchProjection.Equals(projection))
            {
                Flush();
                SetBatchMatrices(view, projection);
            }

            if (_batchSpriteCount >= MaxSprites) Flush();
            int textureSlot = ResolveTextureSlot(texture.Handle);

            float halfWidth = size.X * 0.5f * (flipX ? -1f : 1f);
            float halfHeight = size.Y * 0.5f;
            float radians = rotationDeg * MathF.PI / 180f;
            float cosine = MathF.Cos(radians);
            float sine = MathF.Sin(radians);

            float u0 = texOffset.X;
            float v0 = texOffset.Y;
            float u1 = texOffset.X + texScale.X;
            float v1 = texOffset.Y + texScale.Y;

            AddSpriteVertex(halfWidth, halfHeight, u1, v0);
            AddSpriteVertex(halfWidth, -halfHeight, u1, v1);
            AddSpriteVertex(-halfWidth, -halfHeight, u0, v1);
            AddSpriteVertex(-halfWidth, halfHeight, u0, v0);

            _batchSpriteCount++;
            _quadsThisFrame++;

            void AddSpriteVertex(float localX, float localY, float u, float v)
            {
                float x = position.X + localX * cosine - localY * sine;
                float y = position.Y + localX * sine + localY * cosine;
                _batchVertices[_batchVertexFloatCount++] = x;
                _batchVertices[_batchVertexFloatCount++] = y;
                _batchVertices[_batchVertexFloatCount++] = 0f;
                _batchVertices[_batchVertexFloatCount++] = u;
                _batchVertices[_batchVertexFloatCount++] = v;
                _batchVertices[_batchVertexFloatCount++] = color.X;
                _batchVertices[_batchVertexFloatCount++] = color.Y;
                _batchVertices[_batchVertexFloatCount++] = color.Z;
                _batchVertices[_batchVertexFloatCount++] = color.W;
                _batchVertices[_batchVertexFloatCount++] = textureSlot;
            }
        }

        public void Flush()
        {
            if (_batchSpriteCount == 0) return;

            _shader.Use();
            _shader.SetUniform("uView", _batchView);
            _shader.SetUniform("uProjection", _batchProjection);
            for (int slot = 0; slot < _batchTextureCount; slot++)
            {
                _gl.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + slot));
                _gl.BindTexture(TextureTarget.Texture2D, _batchTextureHandles[slot]);
            }

            _gl.BindVertexArray(_batchVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _batchVbo);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _batchEbo);
            unsafe
            {
                fixed (float* vertexData = _batchVertices)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0,
                        (nuint)(_batchVertexFloatCount * sizeof(float)), vertexData);
                }
                _gl.DrawElements(PrimitiveType.Triangles,
                    (uint)(_batchSpriteCount * IndicesPerSprite), DrawElementsType.UnsignedInt, null);
            }
            _gl.ActiveTexture(TextureUnit.Texture0);
            _drawCallsThisFrame++;
            _batchSpriteCount = 0;
            _batchVertexFloatCount = 0;
            _batchTextureCount = 0;
        }

        public void FlushAll()
        {
            // Оба типа batch не должны содержать данные одновременно, но такой порядок безопасен и при ошибочном вызове.
            FlushSolidBatch();
            Flush();
        }

        public void DrawCollisionBox(Vector2D<float> position, Vector2D<float> size, Camera camera, Vector4D<float> color)
        {
            FlushAll();

            _debugShader.Use();
            _debugShader.SetUniform("uView", camera.GetViewMatrix());
            _debugShader.SetUniform("uProjection", camera.GetProjectionMatrix());
            int colorLocation = _debugShader.GetUniformLocation("uColor");
            if (colorLocation >= 0) _gl.Uniform4(colorLocation, color.X, color.Y, color.Z, color.W);

            var model = Matrix4X4<float>.Identity;
            model *= Matrix4X4.CreateScale(size.X, size.Y, 1f);
            model *= Matrix4X4.CreateTranslation(position.X, position.Y, 0f);
            _debugShader.SetUniform("uModel", model);

            _debugVao.Bind();
            _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            unsafe
            {
                _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
            }
            _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            _drawCallsThisFrame++;
            _quadsThisFrame++;
        }

        private int ResolveTextureSlot(uint handle)
        {
            for (int slot = 0; slot < _batchTextureCount; slot++)
                if (_batchTextureHandles[slot] == handle) return slot;

            if (_batchTextureCount >= MaxTextureSlots) Flush();
            int result = _batchTextureCount;
            _batchTextureHandles[_batchTextureCount++] = handle;
            return result;
        }

        private void SetBatchMatrices(Matrix4X4<float> view, Matrix4X4<float> projection)
        {
            _batchView = view;
            _batchProjection = projection;
            _batchHasMatrices = true;
        }

        private static uint[] BuildBatchIndices()
        {
            var indices = new uint[MaxSprites * IndicesPerSprite];
            for (int sprite = 0; sprite < MaxSprites; sprite++)
            {
                uint vertex = (uint)(sprite * VerticesPerSprite);
                int index = sprite * IndicesPerSprite;
                indices[index] = vertex;
                indices[index + 1] = vertex + 1;
                indices[index + 2] = vertex + 3;
                indices[index + 3] = vertex + 1;
                indices[index + 4] = vertex + 2;
                indices[index + 5] = vertex + 3;
            }
            return indices;
        }

        public void Dispose()
        {
            FlushAll();
            _solidBatchActive = false;
            _solidBatchCamera = null;

            _solidBatchShader.Dispose();
            _gl.DeleteBuffer(_solidBatchVbo);
            _gl.DeleteBuffer(_solidBatchEbo);
            _gl.DeleteVertexArray(_solidBatchVao);

            _gl.DeleteBuffer(_batchVbo);
            _gl.DeleteBuffer(_batchEbo);
            _gl.DeleteVertexArray(_batchVao);

            _debugVbo.Dispose();
            _debugEbo.Dispose();
            _debugVao.Dispose();
            _shader.Dispose();
            _debugShader.Dispose();
        }
    }
}
