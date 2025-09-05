// Input texture (render target)
sampler2D renderTarget : register(s0);

// Palette texture (1D texture for palette colors)
sampler2D PaletteTexture : register(s1);

// Bayer matrix
static const float bayer4x4[16] =
{
    1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
    13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
    4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
    16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
};

// HSV conversion functions
float3 HSVToRGB(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float3 RGBToHSV(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float GetBayerValue(float2 uv)
{
    int2 pixelCoord = int2(uv * float2(1920, 1080));
    int index = (pixelCoord.x % 4) + (pixelCoord.y % 4) * 4;
    return bayer4x4[index];
}
float4 ps_main(float2 texCoord : TEXCOORD0) : COLOR
{
    float4 originalColor = tex2D(renderTarget, texCoord);
    float3 originalHSV = RGBToHSV(originalColor.rgb);

    // Reduced palette size to fit SM3.0 limits
    float minDistance = 1e10;
    float4 closestColor = float4(0, 0, 0, 1);
    float3 secondClosestHSV = float3(0, 0, 0);
    float secondMinDistance = minDistance;

    [unroll(32)] // Force unroll
    for (int i = 0; i < 32; i++)
    {
        float2 paletteUV = float2(i / 31.0, 0.5); // 0 to 1 across 32 colors
        float3 paletteColor = tex2D(PaletteTexture, paletteUV).rgb;
        float3 paletteHSV = RGBToHSV(paletteColor);
        float currentDistance = dot(originalHSV - paletteHSV, float3(1.0, 0.5, 1.0));
        if (currentDistance < minDistance)
        {
            secondMinDistance = minDistance;
            secondClosestHSV = paletteHSV;
            minDistance = currentDistance;
            closestColor = float4(paletteColor, originalColor.a);
        }
        else if (currentDistance < secondMinDistance)
        {
            secondMinDistance = currentDistance;
            secondClosestHSV = paletteHSV;
        }
    }

    // Apply dithering
    float ditherThreshold = GetBayerValue(texCoord);
    float colorDifference = dot(originalHSV - secondClosestHSV, float3(1.0, 0.5, 1.0));
    if (colorDifference > 0.1)
    {
        float blendFactor = smoothstep(0.0, 1.0, (minDistance - minDistance) / colorDifference);
        float ditheredValue = (blendFactor + ditherThreshold - 0.5);
        float3 finalHSV = lerp(originalHSV, secondClosestHSV, saturate(ditheredValue));
        return float4(HSVToRGB(finalHSV), originalColor.a);
    }

    return closestColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 ps_main();
    }
}