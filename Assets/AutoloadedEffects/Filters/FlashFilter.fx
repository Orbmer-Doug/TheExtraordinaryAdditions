sampler screenTexture : register(s0);

float intensity; // Effect strength (e.g., 0.1 to 1.0)
float2 screenPos; // Center of effect in screen space (pixels)
float2 screenSize; // Screen resolution (width, height in pixels)
float radius; // Max radius of effect in UV space (e.g., 0.05)
float zoom; // Zoom level (1.0 = no zoom, 2.0 = 2x zoom)
float falloffSigma; // Controls Gaussian falloff smoothness (e.g., 0.5 to 2.0)

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float2 effectPos = screenPos / screenSize; // Convert to UV space
    
    // Calculate distance from center in UV space, adjusted for aspect ratio
    float2 adjustedDelta = coords - effectPos;
    adjustedDelta.x *= screenSize.x / screenSize.y; // Correct for aspect ratio
    float dist = length(adjustedDelta) / radius; // Normalized distance from center
    
    // Original color
    float4 originalColor = tex2D(screenTexture, coords);
    
    // Early exit if outside radius
    if (dist > 1.0)
        return originalColor;
    
    // Sharp Gaussian falloff for glow
    float sigma = falloffSigma * 0.5; // Sharper falloff than blur
    float weight = exp(-dist * dist / (2.0 * sigma * sigma));
    weight = max(weight, 0.0001); // Prevent division by zero
    
    // Calculate glow color (white, scaled by intensity)
    float glowStrength = weight * intensity;
    float4 glowColor = float4(glowStrength, glowStrength, glowStrength, 0.0);
    
    // Add glow to original color
    float4 finalColor = originalColor + glowColor;
    finalColor.rgb = min(finalColor.rgb, 1.0); // Clamp to avoid overexposure
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}