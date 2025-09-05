sampler tex : register(s0);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 result = tex2D(tex, coords);
    result.rgb = 1 - lerp(result.rgb, sampleColor.rgb, .5);
    return result * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}