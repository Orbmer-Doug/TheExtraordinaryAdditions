sampler noiseTex : register(s1);
sampler fadeTex : register(s2);

#define pi 3.141592654
#define twopi 6.283185307

#define swirlStrength 1.1
#define swirlSpeed 0.7
#define swirlFalloff 3.0

float globalTime;
float scale;
float3 coolColor = float3(.0, .62, .2);
float3 mediumColor = float3(91. / 255., 1., 1.);
float3 hotColor = float3(181. / 255., 1., 1.);

matrix vertexMatrix;

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
    float4 pos = mul(input.Position, vertexMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;
    coords *= 1.25;
    coords -= float2(.125, .125);
    
    // Scale being at 0 creates a weird box
    float fixScale = max(scale, .00000000000001);
    
    // Get some polar
    float distanceFromCenter = distance(coords, 0.5);
    float angleFromCenter = atan2(coords.y - 0.5, coords.x - 0.5) + twopi;
    
    float angleOffset = swirlStrength * (1.0 / (distanceFromCenter * 4.0 + 0.15)) * sin(globalTime * swirlSpeed + distanceFromCenter * 14.0);
    float swirledAngle = angleFromCenter + angleOffset;
    float2 swirlPolar = float2(distanceFromCenter, swirledAngle / pi + 0.5);
    
    // Make two distance values that are interpolated between when calculating the edge shape of the portal
    // This creates the spawn animation when it scales up
    float noisyDistance = (tex2D(noiseTex, swirlPolar * .5 + float2(.9, 0.4) * globalTime * .3) 
    * 0.26 + 0.36).x;
    float fadeOutDistance = tex2D(fadeTex, coords * float2(.4, .2)).x;
    float distanceToEdge = lerp(fadeOutDistance, noisyDistance, pow(fixScale, 4.)) * fixScale;
    
    // Create a glow within the portal
    float innerColorInterpolant = smoothstep(distanceToEdge, distanceToEdge * 0.7, distanceFromCenter);
    float3 swirlColor = lerp(coolColor, hotColor, distanceToEdge * 1.5) 
    * pow(smoothstep(0., distanceToEdge * 1.2, distanceFromCenter), 1.5) * 2.5;
    
    float4 color = float4(0.0, 0., 0., 0.);
    color += float4(lerp(coolColor, mediumColor, distanceToEdge * 1.5) 
    * pow(smoothstep(0., distanceToEdge * 1.2, distanceFromCenter), 2.5) * 3.5, 1.);
    color += float4(swirlColor, 1.);
    color *= innerColorInterpolant;
    
    // Combine and add a little contrast
    return pow(color, 1.4);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}