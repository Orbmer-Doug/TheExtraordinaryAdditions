sampler noiseTex : register(s0);
sampler noiseTex2 : register(s1);
sampler noiseTex3 : register(s2);
float3 firstColor;
float3 secondaryColor;
float3 tertiaryColor;
float globalTime;
matrix transformMatrix;

int detail = 2; // The less this number is the more detail the shader will give

float trailSpeed = .45f;

bool flip;

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

static const float PiOver2 = 1.570796327;
static const float Pi = 3.14159265359;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float Sin01(float x)
{
    return sin(x) * .5f + .5f;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    if (flip)
        coords.y = 1 - coords.y;
    
    // Move against the starting point of the slash
    float2 noiseDetail = float2(detail, detail);
    float2 noiseCoords = coords * float2(1.0 / noiseDetail.x, noiseDetail.y) - float2(globalTime * trailSpeed, 0);
    
    // Smooth the trail progression
    noiseCoords.x = Sin01(noiseCoords.x * 4);
    
    // Create varied noises for detail
    float noise = tex2D(noiseTex, noiseCoords).r;
    float noise2 = abs(tex2D(noiseTex, noiseCoords * 1.2).r * 1.6);
    float noise3 = abs(tex2D(noiseTex, noiseCoords * 1.1).r * 1.3);

    // Read the fade map as a streak
    float4 fadeMapColor = tex2D(noiseTex, float2(frac(coords.y + sin(globalTime + PiOver2) * 0.01), frac(coords.x - globalTime * 1.4)));
    fadeMapColor.r *= abs(coords.x * 10.2);
    
    // Base opacity calculation with smoother tapering
    float opacity = lerp(1.25, 1.95, fadeMapColor.r) * color.a;
    opacity *= pow(abs(sin(coords.y * Pi)), 1.5) * lerp(1, 6, pow(abs(coords.x), 2.5));
    opacity *= sin(coords.x * Pi) * 1.2;
    opacity *= (fadeMapColor.r * 1.2 + 0.8);
    
    // Smooth tapering along the trail length
    float trailFade = pow(abs(1.0 - coords.x), 1.5);
    opacity *= trailFade;

    // Taper toward the tip of the blade
    float tipTaper = InverseLerp(0.3, 1.0, coords.y);
    opacity *= (1.0 - tipTaper * 0.8);

    // Soften noise application
    opacity *= (noise * 1.8 + 0.2) * saturate((1 - coords.x) - noise * coords.y * 0.3) * 1.5;
    opacity *= InverseLerp(0, coords.y, saturate(1 - coords.x));
    
    float blendInterpolant = tex2D(noiseTex, coords * float2(1, 4) + float2(globalTime * -trailSpeed * 4, 0));
    float3 whiteHotColor = float3(1.0, 1.0, 1.0);
    
    // Create a heat gradient
    float heatFactor = pow(coords.y, .5);
    float3 heatColor = lerp(tertiaryColor, whiteHotColor, heatFactor);

    // Start with the heat color as the base
    color = float4(heatColor, 1);
    
    // Fade to the second primary color based on noise, blending with the heat effect and adding a detailed dark effect
    color = lerp(color, float4(firstColor, 1), blendInterpolant * 7.7);
    
    // Create dark colors with softer transitions
    float darkColorWeight = saturate(coords.y * 1.0 + coords.x * 0.4 + noise * 0.15);
    color = lerp(color, float4(secondaryColor, 1), darkColorWeight);

    // Adjust edge streak toward the top of the blade
    float edgeWeight = InverseLerp(.3, 0, coords.x) * (1.0 - coords.y * PiOver2);
    edgeWeight *= trailFade;
    color = lerp(color, float4(tertiaryColor, 1), edgeWeight);
    
    // Finalize the color with softened randomness
    float4 finalColor = color * opacity * input.Color.a * (noise3 * 3.0 + 2.0) * 1.2;
    
    // Quantization, 16 is usually a good number
    finalColor = floor(finalColor * 16) / 16;
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}