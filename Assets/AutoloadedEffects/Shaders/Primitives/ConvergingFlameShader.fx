float time : register(c0);
float opacity : register(c1);
matrix transformMatrix : register(c2);

static const float PI = 3.141592653589;

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

static const int PaletteLen = 5;
static const float3 HeatPalette[PaletteLen] =
{
    float3(0.671, 0.671, 0), // Red
    float3(0.949, 0.949, 0.29), // Orange
    float3(1, 1, 0.525), // Light Orange
    float3(1, 1, 0.796), // Yellow
    float3(1, 1, 1) // White
};

float3 n_rand3(float3 p)
{
    float3 r = frac(sin(float3(dot(p, float3(127.1, 311.7, 371.8)), dot(p, float3(269.5, 183.3, 456.1)), dot(p, float3(352.5, 207.3, 198.67)))) * 43758.5453) * 2.0 - 1.0;
    return normalize(r);
}

float noise(float3 p)
{
    float3 fractional = frac(p);
    float3 integral = floor(p);
    float3 weight = fractional * fractional * fractional * (fractional * (fractional * 6.0 - 15.0) + 10.0);
    return lerp(lerp(lerp(dot(n_rand3(integral), fractional), dot(n_rand3(integral + float3(1.0, 0.0, 0.0)), fractional - float3(1.0, 0.0, 0.0)), weight.x),
                lerp(dot(n_rand3(integral + float3(0.0, 1.0, 0.0)), fractional - float3(0.0, 1.0, 0.0)), dot(n_rand3(integral + float3(1.0, 1.0, 0.0)), fractional - float3(1.0, 1.0, 0.0)), weight.x), weight.y),
                lerp(lerp(dot(n_rand3(integral + float3(0.0, 0.0, 1.0)), fractional - float3(0.0, 0.0, 1.0)), dot(n_rand3(integral + float3(1.0, 0.0, 1.0)), fractional - float3(1.0, 0.0, 1.0)), weight.x),
                     lerp(dot(n_rand3(integral + float3(0.0, 1.0, 1.0)), fractional - float3(0.0, 1.0, 1.0)), dot(n_rand3(integral + float3(1.0, 1.0, 1.0)), fractional - float3(1.0, 1.0, 1.0)), weight.x), weight.y), weight.z);
}

float oct_noise(float3 pos, float o)
{
    float noiseSum = 0.0;
    float divisorSum = 0.0;
    int intOctaves = (int) o;
    float fracOctaves = frac(o);
    
    [unroll(3)]
    for (int i = 0; i <= intOctaves; ++i)
    {
        float scale = pow(2.0, (float) i);
        divisorSum += 1.0 / scale;
        noiseSum += noise(pos * scale) / scale;
    }
    float scale = pow(2.0, (float) (intOctaves + 1));
    divisorSum += fracOctaves / scale;
    noiseSum += noise(pos * scale) * (fracOctaves / scale);
    return noiseSum / divisorSum;
}

float posterize(float v, int n)
{
    float fn = float(n);
    return floor(v * fn) / fn;
}

float fireball(float2 uv)
{
    float2 pos = uv;
    pos *= 10.0;
    float base = (-pow(abs(uv.y - 0.5) * 2.0, 2.0) + pow(abs(uv.x + 0.01), 8.0) - pow(abs(uv.x + 0.01), 10.0)) * 12.0 - pow(abs(1.01 - uv.x), 10.0);
    float wave = oct_noise(float3(pos + float2(time * 8.0, 0.0), time * 0.5), (1.0 - uv.x) * 2.0) / 3.0;
    float flares = pow(sin(1.0 - noise(float3(pos * 2.0 + float2(time * 16.0, 0.0), time)) * PI), 4.0) / 7.0;
    return base + wave + flares;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = 1 - input.TextureCoordinates;
    float value = fireball(coords) * opacity;
    float alpha = step(-0.0, (value));
    float3 color = HeatPalette[(int) (posterize(value, PaletteLen) * (float) PaletteLen)];
    float4 final = lerp(float4(color, alpha), float4(HeatPalette[0], alpha), alpha != 1) * alpha;
    final.rgb = lerp(final.rgb, HeatPalette[PaletteLen - 1] * alpha, final.r <= 0);
    
    return final;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}