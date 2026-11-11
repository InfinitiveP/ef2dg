#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec4 aColor;
layout (location = 3) in float aTextureIndex;

out vec2 TexCoord;
out vec4 VertColor;
flat out int TextureIndex;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * vec4(aPosition, 1.0);
    TexCoord = aTexCoord;
    VertColor = aColor;
    TextureIndex = int(aTextureIndex + 0.5);
}
