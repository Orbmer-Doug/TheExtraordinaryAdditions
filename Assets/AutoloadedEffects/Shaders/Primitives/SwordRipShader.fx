sampler tex : register(s1);
float globalTime;
bool flip;
matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
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
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float y = lerp(coords.y, 1 - coords.y, flip);
    float slashMapColor = tex2D(tex, coords);
    float opacity = 1 - slashMapColor.r;
    color = lerp(color, input.Color * 5, y);
    color.rgb -= slashMapColor * 1.2;
    color *= pow(opacity, 3);
    color *= smoothstep(1, 0, coords.x);
    color = pow(color, 2.2);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
