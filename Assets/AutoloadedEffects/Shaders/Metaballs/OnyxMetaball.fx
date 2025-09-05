sampler2D InfluenceMap : register(s1);

float globalTime;
float threshold;
float epsilon;

float edgeDetect(float2 uv, float2 resolution)
{
    float step = 1.0 / resolution; // Step size based on resolution
    float h = tex2D(InfluenceMap, uv + float2(step, 0)).r - tex2D(InfluenceMap, uv - float2(step, 0)).r;
    float v = tex2D(InfluenceMap, uv + float2(0, step)).r - tex2D(InfluenceMap, uv - float2(0, step)).r;
    return sqrt(h * h + v * v) * .8; // Magnitude of gradient
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float influence = tex2D(InfluenceMap, uv).r;
    float alpha = smoothstep(threshold - epsilon, threshold + epsilon, influence);
    
    // Edge detection
    float edge = edgeDetect(uv, float2(1150, 1150) * alpha);
    
    float3 onyxPurple = float3(0.6, 0.2, 1.0);
    float shimmer = sin(globalTime * 5.0 + uv.x * 10.0 + uv.y * 10.0) * 0.1 + 0.9;
    onyxPurple *= shimmer;
    
    // Calculate outline strength
    float outlineStrength = edge * (8.0 - alpha);
    
    // Interior is pitch black where influence is high
    float3 interiorColor = float3(0.0, 0.0, 0.0);
    float3 finalColor = lerp(interiorColor, onyxPurple, outlineStrength);
    
    float3 border = onyxPurple * edge * (1.0 - alpha);
    return lerp(float4(finalColor, alpha), float4(border, 1), edge) * 2;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}