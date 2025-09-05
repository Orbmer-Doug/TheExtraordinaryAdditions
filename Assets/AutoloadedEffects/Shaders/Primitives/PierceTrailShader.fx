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

float rand(float2 n)
{
    return frac(sin(dot(n, float2(12.9898, 12.1414))) * 83758.5453);
}

float noise(float2 n)
{
    const float2 d = float2(0.0, 1.0);
    float2 b = floor(n);
    float2 f = lerp(float2(0.0, 0.0), float2(1.0, 1.0), frac(n));
    return lerp(lerp(rand(b), rand(b + d.yx), f.x), lerp(rand(b + d.xy), rand(b + d.yy), f.x), f.y);
}

float3 ramp(float t, float3 col)
{
    return t <= .5 ? float3(col.r * 3 - t * 1.4, col.g, col.b * 1.4) / t : float3(col.r * 2 * (1. - t) * 2., col.g, col.b * 1.5) / t;
}

float fire(float2 n)
{
    return noise(n) + noise(n * 11.1) * .6 + noise(n * 5.4) * .42;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    float4 final = color;
    
    coords.x += coords.y < .5 ? 2.0 + globalTime * .35 : -11.0 + globalTime * .3;
    coords.y = abs(coords.y - .5);
    coords *= 4.0;
    
    float q = fire(coords - globalTime) / 2.0;
    float2 r = float2(fire(coords + q / 2.0 + globalTime - coords.x - coords.y), fire(coords + q - globalTime));
    float grad = pow((r.y + r.y) * max(.0, coords.y) + .1, 5.0);
    float3 rgb = ramp(grad, color.rgb);
    final = float4(rgb, 0);
    final *= color.a;
    
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
