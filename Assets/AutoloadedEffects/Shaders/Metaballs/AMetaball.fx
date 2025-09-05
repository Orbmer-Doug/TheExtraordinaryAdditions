sampler2D InfluenceMap : register(s1);

float globalTime;
float threshold;
float epsilon;
float4 color;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    /*
    float influence = tex2D(InfluenceMap, uv).r;
    float alpha = smoothstep(threshold - epsilon, threshold + epsilon, influence);
    // Amplify RGB based on influence to counteract transparency
    float3 finalColor = tex2D(InfluenceMap, uv).rgb * (alpha * 2);
    finalColor = pow(finalColor, 2.2);
    finalColor += tex2D(InfluenceMap, uv).b;
    finalColor += tex2D(InfluenceMap, uv) * 2;
    
    return float4(finalColor, alpha);
    */
    
    // Sample the accumulated influence (RGB contains color, R is intensity)
    float influence = tex2D(InfluenceMap, uv).r;
    float3 accumulatedColor = tex2D(InfluenceMap, uv).rgb;

    // Smoothstep for contouring
    float alpha = smoothstep(threshold - epsilon, threshold + epsilon, influence);

    // Base color: Fire-hot orange with gradient
    float3 baseColor = float3(1.0, 0.5, 1.0); // Bright orange (RGB: 255, 128, 0)
    float3 coreColor = float3(1.0, 0.8, 0.2); // Lighter orange core (RGB: 255, 204, 51)

    // Simulate ionization flicker with simple noise (using UV and time)
    float3 dynamicColor = lerp(baseColor, coreColor, saturate(influence));

    // Add a glowing halo effect at the edges
    float edgeGlow = smoothstep(threshold - epsilon * 2.0, threshold, influence);
    float3 haloColor = float3(5.0, 0.3, 0.0) * edgeGlow * 0.5; // Red-orange halo

    // Combine colors with accumulated input
    float3 finalColor = (accumulatedColor / dynamicColor + haloColor) * (alpha * 2.0);

    return float4(finalColor, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}