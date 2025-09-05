sampler screenTexture : register(s0);

float intensity;
float2 screenPos; // Center of effect in screen space (pixels)
float2 screenSize; // Screen resolution (width, height in pixels)
float radius; // Max radius of effect in UV space (e.g., 0.05)
float globalTime;
float warp = 20;

// Random number generator based on sine
float random(float2 coords)
{
    float2 r = frac(sin(coords) * 2.7644437);
    return frac(r.y * 276.44437 + r.x);
}

// For pseudo-random values
float hash(float value)
{
    return frac(sin(value) * 43758.5453123);
}

float noise(float2 coords)
{
    float2 integerPart = floor(coords);
    float2 fractionalPart = frac(coords);
    
    // Sample noise at four corners
    float a = random(integerPart);
    float b = random(integerPart + float2(1, 0));
    float c = random(integerPart + float2(0, 1));
    float d = random(integerPart + float2(1, 1));
    
    // Smooth interpolation
    float2 u = fractionalPart * fractionalPart * (3 - 2 * fractionalPart);
    
    return lerp(a, b, u.x) + (c - a) * u.y * (1 - u.x) + (d - b) * u.x * u.y;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 effectPos = screenPos / screenSize;

    // Calculate distance from center in UV space, adjusted for aspect ratio
    float2 adjustedDelta = coords - effectPos;
    adjustedDelta.x *= screenSize.x / screenSize.y; // Correct for aspect ratio
    float dist = length(adjustedDelta) / radius; // Normalized distance from center

    if (dist > 1.0)
    {
        return tex2D(screenTexture, coords);
    }
    
    float fallOff = (1 - dist);

    float2 noiseCoords = coords * warp + float2(0., -globalTime * 2.5); // Scale and animate noise
    float noiseValue = noise(noiseCoords) * fallOff;
    float noiseValue2 = noise(noiseCoords + float2(1.5, 2.5)) * fallOff; // Second noise for variation

    // Calculate distortion offset
    float2 distortion = float2(noiseValue - 0.5, noiseValue2 - 0.5) * intensity * .01 * fallOff;
    
    // Combine distortion
    float2 distortedCoords = effectPos + (coords - effectPos) + distortion;
    
    return tex2D(screenTexture, distortedCoords);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}