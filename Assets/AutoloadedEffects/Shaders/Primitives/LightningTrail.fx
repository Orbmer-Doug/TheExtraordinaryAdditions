sampler samplerTex : register(s1);

float globalTime;
matrix transformMatrix;

// Typical vertex install
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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 st = float2(input.TextureCoordinates.x * 2, 0.25 + input.TextureCoordinates.y * 0.5);

    float3 color = tex2D(samplerTex, st + float2(globalTime, 0)).xyz;

    return float4(color * input.Color.xyz * (1.0 + color.x * 2.0), color.x * input.Color.w) * 2;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}