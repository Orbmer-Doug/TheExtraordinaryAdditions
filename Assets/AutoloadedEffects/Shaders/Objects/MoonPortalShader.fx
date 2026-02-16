sampler ringTexture : register(s1);

float globalTime;
float spinScrollOffset;
matrix projection;

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
    float4 pos = mul(input.Position, projection);
    output.TextureCoordinates = input.TextureCoordinates;
    output.Position = pos;
    output.Color = input.Color;

    return output;
}

float Convert01To101(float value)
{
    return -sin(3.1415 * saturate(value)) + 1;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float glow = 1 - (smoothstep(0, 0.15, input.TextureCoordinates.y) * smoothstep(1, 0.85, input.TextureCoordinates.y));
    float bottomFade = smoothstep(1, 0.9, input.TextureCoordinates.y);
    
    return saturate(input.Color) * (tex2D(ringTexture, input.TextureCoordinates.xy * float2(-1, 1) + float2(-spinScrollOffset, 0)) + (Convert01To101(input.TextureCoordinates.y * 6.0) * .6));
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}