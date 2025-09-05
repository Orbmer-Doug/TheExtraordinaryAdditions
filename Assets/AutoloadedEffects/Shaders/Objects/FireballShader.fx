sampler noise : register(s1);

float3 mainColor : register(c0);
float2 resolution : register(c1);
float time : register(c2);
float opacity : register(c3);

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    coords = round(coords * resolution) / resolution;
    float2 mappedUv = float2(coords.x - .5, (1 - coords.y) - .5);
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(mappedUv) * 2;
    
    // Crop the sprite into a circle
    if (distanceFromCenter > 1)
        discard;
    
    // An intensity from the distance to the center
    float mainOpacity = max(0, 1 - distanceFromCenter);
    
    // Make the opacity increase exponentially towards the center
    mainOpacity = lerp(mainOpacity, mainOpacity / pow(distanceFromCenter / 0.8, 2.5), distanceFromCenter < .8);
    
    float2 coordsNormalizedToCenter = coords * 2 - 1;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 2.145;
    float2 sphereCoords = coords * spherePinchFactor;
    
    // Get the texture coords from the modified coords
    float noiseAmount = tex2D(noise, float2(sphereCoords.x, sphereCoords.y + time * .9)).r;
    float noiseAmount2 = tex2D(noise, float2(sphereCoords.x, sphereCoords.y - time * .5)).r;
    float noiseAmount3 = tex2D(noise, float2(sphereCoords.x + time * .3, sphereCoords.y)).r;
    float noiseAmount4 = tex2D(noise, float2(sphereCoords.x - time * .7, sphereCoords.y)).r;
    float finalNoiseAmount = noiseAmount * .2 + noiseAmount2 * .3 + noiseAmount3 * .2 + noiseAmount4 * .3;
    
    // Modify the opacity
    finalNoiseAmount *= mainOpacity;
    mainOpacity *= mainOpacity;
    mainOpacity /= finalNoiseAmount;

    // Fade the edges
    mainOpacity = lerp(mainOpacity, mainOpacity * pow((1 - ((distanceFromCenter - .6) / .4)), 3), distanceFromCenter > .6);
    
    // Modify the color by the opacity, toned down and clamped to allow for more of the main color to show
    float3 color = mainColor * clamp(pow(mainOpacity, .5), 0, 2.7);
    color *= float3(finalNoiseAmount, finalNoiseAmount, finalNoiseAmount) * .5;
    return float4(color, 0) * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}