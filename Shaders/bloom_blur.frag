#version 330 core

in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uTexture;
// Sampling step in UV: texel size * direction (X or Y) * radius
uniform vec2 uDirection;

const float Weights[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

void main()
{
    vec3 color = texture(uTexture, vUv).rgb * Weights[0];
    for (int i = 1; i < 5; i++)
    {
        vec2 offset = uDirection * float(i);
        color += texture(uTexture, vUv + offset).rgb * Weights[i];
        color += texture(uTexture, vUv - offset).rgb * Weights[i];
    }
    FragColor = vec4(color, 1.0);
}
