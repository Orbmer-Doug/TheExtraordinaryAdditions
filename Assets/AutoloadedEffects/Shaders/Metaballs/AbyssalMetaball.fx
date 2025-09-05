sampler2D InfluenceMap : register(s1);
sampler background : register(s2);

float globalTime;
float threshold;
float epsilon;

float edgeDetect(float2 uv, float2 resolution)
{
    float step = 1.0 / resolution; // Step size based on resolution
    float h = tex2D(InfluenceMap, uv + float2(step, 0)).r - tex2D(InfluenceMap, uv - float2(step, 0)).r;
    float v = tex2D(InfluenceMap, uv + float2(0, step)).r - tex2D(InfluenceMap, uv - float2(0, step)).r;
    return sqrt(h * h + v * v) * .8; // Magnitude of gradient
}

// Simple hash function for noise
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// 2D value noise
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i + float2(0.0, 0.0));
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p)
{
    float v = 0.0;
    float a = 2;
    float2 shift = float2(100.0, 100.0);

    for (int i = 0; i < 12; i++)
    {
        v += a * noise(p);
        p = p * 3.0 + shift;
        a *= 0.225;
    }
    return v;
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float influence = tex2D(InfluenceMap, uv).r;
    float alpha = smoothstep(threshold - epsilon, threshold + epsilon, influence);
    
    // Edge detection
    float edge = edgeDetect(uv, float2(1000, 1000) * alpha);
    
    float3 blue = float3(0.1, 0.37, 1.20);
    
    // Calculate outline strength
    float outlineStrength = edge * (1.0 - alpha);
    
    float2 flowOffset = float2(0.0, 0.0);
    float flowSpeed = .5;
    float flowScale = 3.0;
    flowOffset.x = fbm(uv * flowScale + float2(globalTime * flowSpeed, 0.0));
    flowOffset.y = fbm(uv * flowScale + float2(0.0, globalTime * flowSpeed));
    uv += (flowOffset - 0.5) * .8;
    
    float3 interiorColor = blue * tex2D(background, uv * float2(.4, 1) ).rgb * alpha;
    float3 finalColor = lerp(interiorColor, blue, outlineStrength);
    
    float3 border = blue * edge * (1.0 - alpha);
    return lerp(float4(finalColor, alpha), float4(border, 1), edge) * 2;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}