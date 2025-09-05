sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float globalTime;
float3 color;
float noiseIntensity;
float horizontalDisplacementFactor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate noise values for both texture and positional displacement.
    float noise = tex2D(uImage1, coords * 3 + float2(0, globalTime * 0.54)).r;
    float pixelOffsetNoise = tex2D(uImage1, coords * 3 + float2(0, globalTime * 0.43)) * 2 - 1;
    
    float4 baseColor = tex2D(uImage0, coords + float2(pixelOffsetNoise * horizontalDisplacementFactor, 0)) * sampleColor;
    return baseColor - (1 - float4(color, 0)) * baseColor.a * pow(noise, 3) * noiseIntensity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}