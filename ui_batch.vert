#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;

out vec4 vertexColor;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    vertexColor = aColor;
    gl_Position = uProjection * uView * vec4(aPosition, 1.0);
}
