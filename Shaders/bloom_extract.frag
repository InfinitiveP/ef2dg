#version 330 core

in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec2 uTexelSize;
uniform float uThreshold;

void main()
{
    // 4 bilinear samples average a 4x4 block so small highlights do not flicker when downscaled
    vec3 color = texture(uTexture, vUv + uTexelSize * vec2(-1.0, -1.0)).rgb
               + texture(uTexture, vUv + uTexelSize * vec2( 1.0, -1.0)).rgb
               + texture(uTexture, vUv + uTexelSize * vec2(-1.0,  1.0)).rgb
               + texture(uTexture, vUv + uTexelSize * vec2( 1.0,  1.0)).rgb;
    color *= 0.25;

    // Soft knee: pixels slightly below the threshold glow faintly, without a hard edge
    float brightness = max(color.r, max(color.g, color.b));
    float knee = max(uThreshold * 0.5, 0.0001);
    float soft = clamp(brightness - uThreshold + knee, 0.0, 2.0 * knee);
    soft = soft * soft / (4.0 * knee);
    float contribution = max(soft, brightness - uThreshold) / max(brightness, 0.0001);

    FragColor = vec4(color * contribution, 1.0);
}
