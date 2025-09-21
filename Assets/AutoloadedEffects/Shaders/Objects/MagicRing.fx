sampler ring : register(s0);
sampler noise : register(s1);
float3 firstCol;
float3 secondCol;
float opacity;
float time;
float cosine;
static const float fade = .65;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2x2 rotate = float2x2(cosine, -sin(time), sin(time), cosine);
    float4 baseColor = tex2D(ring, uv);

    float colorFade = abs(sin(uv.x + time * 0.5));
    float2 coords = mul((uv + float2(-.5, -.5)), rotate) + float2(.5, .5);
    float3 color = firstCol * tex2D(ring, coords).xyz;
    float luminosity = (color.r + color.g + color.b) / 3;
    float4 endColor = float4(lerp(firstCol, secondCol, colorFade), 1);
    endColor *= opacity + luminosity * 0.5;
    float4 final = float4(color, 1.0 * opacity - uv.x * fade) * endColor;
    
    return final * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}