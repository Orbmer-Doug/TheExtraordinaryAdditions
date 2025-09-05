// Inspired from: https://github.com/LucilleKarma/WrathOfTheGodsPublic/blob/5f9b71995d430f20d755e1e8a4252575524380b6/Assets/AutoloadedEffects/Shaders/Primitives/NamelessDeityCosmicLaserShader.fx

// Global variables
matrix transformMatrix;
float globalTime;
sampler2D NoiseTexture1 : register(s1);
sampler2D NoiseTexture3 : register(s2);
sampler2D NoiseTexture4 : register(s3);

// Vertex shader input and output structures
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

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float OpacityFromCutoffInterpolant(float cutoffInterpolant, float edgeOffset, float edgeCenter, float maxWidth)
{
    float edgeCutoff = 1 - pow(abs(1 - cutoffInterpolant), 10);
    float edgeOpacity = InverseLerp(maxWidth, maxWidth * 0.83, distance(edgeOffset, edgeCenter) / edgeCutoff);
    
    return edgeOpacity;
}

float4 CalculateSpaceColor(float2 coords)
{
    float time = globalTime * 0.2;
    float4 fadeMapColor1 = tex2D(NoiseTexture3, float2(coords.x * 2 - time * 1.6, coords.y));
    float4 fadeMapColor2 = tex2D(NoiseTexture3, float2(coords.x * 1 - time * 0.8, coords.y * 0.5));
    float4 fadeMapColor3 = tex2D(NoiseTexture4, float2(coords.x * 4 - time * 0.8, coords.y * 0.9));

    float opacity = (1 + fadeMapColor1.g);
    
    // Apply bloom to the resulting colors.
    float4 finalColor = float4(0.196, 0.07, 0.392, 1) * opacity;
    finalColor += fadeMapColor2 * finalColor.a;
    
    float distanceOffset = (tex2D(NoiseTexture4, coords + float2(time * -1.8 + .4, 0)) - 0.5) * 0.256;
    float distanceFromCenter = distance(coords.y, 0.5) + distanceOffset;
    float glow = pow(abs(0.25 / distanceFromCenter), 2) * QuadraticBump(coords.y) * 1;
    finalColor += fadeMapColor2 + finalColor.a * glow * fadeMapColor3;
   
    // Saturate everything a bit.
    finalColor.rgb = pow(abs(finalColor.rgb * 1.09), 3) * 0.7;
    
    return finalColor ;
}

float4 WhiteHotBeam(float2 coords)
{
    float distanceFromCenter = distance(coords.y, 0.5) + tex2D(NoiseTexture4, coords + float2(-0.3, 0.5) * globalTime * 2) * .1;
    float edgeGlow = 1.5 / pow(abs(distanceFromCenter), 0.9) * .5;
    float4 color = float4(.18, .1, .5, 1) * saturate(edgeGlow);
    
    float taper = lerp(.2, .01, coords.x);
    float glow = pow(abs(taper * pow(abs((1 - coords.x)), .4) * 2 / distanceFromCenter), 3);
    color *= glow + tex2D(NoiseTexture4, coords + float2(-0.3, 0.5) * globalTime * 2);

    return color;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 baseCoords = input.TextureCoordinates;
    
    // Warp the coords around to significantly help the illusion of a cylinder
    float pinchAmount = .75;
    float2 center = float2(.5 , .5);
    float2 offset = baseCoords - center;
    float dist = length(offset);
    float pinchFactor = 1 + pinchAmount * (dist * dist);
    float2 coords = center + offset * pinchFactor;
    
    // Calculate edge cutoff values.
    float edgeOpacity = OpacityFromCutoffInterpolant(InverseLerp(0, 0.1, coords.x), coords.y, 0.5, 0.56) * OpacityFromCutoffInterpolant(InverseLerp(1, 0.9, .1 + coords.x), coords.y, 0.5, 0.56);
    float4 baseColor = color * edgeOpacity * InverseLerp(0, 0.011, coords.x);
    
    // Alter the laser such that pixels at the edges are more squished. Also apply scrolling
    float squishFactor = 1 - saturate(pow(abs(distance(coords.y, 0.5) * 2), 15)) * 0.3;
    float2 scrolledCoords = coords + float2(coords.y * 0.015 + globalTime * -0.055, 0);
    float2 cylindricalCoords = (frac(scrolledCoords) - 0.5) * float2(1, squishFactor) + 0.5;
    
    // Make the coordinates have a bit of a sideways slant, to help reinforce the visual that the laser is a cylinder
    cylindricalCoords += float2(-0.4, 0.56) * globalTime * .4;
    
    // Calculate the texture of the laser based on the aforementioned cylindrical coordinates
    float4 spaceColor = CalculateSpaceColor(cylindricalCoords) * pow(abs(QuadraticBump(coords.y)), .8) + WhiteHotBeam(coords);
    
    return saturate(baseColor * spaceColor);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}