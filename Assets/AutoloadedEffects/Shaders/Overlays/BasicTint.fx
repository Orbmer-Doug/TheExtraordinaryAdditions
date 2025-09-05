sampler tex : register(s0);
float3 color;
float opacity;

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 result = tex2D(tex, coords);
    float originalAlpha = result.a;
    result.rgb = lerp(result.rgb, color, .6);
    return result * originalAlpha;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 Recolor();
    }
}