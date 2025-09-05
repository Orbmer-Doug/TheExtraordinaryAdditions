sampler tex : register(s1);
sampler tex2 : register(s2);

float globalTime : register(c0);
float3 baseColor : register(c1);
matrix transformMatrix : register(c2);

static const float edgeColorSubtraction = .3;

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
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;
    
    float scrollTime = globalTime * 1.2;
    float baseColorInterpolant = smoothstep(0.5, 0.2, tex2D(tex2, coords * float2(1, 1.9) + scrollTime * float2(-3, 0)));
    baseColorInterpolant += smoothstep(0.08, 0.01, coords.x);
    color = lerp(color, float4(baseColor, 1), saturate(baseColorInterpolant));

    float noise = tex2D(tex, coords * float2(0.8, 1.75) + float2(globalTime * -2, 0));
    
    // Calculate the edge glow, creating a strong, bright center coloration
    float distanceOffset = (tex2D(tex, coords + float2(globalTime * -2.8 + .4, 0)) - 0.5) * 0.156;
    float distanceFromCenter = distance(coords.x, 0.5) + distanceOffset;
    
    // Apply subtractive blending that gets stronger near the edges of the beam, to help with saturating the colors a bit
    color.rgb -= distanceFromCenter * edgeColorSubtraction;
    
    // Apply additive blending
    color += tex2D(tex2, coords * float2(0.9, 2) + float2(globalTime * -2.5, -globalTime)).r * color.a * (color.g + 0.35);
    
    // Apply some fast, scrolling noise to the overall result
    return color * (noise + 1.3 + step(.35, noise + (0.5 - distanceFromCenter))) * input.Color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}