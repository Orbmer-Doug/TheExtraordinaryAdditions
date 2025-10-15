// Inspired from: https://github.com/LucilleKarma/WrathOfTheGodsPublic/blob/fa20477aa93ce1525a0b0abe8a471149fe5ce271/Assets/AutoloadedEffects/Shaders/Objects/Avatar/VoidBlotShader.fx

sampler screenTexture : register(s0);
sampler noiseRingTexture : register(s1);

float globalTime;
float scale;
float3 edgeColor;
float2 screenPos; // Center of effect in screen space (pixels)
float2 screenSize; // Screen resolution (width, height in pixels)
float radius; // Radius of effect in UV space

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 screenColor = tex2D(screenTexture, coords);
    float2 effectPos = screenPos / screenSize;
    float2 delta = coords - effectPos;
    float aspect = screenSize.x / screenSize.y;
    float2 adjustedDelta = float2(delta.x * aspect, delta.y);
    float distanceFromCenter = length(adjustedDelta);
    float scaledDistance = distanceFromCenter / radius * 0.5;

    // Glow calculations
    float noise = tex2D(noiseRingTexture, coords * 0.23);
    float edgeNoise = tex2D(noiseRingTexture, coords * 0.09 + float2(0, globalTime * 0.05)) - 0.7;
    float edgeGlowOpacity = pow(0.02 / distance(scaledDistance - edgeNoise * 0.091, 0.38), 3);
    float edgeCutoffOpacity = smoothstep(0.5, 0.49, scaledDistance);
    float4 glowColor = noise * edgeCutoffOpacity * float4(edgeColor, 1) * clamp(edgeGlowOpacity, 0, 3);
    
    // White center calculations
    float whiteEdgeExpand = (1 - scale) * 0.39 + noise * (1 - scale) * 0.2;
    float whiteInterpolant = smoothstep(0.38, 0.34, scaledDistance) * smoothstep(whiteEdgeExpand - 0.01, whiteEdgeExpand, scaledDistance);
    
    // Combine with glow
    float4 finalColor = lerp(screenColor, float4(1, 1, 1, 1), whiteInterpolant) + glowColor;
    finalColor = clamp(finalColor, 0, 1);

    return float4(finalColor.rgb, screenColor.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}