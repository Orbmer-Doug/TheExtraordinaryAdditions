sampler2D screen : register(s0);
float Time : register(c0);
float warpDirection = 1.0;

float InverseLerp(float from, float to, float x)
{
    return (x - from) / (to - from);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 BACKGROUND_COLOR = float4(1.0, 1.0, 1.0, 1.0);
    float IDLE_DURATION = 1.0;
    float COLLAPSE_DURATION = 2.0;
    float COLLAPSE_MIN_POWER = 1.0;
    float COLLAPSE_MAX_POWER = 10.0;
    float SCALE_DURATION = 2.0;
    float totalDuration = IDLE_DURATION + COLLAPSE_DURATION + SCALE_DURATION;
    float time = fmod(Time, totalDuration);
    
    float idleEnd = IDLE_DURATION;
    float collapseEnd = IDLE_DURATION + COLLAPSE_DURATION;
    float scaleEnd = totalDuration;
    
    float collapseProgress;
    float scaleProgress;
    
    if (warpDirection > 0.5) // Warp
    {
        collapseProgress = clamp(InverseLerp(idleEnd, collapseEnd, time), 0.0, 1.0);
        scaleProgress = clamp(InverseLerp(collapseEnd, scaleEnd, time), 0.0, 1.0);
    }
    else // Unwarp
    {
        float reverseTime = totalDuration - time;
        collapseProgress = clamp(InverseLerp(idleEnd, collapseEnd, reverseTime), 0.0, 1.0);
        scaleProgress = clamp(InverseLerp(collapseEnd, scaleEnd, reverseTime), 0.0, 1.0);
    }
    
    float collapsePower = lerp(COLLAPSE_MIN_POWER, COLLAPSE_MAX_POWER, collapseProgress);
    
    float2 center = float2(0.5, 0.5);
    float2 offset = coords - center;
    float d = length(offset);
    float newD = pow(d * 2.0, 1.0 / collapsePower) / 2.0;
    float angle = atan2(offset.y, offset.x);
    
    float2 newOffset = normalize(offset) * newD / (1.0 - scaleProgress);
    
    float wave = sin(d * 10.0 + Time * 5.0) * 0.05 * collapseProgress;
    newOffset += normalize(offset) * wave;
    
    float2 newUv = center + newOffset;

    float4 collapsedColor = tex2D(screen, newUv);
    float doppler = clamp(d * 2.0, 0.0, 1.0);
    collapsedColor.rgb = lerp(collapsedColor.rgb, float3(0.5, 0.5, 1.0), collapseProgress * (1.0 - doppler) * 0.3);
    
    // Output to screen
    return lerp(collapsedColor, BACKGROUND_COLOR, scaleProgress);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}