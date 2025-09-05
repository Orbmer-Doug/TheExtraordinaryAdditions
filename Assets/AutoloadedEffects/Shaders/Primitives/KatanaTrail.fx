float globalTime;
uniform bool flip;
sampler noiseTex : register(s0);

matrix transformMatrix;

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
    float4 pos = mul(input.Position, transformMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float QuadraticBezier(float t, float p0, float p1, float p2)
{
    float u = 1.0 - t;
    float tt = t * t;
    float uu = u * u;
    return (uu * p0) + (2.0 * u * t * p1) + (tt * p2);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    coords.y = lerp(coords.y, 1 - coords.y, flip);
    
    // Calculate the y-coordinate of the Bezier curve at this x
    float curveV = QuadraticBezier(coords.y, 0.0, 0.1, 1.0);

    // Discard pixels above the curve
    if (coords.x > curveV)
        discard;
    
    float4 orig = input.Color;
    float noise = tex2D(noiseTex, coords).r;
    float noise2 = abs(tex2D(noiseTex, coords * 1.1 + float2(-globalTime * 1.8, 0)).r * 1.3);
    float blendInterpolant = tex2D(noiseTex, coords * float2(1, 4) + float2(globalTime * -.5 * 4, 0));
    
    float4 color = input.Color;
    
    color /= (noise + 1 + step(0.1, noise + (0.5 - coords.y)));
    color -= pow(noise2, 2.2);
    color *= (noise * 1.8 + 0.2) * saturate((1 - coords.x) - noise * coords.y * 0.3) * 1.5;
    color = lerp(color, float4(.2, .2, .2, 1), blendInterpolant * 2.7);
    
    // Increase brightness toward the tip
    float tipBrightness = 2.0 - coords.x;
    color *= lerp(1.0, 3.6, pow(coords.y, 3.0) * (1.0 - coords.x) * tipBrightness) * (1.1 * tipBrightness);
    
    color *= orig.a;
    
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