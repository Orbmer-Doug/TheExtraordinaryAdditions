sampler tex : register(s1);
matrix transformMatrix;
float globalTime;

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

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float distanceOffset = (tex2D(tex, coords + float2(globalTime * -2.8 + 1, 0)) - 0.5) * .5;
    float distanceFromCenter = distance(coords.y, 0.5) + distanceOffset;
    float glow = pow(abs(0.25 / distanceFromCenter), 1.5) * QuadraticBump(coords.y) * .92 * 1.5;
    
    // Ensure all colors a possible
    color += glow * 0.001;
    
    return color * glow;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
