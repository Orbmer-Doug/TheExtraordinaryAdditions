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
    
    // Gaussian falloff
    float sigma = falloffSigma + 0.1 / (intensity + 0.1); // Soften at low intensity
    float weight = exp(-dist * dist / (2.0 * sigma * sigma));
    weight = max(weight, 0.0001); // Prevent division by zero
    
    // Calculate offset based on intensity and zoom
    float offset = intensity * 0.005 / zoom; // Smaller offset at higher zoom
    float2 offsetR = float2(-offset, 0.0); // Red shifts left
    float2 offsetG = float2(0.0, -offset); // Green shifts up
    float2 offsetB = float2(offset, offset); // Blue shifts right-down
    
    // Sample each channel separately
    float r = tex2D(screenTexture, coords + offsetR).r;
    float g = tex2D(screenTexture, coords + offsetG).g;
    float b = tex2D(screenTexture, coords + offsetB).b;
    
    // Combine channels
    float4 aberratedColor = float4(r, g, b, originalColor.a);
    
    // Blend with original based on weight
    float blendFactor = saturate(weight * intensity);
    float4 finalColor = lerp(originalColor, aberratedColor, blendFactor);
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}