sampler noise : register(s1);

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

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    // Make white hot effects based on noise
    float whiteHotNoise = 1 - tex2D(noise, coords + float2(globalTime * -4.3, 0));
    float whiteHotBrightness = pow(abs(QuadraticBump(coords.y) * (1 - coords.x)), 1.55) / whiteHotNoise;
    
    float edgeOpacity = QuadraticBump(coords.y);
    float4 color = input.Color * edgeOpacity * 1.6;
    
    // Bias the color to blues and randomize it with more noise
    color -= float4(1.5, 0.2, -0.9, 0) * tex2D(noise, coords * float2(0.7, 1.6) + float2(globalTime * -3.3, 0.16)) * color.a;
    
    // Fade the center of the trail up to a percentage to leave room for the object
    // This is how it looks 'engulfed'
    color *= (1 - QuadraticBump(coords.y) * smoothstep(0, 0.28, coords.x));
    
    return color + whiteHotBrightness * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}