sampler baseTex : register(s0);
sampler noiseTex : register(s1);
sampler distanceNoiseTex : register(s2);

float globalTime;
float posterizationPrecision;
float2 pixelationFactor;

const static float TwoPi = 6.283185307;
const static float edgeBrightness = 0.03;
const static float sphereBrightness = 0.636;

float CalculateOuterGlowIntensity(float2 coords, float distanceFromCenter)
{
    // This evaluates how far away the distance from the center is relative to a given radius
    float distanceFromEdge = distance(distanceFromCenter, 0.29);
    return edgeBrightness / distanceFromEdge * smoothstep(0.4, 0.25, distanceFromCenter);
}

float CalculateInnerGlowIntensity(float2 coords, float distanceFromCenter)
{
    // Apply the same techniques as with the outer glow, to create an inner circle of light
    float glowIntensity = sphereBrightness / distanceFromCenter;
    glowIntensity *= smoothstep(0.08, 0, distanceFromCenter);
    
    return glowIntensity;
}

float CalculateStormIntensity(float2 coords, float distanceFromCenter)
{
    // Polar coords to allow flowing out from the center
    float2 polar = float2(distanceFromCenter, atan2(coords.y - 0.5, coords.x - 0.5) / TwoPi + 0.5);
    
    // Make the angle part of the polar coordinates loop 2 times across the circle instead of 1, giving a more line-y look
    // Additionally loop the x axis 5 times to thicken it up
    polar.y *= 2;
    polar.x *= 5;
    
    // Use two noise values instead of one, with differing fast scrolls
    float noiseA = tex2D(noiseTex, polar + float2(globalTime * -1.4, 0) - float2(0, polar.x));
    float noiseB = tex2D(noiseTex, polar * float2(0.5, 2) + globalTime * float2(-1.2, 0.1) + float2(0, polar.x));
    float stormIntensity = (noiseA + noiseB) * 0.6;
    
    // Sharpen and intensify
    stormIntensity = pow(stormIntensity, 2) * 2;
    
    // Ensure that the effect tapers off by distance to not go beyond the circle
    stormIntensity *= smoothstep(0.3, 0.2, distanceFromCenter);

    return stormIntensity;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords -= coords % (1 / pixelationFactor);
    
    float distanceFromCenter = distance(coords, 0.5);
    float2 polar = float2(distanceFromCenter, atan2(coords.y - 0.5, coords.x - 0.5) / TwoPi + 0.5);
    float distanceOffset = tex2D(distanceNoiseTex, polar * float2(3, 4) + globalTime * float2(-2.6, 0)).x * 0.025;
    distanceFromCenter += distanceOffset;
    
    // Combine the two base glows
    float outerGlowIntensity = CalculateOuterGlowIntensity(coords, distanceFromCenter);
    float innerGlowIntensity = CalculateInnerGlowIntensity(coords, distanceFromCenter);
    
    float electricityIntensity = CalculateStormIntensity(coords, distanceFromCenter);
    
    // It appears multiplying by the inner glow gives it a dark storm like field around it with a glowing eye and adding gives a bright border akin to the light shockwave shader
    // In this case we will multiply to keep to the dark eldritch theme
    float4 glowColor = outerGlowIntensity * sampleColor 
    * innerGlowIntensity + electricityIntensity;
    
    // Apply some post processing steps to mix with colors, particulary mixing to darker blacks and reds
    glowColor += float4(-.2, -1, -1.5, 0) * smoothstep(0.4, 0, distanceFromCenter) * 0.6;
    glowColor += float4(-1, -.15, -1, 0) * smoothstep(0.2, 0, distanceFromCenter) * 0.7;
    
    // Make translucent parts red
    glowColor += float4(.45, -.5, -1, 1) * (1 - glowColor.a) * smoothstep(0.33, 0.2, distanceFromCenter) * .9;
    
    glowColor = round(glowColor * posterizationPrecision) / posterizationPrecision;
    
    return clamp(glowColor, 0, 2) * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}