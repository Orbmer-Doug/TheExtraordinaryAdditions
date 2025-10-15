sampler noiseTexture : register(s1);

float globalTime;
float appearanceInterpolant;
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

static const float PI = 3.141592654;
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    float taper = lerp(0.85, 0.95, sin(coords.y * PI));
    float4 color = 1;
    
    // Bias colors from white to the sample color near the edges
    color = lerp(color, input.Color, smoothstep(0.95, 0.99, coords.x / taper));
    color = lerp(color, input.Color, smoothstep(0.25, 0.42, distance(coords.y, 0.5)));
    
    float distortion = lerp(-1.1, 1.1, tex2D(noiseTexture, coords + float2(0, globalTime * sign(coords.y > 0.5 ? -1 : 1) * 1.21)).r);
    float opacity = pow(abs(sin((coords.y + (distortion * .1)) * PI)), distortion * .725 + .5);
    color /= opacity;
    
    // Taper off the end of the blade
    clip(1 - coords.x / taper);
    
    // Make it look like its unsheathing
    clip(appearanceInterpolant - coords.x);
    
    return color * appearanceInterpolant;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}