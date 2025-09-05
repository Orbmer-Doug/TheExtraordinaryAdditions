sampler noiseTex : register(s1);
sampler fadeTex : register(s2);

static const float Pi = 3.14159265358979323846;
static const float TwoPi = Pi * 2;

float globalTime;
float scale;
float3 coolColor;
float3 mediumColor;
float3 hotColor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Scale being at 0 creates a weird box
    float fixScale = max(scale, .00000000000001);
    
    // Get some polar
    float distanceFromCenter = distance(coords, 0.5);
    float angleFromCenter = atan2(coords.y - 0.5, coords.x - 0.5) + TwoPi;
    float2 polar = float2(distanceFromCenter, angleFromCenter / Pi + 0.5);
    
    // Make two distance values that are interpolated between when calculating the edge shape of the portal
    // This creates the spawn animation when it scales up
    float noisyDistance = (tex2D(noiseTex, polar * 2 + float2(2.9, 0.4) * globalTime * .3) * 0.16 + 0.36);
    float fadeOutDistance = tex2D(fadeTex, coords * float2(.4, 1.2));
    float distanceToEdge = lerp(fadeOutDistance, noisyDistance, pow(fixScale, 4)) * fixScale;
    
    // Calculate some swirly coords instead of polar
    float swirlRotation = length(coords - .5) * 40 - globalTime * 2;
    float swirlSine = sin(swirlRotation);
    float swirlCosine = sin(swirlRotation + (Pi / 2));
    float2x2 swirlRotationMatrix = float2x2(swirlCosine, -swirlSine, swirlSine, swirlCosine);
    float2 swirlCoordinates = mul(coords - .5, swirlRotationMatrix) + 0.5;
    
    // Make some color for the swirl
    float swirlColorFade = saturate(distanceFromCenter * 3) / fixScale;
    float3 swirlBaseColor = lerp(coolColor, hotColor, pow(swirlColorFade, 0.5));
    float4 swirlNoiseColor = tex2D(noiseTex, swirlCoordinates * float2(2, 1) + float2(globalTime * 1.2, 0)) * (1 - swirlColorFade);
    float4 endColor = lerp(float4(swirlBaseColor, 0.3), 0, swirlColorFade);
    float4 swirl = lerp(0, endColor * (1 + (1 - swirlColorFade) * 1) * fixScale, saturate(swirlNoiseColor.r));
    
    // Create a glow within the portal
    float innerColorInterpolant = smoothstep(distanceToEdge, distanceToEdge * 0.7, distanceFromCenter);
    float3 swirlColor = lerp(mediumColor, hotColor, distanceToEdge * 1.5) * pow(smoothstep(distanceToEdge * 1.2, 0, distanceFromCenter), 4) * 2;
    float4 color = float4(swirlColor, 1) * innerColorInterpolant;
    
    // Combine and add a little contrast
    return saturate(color + pow(abs(swirl), 1.4)) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}