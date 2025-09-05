/*sampler uImage1 : register(s1);
float globalTime;
matrix transformMatrix;
float globalTimeFactor;
float coordZoom = 1;
float saturation = 1;
*/
bool useOutline;
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float globalTime;
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

float InverseLerp(float x, float min, float max)
{
    return saturate((x - min) / (max - min));
}

float4 StarColorFunction(float2 coords)
{
    float timeFactor = 4;
    float4 c1 = tex2D(uImage0, coords + float2(sin(globalTime * timeFactor * 0.12) * 0.5, globalTime * timeFactor * 0.03));
    float4 c2 = tex2D(uImage1, coords + float2(globalTime * timeFactor * -0.019, sin(globalTime * timeFactor * -0.09 + 0.754) * 0.6));
    return pow(c1 + c2, .8);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    float zoom = 2;
    float sat = 0;
    float4 color = StarColorFunction(coords * float2(1, 0.1) * (zoom + 1)) * input.Color;
    
    float bloomPulse = sin(globalTime * 7.1 - coords.x * 12.55) * 0.5 + 0.5;
    float opacity = pow(sin(3.141 * coords.y), 4 - bloomPulse * 2);
    
    return color * opacity * (sat + 1) * 1.6;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
