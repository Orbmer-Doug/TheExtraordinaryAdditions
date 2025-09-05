sampler tex : register(s1);
sampler tex2 : register(s2);

float globalTime;
float repeats;
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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 mainColor = input.Color;
    float2 coords = input.TextureCoordinates;

    float2 st = float2(coords.x * repeats, 0.25 + coords.y * 0.5);
    float distanceFromCenter = distance(coords.y, 0.5);
    
    float3 color = tex2D(tex, st + float2(globalTime, 0)).xyz;
    float3 color2 = tex2D(tex, st + float2(-globalTime * 1.15, 0)).xyz * 0.5;

    float sam = tex2D(tex2, float2(st.y, globalTime * .5)).x;
    float4 output = float4((color + color2) * float3(mainColor.xyz) * (1.0 + color.x * 2.0), color.x * mainColor.w);
    output *= sam + mainColor.g;
    
    float edgeGlow = .8 / pow(distanceFromCenter, 1.4) * 1.2;
    output = saturate(output * edgeGlow);
    
    return output;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
};