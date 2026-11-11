#version 330 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aLocal;
layout (location = 2) in vec4 aColor;
layout (location = 3) in float aFalloff;

out vec2 vLocal;
out vec4 vColor;
out float vFalloff;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * vec4(aPosition, 0.0, 1.0);
    vLocal = aLocal;
    vColor = aColor;
    vFalloff = aFalloff;
}
