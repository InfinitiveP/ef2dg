#version 330 core

in vec2 vLocal;
in vec4 vColor;
in float vFalloff;

out vec4 FragColor;

void main()
{
    // 0 в центре источника, 1 на границе радиуса
    float distanceToCenter = length(vLocal);
    if (distanceToCenter >= 1.0) discard;

    // Плавный спад к краю: Falloff > 1 стягивает свет к центру, < 1 делает пятно ровнее
    float attenuation = pow(1.0 - smoothstep(0.0, 1.0, distanceToCenter), vFalloff);
    FragColor = vec4(vColor.rgb * attenuation, 1.0);
}
