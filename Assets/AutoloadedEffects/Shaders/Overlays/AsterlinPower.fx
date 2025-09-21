texture sampleTexture;
sampler2D tex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};
float time : register(c0);
float resolution : register(c1);
float opacity : register(c2);

// Simple 2D noise function
float random(float2 st)
{
    return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
}

// Smooth noise interpolation
float noise(float2 st)
{
    float2 i = floor(st);
    float2 f = frac(st);
    
    // Four corners in 2D of a tile
    float a = random(i);
    float b = random(i + float2(1.0, 0.0));
    float c = random(i + float2(0.0, 1.0));
    float d = random(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(lerp(a, b, u.x),
                lerp(c, d, u.x),
                u.y);
}

static const float2x2 rot = float2x2(0.877583, 0.479426, -0.479426, 0.877583);
float fbm(float2 st)
{
    float v = 0.0;
    float a = 0.5;
    float2 shift = float2(100.0, 100.0);
    
    for (int i = 0; i < 3; ++i)
    {
        v += a * noise(st);
        st = mul(rot, st) * 2.0 + shift;
        a *= 0.95;
    }
    return v;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 hiAsterlin = tex2D(tex, coords);
    
    coords = round(coords * resolution) / resolution;
    float2 uv = coords * 9 + float2(0, time * .12);
    
    float3 color = float3(0.0, 0.0, 0.0);

    // First layer of fBm
    float2 q = float2(0.0, 0.0);
    q.x = fbm(uv);
    q.y = fbm(uv + float2(1.0, 0.0));

    // Second layer of fBm with offsets
    float2 r = float2(0.0, 0.0);
    r.x = fbm(uv + .20 * q + float2(1.7, 9.2) + 0.15 * time);
    r.y = fbm(uv + 5.0 * q + float2(18.3, 2.8) + 0.126 * time);

    // Final fBm layer
    float f = fbm(uv + r);
    
    // Color mixing
    float3 color1 = float3(1, 1, .4);
    float3 color2 = float3(1, 0.6, .5);
    float3 color3 = float3(.8, .8, .4);
    float3 color4 = float3(1, 1.0, .3);
    
    color = lerp(color1, color2, saturate((f * f) * 4.0));
    color = lerp(color, color3, saturate(length(q)));
    color = lerp(color, color4, saturate(length(r)));

    // Final contrast adjustment
    float finalFactor = (f * f * f + 0.36 * f * f + 0.5 * f);
    color *= finalFactor;
    
    // Fix opacity to overlay on asterlin
    color *= .32 * opacity;
    
    return lerp(float4(0, 0, 0, 0), hiAsterlin + float4(color, 1.0) * sampleColor, any(hiAsterlin));
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}