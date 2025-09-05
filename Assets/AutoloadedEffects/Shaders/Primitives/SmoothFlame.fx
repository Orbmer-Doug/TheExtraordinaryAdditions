sampler whiteHotNoiseTexture : register(s1);

uniform float globalTime;
uniform float heatInterpolant;
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

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    float whiteHotNoise = 1 - tex2D(whiteHotNoiseTexture, coords + float2(globalTime * -5.4, 0));
    float whiteHotBrightness = pow(QuadraticBump(coords.y) * (1 - coords.x), 5) * whiteHotNoise * lerp(0.5, 3.3, heatInterpolant);
    
    float edgeOpacity = QuadraticBump(coords.y);
    float4 color = input.Color * edgeOpacity * 1.6;
    
    color -= float4(0.01, 0.3, 0.08, 0) * tex2D(whiteHotNoiseTexture, coords * float2(0.7, 2) + float2(globalTime * -3.1, 0.16)) * color.a;
    
    return color + whiteHotBrightness * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}