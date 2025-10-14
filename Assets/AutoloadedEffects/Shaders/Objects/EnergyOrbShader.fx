sampler noiseTexture : register(s0);

float globalTime : register(c0);
float pulseIntensity : register(c1);
float glowIntensity : register(c2);
float glowPower : register(c3);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Polar setup
    float distanceFromCenter = distance(coords, 0.5);
    float2 polar = float2(distanceFromCenter, atan2(coords.y - 0.5, coords.x - 0.5) / 3.141 + 0.5);
    
    // Make the orb pulse and glow
    float pulse = cos(globalTime * 45 + polar.x * 12) * 0.5 + 0.5;
    float distanceNoise = tex2D(noiseTexture, polar * float2(0.7, 1) - globalTime);
    float distortedDistance = saturate(distance(coords, 0.5) + distanceNoise * 0.03 + pulse * pulseIntensity);
    float glow = glowIntensity / pow(distortedDistance, glowPower);
    
    // Combine colors
    float4 color = saturate(sampleColor * (glow + 1)) * smoothstep(0.5, 0.4, distortedDistance);
    color = saturate(color);
    
    // Color the edges more blue
    color.rg -= smoothstep(0.3, 0.4, distanceFromCenter) * 0.3;
    
    // Add some quantization
    color = floor(color * 20) / 20;
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}