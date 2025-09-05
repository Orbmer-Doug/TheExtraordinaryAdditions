static const float edgeColorSubtraction = .3;
static const float edgeGlowIntensity = .1;
static const float glowIntensity = .2;

sampler tex1 : register(s1);
sampler tex2 : register(s2);

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    float noise = tex2D(tex1, coords * float2(0.8, 1.75) + float2(globalTime * -2, 0));
    
    // Calculate the edge glow, creating a strong, bright center coloration
    float distanceOffset = (tex2D(tex1, coords + float2(globalTime * -2.8 + .4, 0)) - 0.5) * 0.156;
    float distanceFromCenter = distance(coords.y, 0.5) + distanceOffset;
    float edgeGlow = edgeGlowIntensity / pow(abs(distanceFromCenter), 0.9);
    color = saturate(color * edgeGlow);
    
    // Apply subtractive blending that gets stronger near the edges of the beam, to help with saturating the colors a bit
    color.rgb -= distanceFromCenter * edgeColorSubtraction;
    
    // Apply additive blending
    color += tex2D(tex2, coords * float2(0.9, 2) + float2(globalTime * -2.5, -globalTime)).r * color.a * (color.g + 0.35);
    
    // Fade at the edges
    color *= smoothstep(0.5, 0.3, distanceFromCenter);
    
    // Brighten the center tremendously
    color += color.a / smoothstep(0, glowIntensity, distanceFromCenter) * 0.25;
    
    // Fade out at the bottom
    color = saturate(color) * smoothstep(0.01, .03, coords.x - noise * 0.02);
    
    // Fade at the lasers end
    float endOfLaserFade = smoothstep(0.98, 0.9 + noise * 0.06, coords.x);
    color *= endOfLaserFade;
    
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
