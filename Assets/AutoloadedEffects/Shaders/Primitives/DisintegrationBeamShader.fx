sampler tex1 : register(s1);
sampler tex2 : register(s2);

float globalTime;
matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, transformMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float2 Random(float2 p)
{
    return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f); // Smooth interpolation
    return lerp(
        lerp(Random(i + float2(0.0, 0.0)), Random(i + float2(1.0, 0.0)), u.x),
        lerp(Random(i + float2(0.0, 1.0)), Random(i + float2(1.0, 1.0)), u.x),
        u.y
    );
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float noise = tex2D(tex2, coords * float2(0.5, .15) + Noise(coords) + float2(globalTime * -2.3, 0));
    float endFade = smoothstep(1, 0.8 + noise * .09, coords.x);
    float distanceFromCenter = distance(coords.y, 0.5);
    float bloomFadeout = QuadraticBump(coords.y) * endFade;
    float4 fadeMapColor = tex2D(tex1, float2(frac(coords.x * 10 - globalTime * 5.5), coords.y));
    float opacity = (0.45 + fadeMapColor.g) * bloomFadeout;
    
    float fade = tex2D(tex2, float2(frac(coords.x * 6 - globalTime * 2.5), coords.y)).r;
    float4 laserColor = InverseLerp(0.28, 0.9, fade * bloomFadeout) * color;
    
    // Add strong bloom that fades along the beam
    color = saturate(color + (color.a / smoothstep(0, lerp(.5, 0, coords.x), distanceFromCenter) * 0.25) * bloomFadeout);
    
    float distanceOffset = tex2D(tex2, coords + float2(globalTime * -2.8, 0));
    float edgeFade = pow(abs(QuadraticBump(coords.y)), distanceOffset + 1.25 + laserColor);
    
    // Do even more bloom
    color += smoothstep(0.8, 1, edgeFade) * bloomFadeout;
    
    // Add a bunch of noise
    color *= (noise + 1 + step(0.5, noise + (0.5 - distanceFromCenter)));
    color *= opacity + laserColor;
    color *= bloomFadeout;
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
