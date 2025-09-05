sampler screenTexture : register(s0);

float intensity; // Base blur strength (e.g., 0.1 to 1.0)
float2 screenPos; // Center of blur in screen space (pixels)
float2 screenSize; // Screen resolution (width, height in pixels)
float radius; // Max radius of blur effect (in UV space, e.g., 0.05)
float zoom; // Zoom level (1.0 = no zoom, 2.0 = 2x zoom)
float falloffSigma; // Controls Gaussian falloff smoothness (e.g., 0.5 to 2.0)

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = 0;
    float2 blurPos = screenPos / screenSize; // Convert screenPos to UV space
    
    float totalWeight = 0.0;
    float minWeight = 0.0001; // Prevent division issues
    
    for (int i = 0; i < 20; i++)
    {
        // Base scale for this sample, adjusted for zoom
        float baseScale = 1.0 + float(i) * intensity * 0.01 / zoom;
        
        // Calculate offset coords
        float2 localCoords = blurPos + (coords - blurPos) * baseScale;
        
        // Calculate distance from center in UV space
        float dist = length(coords - blurPos) / radius;
        
        // Gaussian-like falloff with softened decay
        float sigma = falloffSigma + 0.1 / (intensity + 0.1); // Soften at low intensity
        float weight = exp(-dist * dist / (2.0 * sigma * sigma));
        weight = max(weight, minWeight); // Clamp to avoid tiny weights
        
        // Reduce blur strength farther from center
        float blurStrength = exp(-dist * dist / (2.0 * sigma * sigma));
        float scale = 1.0 + (baseScale - 1.0) * blurStrength;
        
        // Recalculate coords with distance-adjusted scale
        localCoords = blurPos + (coords - blurPos) * scale;
        
        // Sample texture and accumulate
        baseColor += tex2D(screenTexture, localCoords) * 0.05 * weight;
        totalWeight += 0.05 * weight;
    }
    
    // Original color for blending
    float4 originalColor = tex2D(screenTexture, coords);
    
    // Normalize and blend with original based on totalWeight
    float4 finalColor;
    if (totalWeight > minWeight)
    {
        baseColor /= totalWeight;
        // Blend with original to smooth transition
        float blendFactor = saturate(totalWeight / 0.5); // Fade to original when weights are low
        finalColor = lerp(originalColor, baseColor, blendFactor);
    }
    else
    {
        finalColor = originalColor; // Fallback to original color
    }
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}