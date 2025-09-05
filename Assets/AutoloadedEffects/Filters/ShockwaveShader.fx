sampler2D screenTex : register(s0);
float2 screenSize;
float globalTime;

float2 screenPos;
float frequency;
float intensity;
float chromatic;
float radius;
float ringSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 main = tex2D(screenTex, coords);
    float2 effectPos = screenPos / screenSize; // UV
    float2 delta = coords - effectPos;
    delta.x *= screenSize.x / screenSize.y; // Aspect ratio correction
    float dist = length(delta);

    if (intensity <= 0.0 || dist > 1)
        return main;
    
    float2 totalDistortion = float2(0.0, 0.0);
        
    // Create ring effect with thickness
    float ringThickness = ringSize * intensity;
    float ringEdge = abs(dist - radius);
    float ringStrength = smoothstep(ringThickness, 0.0, ringEdge) * frequency;
    
    // Add ripples within the ring
    float rippleFreq = frequency / radius;
    float ripple = sin(dist * rippleFreq - globalTime * 2.0) * ringStrength * intensity;
    ripple *= smoothstep(0.0, radius * 0.5, radius - dist); // Fade ripples at edges
        
    // Combine ring and ripple distortion
    float totalWave = (ringStrength + ripple) * 0.1;
    totalDistortion += (normalize(delta) * totalWave) * intensity;
    
    float2 distortedUV = coords + totalDistortion;
    float4 color = tex2D(screenTex, saturate(distortedUV));
    
    if (intensity > 0.0 && chromatic > 0.0)
    {
        float2 chromaOffset = totalDistortion * chromatic;
        color.r = tex2D(screenTex, saturate(distortedUV + chromaOffset)).r;
        color.g = tex2D(screenTex, saturate(distortedUV)).g;
        color.b = tex2D(screenTex, saturate(distortedUV - chromaOffset)).b;
    }
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}