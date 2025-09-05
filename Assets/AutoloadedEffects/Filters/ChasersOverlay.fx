sampler baseTexture : register(s0);
sampler slashTargetTexture : register(s1);
sampler behindSplitTexture : register(s2);
sampler noiseTexture : register(s3);

float globalTime;
float splitBrightnessFactor;
float splitTextureZoomFactor;
float2 backgroundOffset;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Retrieve data from the render target
    // R = Brightness
    // GB = Distortion direction in a 0-1 range
    // A = Distortion intensity
    float4 targetData = tex2D(slashTargetTexture, coords + backgroundOffset);
    float backgroundDimensionBrightness = targetData.r;
    float distortionIntensity = targetData.a * 5;
    float2 distortionOffset = targetData.gb * 2 - 1;
    
    // Sample from the base texture, taking distortion into account
    float2 distortionPosition = coords + distortionOffset * lerp(0.85, 1.15, sin(globalTime * 10) * 0.5 + 0.5) * distortionIntensity * 0.013;
    float4 color = tex2D(baseTexture, distortionPosition);
    
    // Sample colors from the "behind" texture
    float4 backgroundDimensionColor1 = tex2D(behindSplitTexture, coords * splitTextureZoomFactor + float2(globalTime, 0) * -0.23) * splitBrightnessFactor;
    float4 backgroundDimensionColor2 = tex2D(behindSplitTexture, coords + backgroundDimensionColor1.rb * splitTextureZoomFactor * 0.12) * splitBrightnessFactor * 0.5;
    
    // Combine the aforementioned colors together, taking into account the overall brightness of them at the given pixel
    float4 backgroundDimensionColor = (backgroundDimensionColor1 + backgroundDimensionColor2) * backgroundDimensionBrightness;
    
    return color + backgroundDimensionColor + targetData.a * 0.1;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}