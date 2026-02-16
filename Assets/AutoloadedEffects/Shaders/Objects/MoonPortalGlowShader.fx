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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    input.TextureCoordinates.y *= 6.0;
    float baseNoise = tex2D(ringTexture, input.TextureCoordinates.xy * float2(1.2, 0) + float2(globalTime * 0.15, 0.5));
    float glow = 0.42 + pow(baseNoise, 5) * 1.1;
    float verticalGlowPower = 0.96 + (1 - baseNoise) * 2;
    float verticalGlow = smoothstep(1, 0.1, 1- input.TextureCoordinates.y) * pow(1 - input.TextureCoordinates.y, verticalGlowPower);
    return saturate(glow * verticalGlow) * input.Color * 4.;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}