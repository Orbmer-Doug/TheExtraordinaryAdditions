// Inspired from WotM by Lucille Karma: https://github.com/LucilleKarma/WrathOfTheMachines/blob/main/Assets/AutoloadedEffects/Shaders/Objects/HadesElectricBoomShader.fx

texture sampleTexture;
sampler2D baseTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float globalTime : register(c0);
float scale : register(c1); // [0, 1] value to determine the size of the explosion

static const float Pi = 3.1415926535;
static const float TwoPi = Pi * 2;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 pos : SV_Position) : COLOR0
{
    float fade = smoothstep(1.0, 0.6, scale);
    float preFade = smoothstep(0.9, 0.45, scale);
    
    float endingRadius = sqrt(scale) * 0.5;
    float distanceFromCenter = distance(coords, 0.5);
    float radiusInterpolant = smoothstep(-0.2 - preFade * .1, min(endingRadius, 0.51), distanceFromCenter);
    float angleFromCenter = atan2(coords.y - 0.5, coords.x - 0.5);
    float2 polar = float2(angleFromCenter / Pi + 0.5, distanceFromCenter);
    
    // Make a bunch of glow values
    float wavy = sin(polar.y * TwoPi - globalTime * 2.5 + angleFromCenter * 1) * sin(polar.y * -11 + globalTime * 0.48 + angleFromCenter * -5) * 0.5 + 0.5;
    float glowNoise1 = tex2D(baseTex, polar * float2(0.5, 2) + float2(globalTime * 0.3, 0.05 - globalTime * 0.25) + wavy * .4);
    float glowNoise2 = tex2D(baseTex, polar * float2(0.5, 1) + float2(-globalTime * 0.3, 0.29 - globalTime) - wavy * .025);
    float glowNoise = glowNoise1 - glowNoise2;
    float edgeAntialiasingOpacity = smoothstep(1, 0.995 - wavy * scale * 0.07, radiusInterpolant);
    
    // Combine all colors
    float4 color = pow(float4(sampleColor.rgb, 1) / radiusInterpolant, 2.25) + smoothstep(0.8, 0.99, radiusInterpolant) + glowNoise + float4(1, 1, 1, 0) * preFade * (1 - radiusInterpolant);
    
    // Brighten the edge a lot
    color += smoothstep(0.35, 0.5, distanceFromCenter) * 2.2 * preFade;
    
    // And the center
    color += float4(1, 1, 1, 1) * max(0., 1.13 - 6.1 * (distanceFromCenter / max(0.00001, scale))) * 1.8 * preFade;
   
    return color * edgeAntialiasingOpacity * sampleColor.a * fade;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}