float globalTime;
float repeats = 1;
sampler tex : register(s1);

matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION;
    float3 TextureCoordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION;
    float3 TextureCoordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    output.Position = mul(input.Position, transformMatrix);

    return output;
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 mainColor = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float2 st = float2(coords.x * repeats, 0.25 + coords.y * 0.5);

    float3 color = tex2D(tex, st + float2(-globalTime, 0)).xyz;
    float3 color2 = tex2D(tex, st + float2(-globalTime * 1.5, 0)).xyz * 0.5;
    float4 output = float4((color + color2) * float3(mainColor.xyz) * (1.0 + color.x * 2.0), color.x * mainColor.w);
    
    return output * mainColor.a * QuadraticBump(coords.y) * 1.3;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
};