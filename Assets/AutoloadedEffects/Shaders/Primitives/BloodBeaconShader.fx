sampler tex1 : register(s1);
sampler tex2 : register(s2);
float globalTime;
float saturation = 1;
matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, transformMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

static const float PI = 3.141;
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
        
    // Read the fade map as a streak
    float4 fadeMapColor = tex2D(tex1, float2(frac(coords.y), frac(coords.x - globalTime * 1.4)) * saturation);
    fadeMapColor.r *= pow(coords.x, 0.04);
    float streakBrightness = tex2D(tex2, float2(frac(coords.x - globalTime * 10), coords.y)).r;
    
    float opacity = lerp(1.45, 1.95, fadeMapColor.r) * color.a;
    opacity *= pow(sin(coords.y * PI), lerp(1, 6, pow(coords.x, 2)));
    opacity *= pow(sin(coords.x * PI), 0.4);
    opacity *= fadeMapColor.r * 1.5 + 1;
    opacity *= lerp(0.4, 0.9, fadeMapColor.r);
    
    color *= lerp(.5, 1.2, streakBrightness);
    color *= opacity;

    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
