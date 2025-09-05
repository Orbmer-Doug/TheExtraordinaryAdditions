// Inspired off of: https://www.shadertoy.com/view/XsS3Rm

float Time;
sampler warp : register(s0);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

static const int max_iterations = 255;

float2 complex_square(float2 v)
{
    return float2(
		v.x * v.x - v.y * v.y,
		v.x * v.y * 2.0
	);
}

// Derived from iq: https://iquilezles.org/articles/palettes/
float3 palette(float t, float3 a, float3 b, float3 c, float3 d)
{
    return a + b * cos(6.28318 * (c * t + d));
}

float3 mod289(float3 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 mod289(float4 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 permute(float4 x)
{
    return mod289(((x * 34.0) + 1.0) * x);
}

float4 taylorInvSqrt(float4 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

float snoise(float3 v)
{
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);

    // First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - D.yyy;

    // Permutations
    i = mod289(i);
    float4 p = permute(permute(permute(
                i.z + float4(0.0, i1.z, i2.z, 1.0))
                + i.y + float4(0.0, i1.y, i2.y, 1.0))
                + i.x + float4(0.0, i1.x, i2.x, 1.0));

    // Gradients
    float n_ = 0.142857142857; // 1/7
    float3 ns = n_ * D.wyz - D.xzx;
    float4 j = p - 49.0 * floor(p * ns.z * ns.z);

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_);

    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);

    // Normalize gradients
    float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR
{
    float zoom = 3.4;
    float2 uv = input.TextureCoordinates * zoom;
    uv -= float2(.5, .5) * zoom;
    uv = round(uv * 128) / 128;
	
    float time = Time * 1.4;
    
    // Spin around for a smooth animation and add some variation
    float radius = lerp(.69, .755, sin(time * .1) * .5 + .5) * (sin((time / 3.) - .57) * 0.2 + 0.85);
    float2 circle = float2(radius * cos((time / 3.)), radius * sin((time / 3.)));
    float2 coord = uv;
    float scale = 0.08;
	
    int iterations = max_iterations;
	
    // Make the fractal
    // The max iterations is way over what HLSL allows but it breaks before then
    for (int i = 0; i < max_iterations; i++)
    {
        coord = circle + complex_square(coord);
        if (dot(coord, coord) > 4.0)
        {
            iterations = i;
            break;
        }
    }
    
    float psychedelicInterpolant = float(iterations) * scale;
    float4 result = float4(psychedelicInterpolant, psychedelicInterpolant, psychedelicInterpolant, 0);
    
    // Incorporate noise maps for a psychedelic effect
    float2 warpNoiseOffset = tex2D(warp, uv * 7.3 + float2(Time * 0.2, 0)).rg * 6;
    psychedelicInterpolant += snoise(float3(uv * 0.9 + warpNoiseOffset * 0.023, sin(Time * .01) * 20)) * 1.45;
    float brightnessInterpolant = snoise(float3(uv * .5 - warpNoiseOffset * 0.055, 1));

    // Calculate the psychedelic color palette
    float3 psychedelicColor = palette(psychedelicInterpolant, float3(1, .9, 1), float3(0.5, 0.5 , 0.2), float3(1, 1, 1), float3(0, 0.333, 0.667)) * 0.8;
    psychedelicColor += pow(abs(brightnessInterpolant), 1.5) * .4;
    
    result.rgb *= psychedelicColor / max(1.1, max(.0001, length(uv) * 2));
    //final = lerp(final, float4(psychedelicColor, 1), final.r * .8); black
    
    result = floor(result * 32) / 32; // Color quantization
    result = pow(abs(result), 1.5); // Add contrast
    
    return result;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
