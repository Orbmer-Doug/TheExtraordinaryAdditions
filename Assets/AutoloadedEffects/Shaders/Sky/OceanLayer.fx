float time;
float causticSpeed = 0.4;
float causticScale = 2.0;
float intensity = 18.8;
float opacity;
float riseInterpolant;

sampler2D Texture : register(s0);

// Simple 2D noise function
float noise(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 3758.5453);
}

// Smooth noise interpolation
float smoothNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = f * f * (3.0 - 2.0 * f);
    
    return lerp(lerp(noise(i + float2(0, 0)), noise(i + float2(1, 0)), u.x),
                lerp(noise(i + float2(0, 1)), noise(i + float2(1, 1)), u.x), u.y);
}

// Layered noise
float layeredNoise(float2 uv)
{
    float n = 0.0;
    n += smoothNoise(uv * 4.0) * 0.5;
    n += smoothNoise(uv * 8.0) * 0.25;
    n += smoothNoise(uv * 16.0) * 0.125;
    return n;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float2 TexCoord : TEXCOORD0) : COLOR0
{
    float2 uv = TexCoord * causticScale;
    float2 offset1 = float2(time * causticSpeed, time * causticSpeed * 0.7);
    float2 offset2 = float2(-time * causticSpeed * 0.8, time * causticSpeed * 0.3);
    float2 offset3 = float2(time * causticSpeed * 0.5, -time * causticSpeed * 0.7);

    // Generate two caustic patterns
    float caustic1 = layeredNoise(uv + offset1);
    float caustic2 = layeredNoise(uv + offset2);
    float caustic3 = layeredNoise(uv + offset3);

    // Combine patterns
    float caustics = caustic1 * caustic2 * caustic3;
    
    // Increase contrast for sharper highlights
    caustics = pow(caustics, 1.7) * intensity;
    caustics = saturate(caustics);
    
    // Use a more vibrant color for caustics
    float3 causticColor = float3(0.5, 0.8, 1.0) * caustics; // Brighter and more saturated blue
    
    // Sample base texture
    float4 baseColor = float4(.4, .8, 1, 1);
    
    // Blend caustics more prominently
    float causticStrength = 0.85;
    float3 finalColor = lerp(baseColor.rgb, causticColor, causticStrength);
    float4 result = float4(finalColor, baseColor.a);
    float rise = lerp(-1, 1, riseInterpolant);
    result = lerp(result, float4(0, 0, 0, 0), InverseLerp(rise, 1, TexCoord.y));
    
    return result * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
