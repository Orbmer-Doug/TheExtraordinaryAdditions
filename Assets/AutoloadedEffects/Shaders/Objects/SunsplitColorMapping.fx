// Uniforms
sampler2D HeatMap : register(s0); // Current heat map (Shadertoy’s iChannel0)

// Pixel shader
float4 ColorMappingPS(float2 uv : TEXCOORD0) : COLOR0
{
    // Sample temperature
    float4 fragColor = tex2D(HeatMap, uv);
    float temp = fragColor.x; // Temperature is in red channel
    
    // Define gradient keypoints
    float4 gradient[4];
    gradient[0] = float4(0.0, 0.0, 0.0, 0.0); // Black (cold, fully transparent)
    gradient[1] = float4(1.0, 0.0, 0.0, 0.3); // Red (slightly transparent)
    gradient[2] = float4(1.0, 1.0, 0.0, 0.6); // Yellow
    gradient[3] = float4(1.0, 1.0, 1.0, 1.0); // White (hot, fully opaque)
    
    // Interpolate based on temperature
    if (temp < 0.15)
    {
        fragColor = lerp(gradient[0], gradient[1], temp / 0.15);
    }
    else if (temp < 0.35)
    {
        fragColor = lerp(gradient[1], gradient[2], (temp - 0.15) / 0.20);
    }
    else
    {
        fragColor = lerp(gradient[2], gradient[3], (temp - 0.35) / 0.65);
    }
    
    return fragColor;
}
// Technique
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 ColorMappingPS();
    }
}