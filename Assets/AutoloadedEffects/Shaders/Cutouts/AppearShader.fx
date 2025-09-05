sampler tex : register(s0);
float completion;
float2 dir;
static const float erasureBlend = 1;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(tex, coords);
    if (!any(color))
        return color;

    float2 normalizedDir = normalize(dir);
    
    float progress = dot(coords, normalizedDir);
    
    float remappedProgress = (progress + 1.0) * 0.5; // Adjust for (0, -1) direction
    
    // Calculate fade factor using smoothstep for smooth transition
    float fade = smoothstep(completion - 0.1, completion, remappedProgress);
    color.a *= (fade);
    
    return color * fade;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}