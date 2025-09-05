float time : register(c0);
float opacity : register(c1);

matrix vertexMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, vertexMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float3 random(float3 uv)
{
    return float3(frac(sin(dot(uv, float3(12.9898, 78.233, 45.5432))) * 43758.5453),
                  frac(cos(dot(uv, float3(78.233, 12.9898, 45.5432))) * 43758.5453),
                  frac(sin(dot(uv, float3(12.9898, 78.233, 45.5432))) * 43758.5453));
}
float worley(float3 uv)
{
    float scale = 2.2;
    float3 index_uv = floor(uv * scale);
    float3 fract_uv = frac(uv * scale);
    float min_dist = 1;
    for (int z = -1; z <= 1; z++)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                float3 neighbor = float3(float(x), float(y), float(z));
                float3 pointed = random(index_uv + neighbor);
                float3 diff = neighbor + pointed - fract_uv;
                float dist = length(diff);
                min_dist = min(min_dist, dist);
            }
        }
    }
    return min_dist;
}

float fbm3d(float3 x, const in int iterations)
{
    float v = 0;
    float a = .5;
    float3 shift = float3(100, 100, 100);
    
    for (int i = 0; i < 32; ++i)
    {
        if (i < iterations)
        {
            v += a * worley(x);
            x = x * 2 + shift;
            a *= .35;
        }
    }
    return v;
}

float3 rotateZ(float3 v, float angle)
{
    float cosAngle = cos(angle);
    float sinAngle = sin(angle);
    return float3(
        v.x * cosAngle - v.y * sinAngle,
        v.x * sinAngle + v.y * cosAngle,
        v.z
    );
}

float facture(float3 vec) 
{
    float3 normalizedVector = normalize(vec);
    return max(max(normalizedVector.x, normalizedVector.y), normalizedVector.z);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Normalize between [-1, 1]
    float2 uv = input.TextureCoordinates * 2 - 1;
    
    float3 color = float3(uv.xy, 0);
    color.z += .15;
    
    color = normalize(color);
    color -= .2 * float3(0, 0, time);
    
    // Log base 0.5
    float angle = -log2(length(uv));
    
    // Twirl
    color = rotateZ(color, angle);
    
    // Modify the color with noise
    float frequency = 1.4;
    color.x = fbm3d(color * frequency + 0, 5);
    color.y = fbm3d(color * frequency + 1, 5);
    color.z = fbm3d(color * frequency + 2, 5);
    
    // Some other color adjustments
    float3 noiseColor = color;
    noiseColor *= 1.5;
    noiseColor -= 0.1;
    noiseColor *= 0.188;
    noiseColor += float3(uv.xy, 0.0);
    
    float noiseColorLength = length(noiseColor);
    noiseColorLength = .77 - noiseColorLength;
    noiseColorLength *= 7.5; // bright
    
    // The golden center of the portal
    float3 emissionColor = float3(.961, .592, .078) * (noiseColorLength * .4);
    
    float fac = length(uv) - facture(color + .12);
    fac += .1;
    fac *= lerp(0, 3.4, opacity);
    
    // Finally combine all colors and clamp
    color = lerp(emissionColor, float3(fac, fac, fac), fac + 1.2);
    color.r = clamp(color.r, 0, 1);
    color.g = clamp(color.g, 0, 1);
    color.b = clamp(color.b, 0, 1);
    
    // Try to remove the white background
    float alpha = smoothstep(1, .45, length(uv));
    color *= alpha;
    return float4(color, alpha) * alpha * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}