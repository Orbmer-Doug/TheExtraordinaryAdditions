sampler baseTexture : register(s0);
sampler electricityNoiseTexture : register(s1);
sampler distanceNoiseTexture : register(s2);

float globalTime;
float posterizationPrecision;
float2 pixelationFactor;

const static float TwoPi = 6.283185307;

float CalculateOuterGlowIntensity(float2 coords, float distanceFromCenter)
{
    // Rewrite the outer glow intensity to be more explicitly defined in terms of the edge.
    // This evaluates how far away the distance from the center is relative to a given radius.
    // If the distance from the center is 0.24, for example, then this evaluates to 0.01, since it's 0.01 units away from 0.25.
    // This makes the results feel less foggy than they were before.
    float edgeBrightness = 0.03;
    float distanceFromEdge = distance(distanceFromCenter, 0.29);
    return edgeBrightness / distanceFromEdge * smoothstep(0.4, 0.25, distanceFromCenter);
}

float CalculateInnerGlowIntensity(float2 coords, float distanceFromCenter)
{
    // Apply the same techniques as with the outer glow, to create an inner circle of light.
    float sphereBrightness = 0.636;
    
    float glowIntensity = sphereBrightness / distanceFromCenter;
    glowIntensity *= smoothstep(0.08, 0, distanceFromCenter);
    
    return glowIntensity;
}

float2 ConvertToPolar(in float2 coords)
{
    coords -= 0.5;
    float r = sqrt(coords.x * coords.x + coords.y * coords.y);
    float theta = atan(coords.y / coords.x);

    return float2(r, theta);
}

float CalculateStormIntensity(float2 coords, float distanceFromCenter)
{
    // This allows for scrolling to happen based on distance and angle, rather than X/Y position, allowing for an "outflow" effect.
    float2 polar = float2(distanceFromCenter, atan2(coords.y - 0.5, coords.x - 0.5) / TwoPi + 0.5);
    
    // Make the angle part of the polar coordinates loop 2 times across the circle instead of 1, giving a more line-y look.
    // Additionally loop the x axis 5 times to thicken it up
    polar.y *= 2;
    polar.x *= 5;
    
    // Use two noise values instead of one, with differing fast scrolls.
    float noiseA = tex2D(electricityNoiseTexture, polar + float2(globalTime * -1.4, 0) - float2(0, polar.x));
    float noiseB = tex2D(electricityNoiseTexture, polar * float2(0.5, 2) + globalTime * float2(-1.2, 0.1) + float2(0, polar.x));
    float stormIntensity = (noiseA + noiseB) * 0.6;
    
    // Sharpen and intensify.
    stormIntensity = pow(stormIntensity, 2) * 2;
    
    // Ensure that the electricity effect tapers off by distance, to ensure that it doesn't appear beyond the edge of the circle.
    stormIntensity *= smoothstep(0.3, 0.2, distanceFromCenter);

    return stormIntensity;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords -= coords % (1 / pixelationFactor);
    
    // Calculate the distance from the center at the start, rather than multiple times in function areas, and apply a bit of noise to it to make the successive calculations a bit more rough, and not as
    // mathematically perfect. This only affects the distance calculations by a maximum of 0.02 (where the overall circle has a radius of 0.5), ensuring that the effect is noticeable but subtle.
    // This also uses the same polar calculation from before, to ensure that the distance offset travels in an outflow manner.
    float distanceFromCenter = distance(coords, 0.5);
    float2 polar = float2(distanceFromCenter, atan2(coords.y - 0.5, coords.x - 0.5) / TwoPi + 0.5);
    float distanceOffset = tex2D(distanceNoiseTexture, polar * float2(3, 4) + globalTime * float2(-2.6, 0)).x * 0.025;
    distanceFromCenter += distanceOffset;
    
    // Combine the two base glow intensity values by adding them.
    // Since they both occupy different parts in the rendering process (the outer edge versus the inner core), this addition will not interfere
    // with either calculation, since only one of the two terms should ever be above zero.
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