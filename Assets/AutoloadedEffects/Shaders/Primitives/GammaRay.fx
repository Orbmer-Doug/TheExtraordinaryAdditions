// Global variables
matrix transformMatrix;
float time;

sampler tex : register(s1);
static const float glowIntensity = 2;

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
    VertexShaderOutput output = (VertexShaderOutput)0;
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
    
    float t = time * .05;
    float distanceOffset = (tex2D(tex, coords * float2(.5, 2) + float2(t * -2.8 + .4, 0)) - 0.5) * 0.256;
    float distanceFromCenter = distance(coords.y, 0.5) + distanceOffset;
    float4 noise = tex2D(tex, coords + float2(t * -4, 0));
    
    // Help with contrast
    color.rgb -= distanceFromCenter * .2;
    
    // Fade at edges
    float endFade = smoothstep(0.98, 0.9 + (noise * .04), coords.x);
    color *= smoothstep(0.5, 0.3, distanceFromCenter);
    color *= endFade;

    // Add a hot white center
    float glow = pow(0.25 / distanceFromCenter, 2) * glowIntensity * ((1 - coords.x) * 4);
    float flare = sin(time) * 0.3 + 1.0;
    glow *= flare;
    
    // Apply glow
    color = saturate(color * glow);
    
    // Apply some more noise for added texture
    color *= (noise + 1 + step(0.4, noise + (0.5 - distanceFromCenter)));
    
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