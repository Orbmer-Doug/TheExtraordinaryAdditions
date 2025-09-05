sampler uImage1 : register(s1);
float3 color;
float3 secondColor;
float3 thirdColor;
float saturation;

float globalTime;
matrix transformMatrix;

int detail = 2; // The less this number is the more detail the shader will give
float trailSpeed = .45f;
float edgeAmount = .3;

float noise2Rand = 2.2;
float noise3Rand = 1.1;

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

const float PiOver2 = 1.570796327;
const float Pi = 3.14159265359;
const float Tau = 6.28318530718;
float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}
float Sin01(float x)
{
    return sin(x) * .5f + .5f;
}

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 finalColor = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float2 res = float2(800, 800);
    coords.x -= coords.x % (1 / res.x);
    coords.y -= coords.y % (1 / res.y);
    
    // Move against the starting point of the slash
    float2 noiseDetail = float2(detail, detail);
    float2 noiseCoords = coords * float2(1.0 / noiseDetail.x, noiseDetail.y) - float2(globalTime * trailSpeed, 0);
    
    // Prevent sudden cutoffs
    noiseCoords.x = Sin01(noiseCoords.x * 5);
    
    // Create some varied noises based on our coords for pretty detail
    float noise = tex2D(uImage1, noiseCoords).r;
    float noise2 = pow(tex2D(uImage1, noiseCoords * 2.2).r, 1.6);
    float noise3 = pow(tex2D(uImage1, noiseCoords * 1.1).r, 1.3);

    // Read the fade map as a streak.
    float4 fadeMapColor = tex2D(uImage1, float2(frac(coords.y + sin(globalTime + PiOver2) * 0.01), frac(coords.x - globalTime * 1.4 * saturation)));
    fadeMapColor.r *= pow(coords.x, 0.2);
    
    float opacity = lerp(1.45, 1.95, fadeMapColor.r) * finalColor.a;
    opacity *= pow(sin(coords.y * Pi), lerp(1, 8, pow(coords.x, 2)));
    opacity *= pow(sin(coords.x * Pi), 1.5);
    opacity *= fadeMapColor.r * 1.5 + 1;
    
    opacity *= noise * pow(saturate((1 - coords.x) - noise * coords.y * 0.54), 2);
    
    // Fade to the second primary color based on one of the noise values.
    finalColor = lerp(finalColor, float4(color, 1), noise2);

    // Create dark colors. Points that are further along the trail are incentized to fade to the dark color more strongly. This also holds true to points that are closer to the bottom of
    // the trail. To create some variance on top of this, the primary noise value is added as well.
    float darkColorWeight = saturate(coords.y * 1 + coords.x * .5 + noise * 0.3);
    finalColor = lerp(finalColor, float4(secondColor, 1), darkColorWeight);

    // Creates a streak toward the top of the blade which is then multiplied to make this dissipate the farther along a trail point
    float edgeWeight = InverseLerp(edgeAmount, 0, coords.x) * pow(1 - coords.y, PiOver2);
    finalColor = lerp(finalColor, float4(thirdColor, 1), edgeWeight);
    
    // Finalize the color and add some final bit of randomness
    return finalColor * opacity * input.Color.a * (noise3 * 2.4 + 2.4) * 1.6;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
