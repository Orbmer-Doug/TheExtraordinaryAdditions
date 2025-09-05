sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

matrix transformMatrix;
float globalTime;
int paletteLimit = 14;

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

static const float PI = 3.1415;
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;

    float horizontalDistanceFromCenter = distance(coords.y, 0.5);
    
    float4 fadeMapColor = tex2D(uImage1, coords + float2(globalTime * -5.4, 0));

    float distortion = lerp(-1.1, 1.1, tex2D(uImage1, coords + float2(0, globalTime * sign(coords.y > 0.5 ? -1 : 1) * 1.21)).r);
    float opacity = pow(sin((coords.y + (distortion * .1)) * PI), distortion * .95 + 2.5);
    color *= opacity;
    
    float glow = pow(0.12 * pow((1 - coords.x), 1.9) / horizontalDistanceFromCenter, 2);
    color += glow * color.a;
    color *= abs(smoothstep(.4, .1, horizontalDistanceFromCenter / (1.25 - coords.x)) * 1.5);
    color = floor(color * paletteLimit) / paletteLimit;

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