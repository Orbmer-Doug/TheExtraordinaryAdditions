sampler2D mainTex : register(s0);
sampler2D noise : register(s1);

float globalTime;
uniform float threshold;
uniform float magnification = 0.5;
uniform float warpIntensity = 0.1;
uniform int distortionSize = 1;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 noiseColor = tex2D(noise, coords * magnification);
    float isTransparent = step(noiseColor.r, threshold);
    
    // Detect edges by checking neighboring pixels
    float2 pixelSize = 1.0 / coords; // For sampling neighbors
    float edge = 0.0;
    
    [unroll(2)]
    for (int i = -distortionSize; i <= distortionSize; i++)
    {
        [unroll(2)]
        for (int j = -distortionSize; j <= distortionSize; j++)
        {
            if (i == 0 && j == 0)
                continue;
            
            float2 offset = float2(float(i), float(j)) * pixelSize;
            float4 neighborNoise = tex2D(noise, (coords + offset) * magnification);
            float neighborTransparent = step(neighborNoise.r, threshold);
            edge += abs(isTransparent - neighborTransparent);
        }
    }
    
    // Normalize
    edge = saturate(edge);
    
    float2 warpOffset = float2(0.0, 0.0);
    if (edge > 0.0)
    {
        // Sample noise to create a dynamic warp direction
        float2 warpNoise = tex2D(noise, coords + globalTime * 0.1).rg * 2.0 - 1.0;
        warpOffset = warpNoise * edge * warpIntensity;
    }
    
    float4 color = tex2D(mainTex, coords + warpOffset);
    
    return color * (1.0 - isTransparent);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}