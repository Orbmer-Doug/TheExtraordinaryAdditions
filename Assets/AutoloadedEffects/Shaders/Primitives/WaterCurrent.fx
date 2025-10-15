sampler streakTexture : register(s1);

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

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;

    // Read the fade map as a streak
    float4 fadeMapColor = tex2D(streakTexture, float2(frac(coords.x * 2 + (globalTime + 1.57)), frac(coords.y * .8 + globalTime * 1.4)));
    fadeMapColor.r *= saturate(pow(coords.x, .1));

    float opacity = fadeMapColor.r;
    float power = lerp(3, 4, coords.x);
    opacity = abs(lerp(pow(QuadraticBump(coords.y), power), opacity, coords.x));
    opacity *= sin(coords.x * 3.141);
    opacity *= pow(sin(coords.y * 3.141), 1.2);
    opacity *= fadeMapColor.r * 1.5 + 1.5;
    
    // Make colors lean toward blue
    float3 transformColor = lerp(float3(60 / 255.0, 67 / 255.0, 118 / 255.0), float3(147 / 255.0, 177 / 255.0, 1), fadeMapColor.r);
    color.rgb = lerp(color.rgb, transformColor, .5);
    
    return color * opacity * 1.4 * input.Color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
