sampler baseTexture : register(s0);
sampler uvOffsetingNoiseTexture : register(s1);

float globalTime;
float warpSpeed;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate values for the warp noise
    float warpAngle = tex2D(uvOffsetingNoiseTexture, coords * 7.3 + float2(0, globalTime * 0.1)).r * 16;
    float2 warpNoiseOffset = float2(sin(warpAngle + 1.57), sin(warpAngle));
   
    // Make the colors dissipate and move around in accordance with the warp noise
    float4 color = tex2D(baseTexture, coords - warpNoiseOffset * warpSpeed * 2.);
    color.rgb *= 0.884;
    color.a *= 0.8;
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}