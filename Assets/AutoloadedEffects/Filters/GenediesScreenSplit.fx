sampler screenTexture : register(s0);
sampler backgroundTexture : register(s1);

float globalTime;
float glitchIntensity;
float2 screenSize;

float splitWidth;
float2 splitCenter;
float2 splitDirection;

float SignedDistanceToLine(float2 p, float2 linePoint, float2 lineDirection)
{
    return dot(lineDirection, p - linePoint);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the distance from the screen coordinate to the lines, along with the necessary contributing brightness from that distance.
    float2 offset = 0;
    float brightnessBoost = 0;
    
    float signedLineDistance = SignedDistanceToLine(coords, splitCenter, splitDirection * float2(1, screenSize.y / screenSize.x));
    float lineDistance = abs(signedLineDistance);
    float width = splitWidth;
    width *= width >= 8 / screenSize.x;
    
    brightnessBoost += width / (lineDistance + 0.001) * 0.3;
    
    // Calculate how much both sides of the line should be shoved away from the line.
    offset += splitDirection * sign(signedLineDistance) * width * -0.5;
    
    // Calculate colors.
    float4 screen = tex2D(screenTexture, coords + offset) - brightnessBoost;
    
    // Combine colors based on how close the pixel is to the line.
    float brightness = saturate(pow(smoothstep(0.01, 0.4, brightnessBoost), 2));
    
    // Infinite mirror effect
    const int maxIterations = 15;
    float breakOutIteration = maxIterations;
    for (int i = 0; i < maxIterations; i++)
    {
        if (coords.x < 0.25 || coords.x > 0.75 || coords.y < 0.1 || coords.y > 0.75)
        {
            breakOutIteration = i;
            break;
        }

        coords = (coords - .5) * 1.25 + 0.5;
        coords.x += sin(i * 0.4 + globalTime) * 0.05;
    }
    
    float glitchStrength = glitchIntensity;
    float time = globalTime * 2.0;
    
    // Simple noise function
    float noise = frac(sin(dot(coords + globalTime * 10.0, float2(12.9898, 78.233))) * 43758.5453);
    
    // Chromatic Aberration
    float2 offsetR = float2(noise * glitchStrength, 0.0);
    float2 offsetG = float2(-noise * glitchStrength * 0.5, noise * glitchStrength * 0.3);
    float2 offsetB = float2(noise * glitchStrength * -0.7, noise * glitchStrength * -0.2);
    
    // Sample texture with offsets for each channel
    float4 colorR = tex2D(screenTexture, coords + offsetR) * (1 - breakOutIteration / maxIterations);
    float4 colorG = tex2D(screenTexture, coords + offsetG) * (1 - breakOutIteration / maxIterations);
    float4 colorB = tex2D(screenTexture, coords + offsetB) * (1 - breakOutIteration / maxIterations);
    
    // Combine channels
    float4 color = float4(colorR.r, colorG.g, colorB.b, 1.0);
    
    // Scanline effect
    float scanline = sin(coords.y * 10.0 + time) * 0.02;
    color.rgb += scanline * noise * 0.1;
    
    // Flicker effect
    float flicker = 1.0 - 0.2 * step(0.8, sin(time * 5.0) * noise);
    color.rgb *= flicker;
    
    // Add noise grain
    color.rgb += noise * 0.25;
    
    return lerp(screen, 1 - color, brightness);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}