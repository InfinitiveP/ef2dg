using System;
using System.IO;
using System.Text;
using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace ef2dg
{
    public class Shader : IDisposable
    {
        private uint _handle;
        private GL _gl;

        public Shader(GL gl, string vertexPath, string fragmentPath)
        {
            _gl = gl;

            string vertexSource = LoadSource(vertexPath);
            string fragmentSource = LoadSource(fragmentPath);

            uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
            uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertex);
            _gl.AttachShader(_handle, fragment);
            _gl.LinkProgram(_handle);

            _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Link error: {_gl.GetProgramInfoLog(_handle)}");
            }

            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
        }

        // EOF error fix
        private string LoadSource(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            string code = reader.ReadToEnd();

            // line ending fix (Windows -> Unix)
            code = code.Replace("\r\n", "\n");

            // trailing newline
            return code + "\n";
        }

        private uint CompileShader(ShaderType type, string source)
        {
            uint handle = _gl.CreateShader(type);
            _gl.ShaderSource(handle, source);
            _gl.CompileShader(handle);

            string infoLog = _gl.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Compile error {type}: {infoLog}");
            }
            return handle;
        }

        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        // method needed for textures
        public int GetUniformLocation(string name)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            // -1 means the variable is not used in the shader; this is not critical
            return location;
        }

        // Passes a 4x4 matrix to the shader
        public unsafe void SetUniform(string name, Matrix4X4<float> value)
        {
            int location = GetUniformLocation(name);
            if (location == -1) return;

            // false means we do not want to transpose the matrix (swap rows and columns)
            _gl.UniformMatrix4(location, 1, false, (float*)&value);
        }

        // Passes a Vector2D (Scale and Offset) to the shader
        public void SetUniform(string name, Vector2D<float> value)
        {
            int location = GetUniformLocation(name);
            if (location == -1) return;

            // Send two floats (X and Y)
            _gl.Uniform2(location, value.X, value.Y);
        }

        public void SetUniform(string name, float value)
        {
            int location = GetUniformLocation(name);
            if (location == -1) return;
            _gl.Uniform1(location, value);
        }

        // Used to bind a sampler2D to a texture slot number
        public void SetUniform(string name, int value)
        {
            int location = GetUniformLocation(name);
            if (location == -1) return;
            _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, System.Numerics.Vector3 value)
        {
            int location = GetUniformLocation(name);
            if (location == -1) return;
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}