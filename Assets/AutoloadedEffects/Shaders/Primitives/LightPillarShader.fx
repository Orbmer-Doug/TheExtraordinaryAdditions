sampler tex : register(s1);
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
    
    float distanceOffset = (tex2D(tex, coords + float2(globalTime * -2.8 + .4, 0)) - 0.5) * 0.156;
    float distanceFromCenter = distance(coords.y, 0.5) + distanceOffset;
    float noise = tex2D(tex, coords * float2(0.8, 1.75) + float2(globalTime * -2, 0));
    
    float4 color = input.Color * QuadraticBump(coords.y) * 1.1;
    color *= (noise + 1.1 + step(.85, noise + (0.5 - distanceFromCenter))); // the key to the fire aesthetic across the pillar
    color /= smoothstep(noise * .02, 0.1, coords.x); // 'round' the base of the pillar to make it seem like there is a source
    color *= smoothstep(0.98, 0.9 + noise * 0.06, coords.x); // fade the end
    color.rgb -= distanceFromCenter * .5; // subtract the edges to help with coloring
    
    return color * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}