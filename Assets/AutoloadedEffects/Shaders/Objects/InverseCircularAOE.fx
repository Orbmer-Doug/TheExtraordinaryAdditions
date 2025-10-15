sampler tex1 : register(s1);

float3 MainColor;
float3 OuterColor;
float brightness;

float globalTime;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float3 color = MainColor;
    
    // This is the center of the sprite, coords work from 0 - 1
    float2 center = float2(0.5, 0.5);
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(coords - center) * 2;
    
    // Crop the sprite into a circle
    if (distanceFromCenter < .25 || distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    float opacity = 0.1 + (0.1 + 0.35 * pow(brightness, 2)) * (1.5 - distanceFromCenter);
    opacity = lerp(opacity, 1, pow(brightness, 2)) * lerp(opacity, 1, min(brightness * 1.25, min(opacity + 0.1, 1)));

    // Outline the center
    if (distanceFromCenter < .255)
    {
        // Make the color get closer to the outline color
        color = lerp(color, OuterColor, min(brightness * 2, 1));
    }
    else
    {
        color *= tex2D(tex1, sin(coords + globalTime * .4)).r * opacity;
    }
    color *= opacity;
    
    float4 finalColor = float4(color, opacity);
    
    return finalColor * 1.5;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}