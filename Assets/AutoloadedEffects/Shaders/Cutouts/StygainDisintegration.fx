sampler tex : register(s0);
sampler noise : register(s1);
float opacity;
float globalTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(tex, coords);
    
    if (!any(color))
        discard;
    
    float4 erasure = tex2D(noise, coords + float2(0, globalTime * .1));

    if (erasure.g > opacity * 1.1)
        discard;
    else if (erasure.g > opacity * .96) // Pseudo-edge detection
        return float4(1, .1, .2, 1) * .7;
    
    return sampleColor * color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}