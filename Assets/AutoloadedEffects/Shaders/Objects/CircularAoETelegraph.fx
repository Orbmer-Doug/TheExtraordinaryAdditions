float3 color; // Color of the telegraph zone
float3 secondColor; // Color of the outline
float opacity;
float completion;
float globalTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float3 finalColor = color;
    
    float2 center = float2(0.5, 0.5);
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(coords - center) * 2;
    
    // Crop the sprite into a circle
    if (distanceFromCenter > 1)
        discard;
    
    // Make the brightness increase towards the edge
    float opac = 0.1 + (0.2 + 0.35 * pow(completion, 2)) * distanceFromCenter;
    
    // Brighten the very edge
    if (distanceFromCenter > 0.995)
    {
        // Make the color get closer to the outline color. At halfway completion the transition is complete (so proud of them)
        finalColor = lerp(color, secondColor, min(completion * 2, 1));
        
        // Do the same for the opacity
        opac = lerp(opac, 1, pow(completion, 2)) * lerp(opac, 1, min(completion * 1.25, min(opac + 0.1, 1)));
    }
    
    else
        opac = opac * opac;
    
    finalColor = color * opac;
    
    return float4(finalColor, opac) * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}