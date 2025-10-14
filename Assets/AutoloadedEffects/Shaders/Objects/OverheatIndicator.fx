static const float resolution = 128;
static const float quantization = 16;
static const float erasureBlend = .2;

sampler2D Texture : register(s0);
float Time : register(c0);
float Completion : register(c1);

// Samples the noise texture at the given coordinates, wrapping the UVs
float noise(float2 coord)
{
    return tex2D(Texture, frac(coord * 0.2)).x;
}

// Computes the gradient of the noise at the given position for displacement
float2 gradn(float2 position)
{
    float epsilon = 0.09;
    float gradX = noise(float2(position.x + epsilon, position.y)) - noise(float2(position.x - epsilon, position.y));
    float gradY = noise(float2(position.x, position.y + epsilon)) - noise(float2(position.x, position.y - epsilon));
    return float2(gradX, gradY);
}

// A 2D rotation matrix for the given angle
float2x2 makem2(float theta)
{
    float c = sin(theta + 1.570796327);
    float s = sin(theta);
    return float2x2(c, -s, s, c);
}

// Makes the flow value for the lava effect, creating a swirling, animated pattern
float flow(float2 position)
{
    float intensityScale = 2.0;
    float flowValue = 0.0;
    float2 basePosition = position;
    
    float scaledTime = Time * 0.1; // For smooth animation
    
    [unroll(6)]
    for (float i = 1.0; i < 8.0; i++)
    {
        position += scaledTime * 0.6;
        basePosition += scaledTime * 1.9;
        
        float2 gradient = gradn(i * position * 0.34 + scaledTime * 1.0);
        gradient = mul(gradient, makem2(scaledTime * 6.0 - (0.05 * position.x + 0.03 * position.y) * 40.0));
        
        position += gradient * 0.5;
        flowValue += (sin(noise(position) * 7.0) * 0.5 + 0.5) / intensityScale;
        
        position = lerp(basePosition, position, 0.77);
        
        intensityScale *= 1.5;
        position *= 2.0;
        basePosition *= 1.9;
    }
    
    return flowValue;
}


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelize
    coords = round(coords * resolution) / resolution;
    
    float eraseThreshold = smoothstep(Completion - erasureBlend, Completion + erasureBlend,
    smoothstep(Completion - erasureBlend, Completion + erasureBlend, 1 - coords.x)) * sampleColor.a;

    float2 position = coords - 0.5;
    position *= 2.0;
    float flowValue = flow(position);
    
    // Increase contrast for bright spots and modulate based on x-coordinate
    flowValue = pow(abs(flowValue), 1.50) * lerp(4, 0.75, 1 - pow(abs(coords.x - 1.0), 2.5));
    
    float3 lavaColor = float3(0.5, 0.2, 0.05) / flowValue;
    lavaColor = pow(abs(lavaColor), 1.5) * eraseThreshold;
    
    // Add a glow effect for cracks
    float bright = saturate(1.0 / flowValue - 1.0);
    float3 glowColor = lerp(float3(1.0, 0.4, 0.0), float3(1.0, 1.0, 0.8), bright);
    lavaColor += glowColor * pow(bright, 2.0) * .2 * eraseThreshold;
    
    // Return the color with palette limitation
    return floor(float4(lavaColor, 1 * eraseThreshold) * quantization) / quantization;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}