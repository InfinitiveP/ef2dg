#version 330 core

in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uScene;
uniform sampler2D uLightMap;

void main()
{
    // Карта освещения умножается на кадр: темнота затемняет, яркий свет (> 1) пересвечивает
    vec3 albedo = texture(uScene, vUv).rgb;
    vec3 light = texture(uLightMap, vUv).rgb;
    FragColor = vec4(albedo * light, 1.0);
}
