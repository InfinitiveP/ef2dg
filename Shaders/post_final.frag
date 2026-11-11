#version 330 core

in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uScene;
uniform sampler2D uBloom;
uniform float uBloomStrength;
uniform float uExposure;
uniform float uContrast;
uniform float uSaturation;
uniform vec3 uTint;
uniform float uVignetteStrength;
uniform float uVignetteSize;
uniform vec3 uVignetteColor;
uniform float uAspect;

void main()
{
    vec3 color = texture(uScene, vUv).rgb;
    if (uBloomStrength > 0.0) color += texture(uBloom, vUv).rgb * uBloomStrength;

    color *= uExposure * uTint;

    float luminance = dot(color, vec3(0.299, 0.587, 0.114));
    color = mix(vec3(luminance), color, uSaturation);
    color = (color - 0.5) * uContrast + 0.5;

    // Vignette: 0 at the frame centre, 1 in the corners (taking the viewport aspect into account)
    vec2 centered = (vUv - 0.5) * vec2(uAspect, 1.0);
    float radius = length(centered) / length(vec2(0.5 * uAspect, 0.5));
    float vignette = smoothstep(uVignetteSize, uVignetteSize + 0.6, radius) * uVignetteStrength;
    color = mix(color, uVignetteColor, clamp(vignette, 0.0, 1.0));

    FragColor = vec4(clamp(color, 0.0, 1.0), 1.0);
}
