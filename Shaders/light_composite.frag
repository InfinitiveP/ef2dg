#version 330 core

in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uScene;
uniform sampler2D uLightMap;

void main()
{
    // The light map multiplies the frame: darkness darkens, bright light (> 1) overexposes
    vec3 albedo = texture(uScene, vUv).rgb;
    vec3 light = texture(uLightMap, vUv).rgb;
    FragColor = vec4(albedo * light, 1.0);
}
