// Uniforms
sampler2D PreviousHeatMap : register(s0); // Previous frame’s heat map (Shadertoy’s iChannel0)
sampler2D NoiseTexture : register(s1); // Noise texture (Shadertoy’s iChannel1)
float2 Resolution; // Screen resolution (Shadertoy’s iResolution)
float2 MousePos; // Mouse position in UV space (Shadertoy’s iMouse.xy)
float MouseDown; // Mouse button state (Shadertoy’s iMouse.z)
float Time; // Animation time (Shadertoy’s iTime)
float Diff; // Diffusion step (0.008 in Shadertoy)
float2 AimDirection;

// Pixel shader
float4 HeatSimulationPS(float2 uv : TEXCOORD0) : COLOR0
{
    // Initialize to 0 temperature
    float4 fragColor = float4(0.0, 0.0, 0.0, 1.0);
    
    // Convection: Average temperatures from pixels above (adjusted for upward motion)
    float2 dirOffset = AimDirection * 3.0 * Diff; // Offset in the aim direction
    fragColor = (
        tex2D(PreviousHeatMap, uv + dirOffset) +
        tex2D(PreviousHeatMap, uv) +
        tex2D(PreviousHeatMap, uv + dirOffset + float2(-Diff, 0)) +
        tex2D(PreviousHeatMap, uv + dirOffset + float2(Diff, 0))
    ) / 4.0;
    
    // Cooling based on noise texture (moving with time)
    float noise = tex2D(NoiseTexture, float2(uv.x, uv.y + Time)).x;
    fragColor *= (0.94 - 0.1 * noise);
    
    // Boundary decay: Reduce heat near the bottom of the screen
    float boundaryDecay = 1.0 - smoothstep(0.0, 0.1, uv.y); // Strong decay when uv.y is close to 0
    fragColor *= (1.0 - 0.1 * boundaryDecay); // Reduce heat by up to 50% near the bottom
    
    // Heat source at mouse position
    float dist = distance(MousePos, uv);
    if (dist < 0.04 && MouseDown > 0.0 && MousePos.y < .96)
    {
        fragColor = float4(1.0, 1.0, 1.0, 1.0);
    }
    
    return fragColor;
}

// Technique
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 HeatSimulationPS();
    }
}