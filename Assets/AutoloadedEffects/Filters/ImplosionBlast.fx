sampler screenTexture : register(s0);

float intensity; // Effect strength (0.0 to 1.0)
float2 screenPos; // Center of effect in screen space (pixels)
float2 screenSize; // Screen resolution (width, height in pixels)
float radius; // Max radius of effect in UV space (e.g., 0.05)
float falloffSigma; // Controls Gaussian falloff smoothness (e.g., 0.5 to 2.0)

float InverseLerp(float from, float to, float x)
{
    return (x - from) / (to - from);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float2 effectPos = screenPos / screenSize; // Convert to UV space
    
    // Calculate distance from center in UV space, adjusted for aspect ratio
    float2 adjustedDelta = coords - effectPos;
    adjustedDelta.x *= screenSize.x / screenSize.y; // Correct for aspect ratio
    float dist = length(adjustedDelta) / radius; // Normalized distance from center
    
    // Early exit if outside radius
    if (dist > 1.0)
        return tex2D(screenTexture, coords);
    
    // Gaussian falloff for smooth warp
    float sigma = falloffSigma * 0.5;
    float weight = exp(-dist * dist / (2.0 * sigma * sigma));
    weight = max(weight, 0.0001); // Prevent division by zero
    
    // Warp coordinates toward center
    float warpStrength = intensity * weight; // Scale warp by intensity and falloff
    float2 warpOffset = adjustedDelta * warpStrength; // Pull toward center
    float2 warpedCoords = coords + warpOffset;
    
    // Sample color at warped coordinates
    float4 color = tex2D(screenTexture, warpedCoords);
    
    if (intensity > 0.7)
    {
        color = lerp(color, float4(0, 0, 0, 0), InverseLerp(.7, 1., intensity) * weight);
    }
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}