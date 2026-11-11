#version 330 core

in vec2 vLocal;
in vec4 vColor;
in float vFalloff;

out vec4 FragColor;

void main()
{
    // 0 at the source centre, 1 at the radius edge
    float distanceToCenter = length(vLocal);
    if (distanceToCenter >= 1.0) discard;

    // Smooth edge falloff: Falloff > 1 pulls light towards the centre, < 1 makes the spot more even
    float attenuation = pow(1.0 - smoothstep(0.0, 1.0, distanceToCenter), vFalloff);
    FragColor = vec4(vColor.rgb * attenuation, 1.0);
}
