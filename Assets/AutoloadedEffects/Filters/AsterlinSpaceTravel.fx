sampler2D screen : register(s0);
float time : register(c0);
bool reverse : register(c1);
bool stop : register(c2);

static const float CollapseDuration = 2.0;
static const float CollapseMinPower = 1.0;
static const float CollapseMaxPower = 7.0;
static const float ScaleDuration = 2.0;
static const float TotalDuration = CollapseDuration + ScaleDuration;
static const float4 BackgroundColor = float4(1.0, 1.0, 1.0, 1.0);
static const float2 Center = float2(0.5, 0.5);

float InverseLerp(float from, float to, float x)
{
    return clamp((x - from) / (to - from), 0.0, 1.0);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float actualTime = lerp(time, TotalDuration - time, reverse);
    float collapseProgress = InverseLerp(0, CollapseDuration, actualTime);
    float scaleProgress = InverseLerp(CollapseDuration, TotalDuration, actualTime);

    float collapsePower = lerp(CollapseMinPower, CollapseMaxPower, collapseProgress);
    
    float2 offset = coords - Center;
    float d = length(offset);
    float newD = pow(d * 2.0, 1.0 / collapsePower) / 2.0;
    float angle = atan2(offset.y, offset.x);
    
    float2 newOffset = normalize(offset) * newD / (1.0 - scaleProgress);
    
    float wave = sin(d * 10.0 + time * 5.0) * 0.05 * collapseProgress;
    newOffset += normalize(offset) * wave;
    
    float2 newUv = Center + newOffset;

    float4 collapsedColor = tex2D(screen, newUv);
    float doppler = clamp(d * 2.0, 0.0, 1.0);
    collapsedColor.rgb = lerp(collapsedColor.rgb, float3(0.5, 0.5, 1.0), collapseProgress * (1.0 - doppler) * 0.3);
    
    // Output to screen
    return lerp(lerp(collapsedColor, BackgroundColor, scaleProgress), tex2D(screen, coords), stop);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}