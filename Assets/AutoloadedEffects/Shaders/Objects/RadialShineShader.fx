sampler baseTexture : register(s0);

float globalTime : register(c0);
float4 glowColor : register(c1) = float4(1, .8, .6, 1);
float glowPower : register(c2) = .1;
float2 resolution : register(c3);
#define TwoPi 6.283185307

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = round(coords * (resolution / 2)) / (resolution / 2);
    float2 polar = float2(atan2(coords.y - 0.5, coords.x - 0.5) / TwoPi + 0.5, distance(coords, 0.5));
    
    float noiseA = tex2D(baseTexture, polar * float2(2, 0.02) + float2(0, globalTime * -0.11));
    float noiseB = tex2D(baseTexture, polar * float2(3, 0.04) + float2(0, globalTime * -0.08));
    
    float distanceGlow = smoothstep(0.5 - noiseB * 0.3, 0, polar.y);
    float baseGlow = sqrt(noiseA * noiseB) * distanceGlow * 3;
    
    float centerGlow = smoothstep(0.18, 0, polar.y) * glowPower / polar.y;
    float4 result = smoothstep(0, 0.85, pow(baseGlow, 2.4)) * sampleColor + centerGlow * glowColor * sampleColor.a;
    result.a = 0;
    
    return result;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}